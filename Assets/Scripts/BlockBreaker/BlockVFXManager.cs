using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Spawns GPU VFX Graph bursts and physical 3D sub-cube debris particles when blocks shatter.
    /// Optimized for zero GC allocations per frame and instant frame-0 PSO prewarming across iOS (Metal), PC (DX12/Vulkan), and Web (WebGPU).
    /// </summary>
    public class BlockVFXManager : MonoBehaviour
    {
        public static BlockVFXManager Instance { get; private set; }

        [Header("VFX Configuration")]
        [SerializeField] private VisualEffectAsset shatterVfxAsset;
        [SerializeField] private int poolSize = 10;

        [Header("Sub-Box Physics Debris")]
        [SerializeField] private bool spawnPhysicalSubBoxes = true;
        [SerializeField] private int debrisPiecesPerBlock = 8; // 2x2x2 sub-cubes
        [SerializeField] private float debrisExplosionForce = 7f;
        [SerializeField] private Material debrisMaterial;

        public Material DebrisMaterial => debrisMaterial;

        public void SetDebrisMaterial(Material mat)
        {
            debrisMaterial = mat;
        }

        public static void SetInstanceForTesting(BlockVFXManager instance)
        {
            Instance = instance;
        }

        private Material cachedFallbackMaterial;
        private static MaterialPropertyBlock debrisPropBlock;

        private readonly Queue<VisualEffect> vfxPool = new Queue<VisualEffect>();
        private readonly Queue<GameObject> debrisPool = new Queue<GameObject>();

        // Struct-based zero-allocation tracking for active debris particles
        private struct ActiveDebris
        {
            public GameObject gameObject;
            public Transform transform;
            public Vector3 velocity;
            public Vector3 rotationSpeed;
            public Vector3 startScale;
            public float elapsed;
            public float duration;
        }

        // Struct-based zero-allocation tracking for active VFX Graph bursts
        private struct ActiveVFX
        {
            public VisualEffect effect;
            public float elapsed;
            public float duration;
        }

        private readonly List<ActiveDebris> activeDebrisList = new List<ActiveDebris>(128);
        private readonly List<ActiveVFX> activeVfxList = new List<ActiveVFX>(32);

        private static readonly int ColorPropertyId = Shader.PropertyToID("BlockColor");
        private static readonly int NormalPropertyId = Shader.PropertyToID("HitNormal");
        private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorPropId = Shader.PropertyToID("_EmissionColor");
        private static readonly Vector3 Gravity = new Vector3(0f, -14f, 0f);

        public int AvailableVfxCount => vfxPool.Count;
        public int AvailableDebrisCount => debrisPool.Count;
        public int ActiveDebrisCount => activeDebrisList.Count;
        public int ActiveVfxCount => activeVfxList.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            InitializePool();
        }

        private void Start()
        {
            Prewarm();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            ClearAllActive();
        }

        public void InitializePool()
        {
            // VFX Graph pool
            for (int i = 0; i < poolSize; i++)
            {
                VisualEffect effect = CreateNewVFXInstance();
                effect.gameObject.SetActive(false);
                vfxPool.Enqueue(effect);
            }

            // Debris mini-cubes pool
            if (spawnPhysicalSubBoxes)
            {
                int totalDebris = poolSize * debrisPiecesPerBlock;
                for (int i = 0; i < totalDebris; i++)
                {
                    GameObject debris = CreateDebrisPiece();
                    debris.SetActive(false);
                    debrisPool.Enqueue(debris);
                }
            }
        }

        /// <summary>
        /// Frame-0 PSO and shader prewarming to eliminate first-hit pipeline compilation hitches.
        /// Prewarms raster shaders, compiles VFX Graph compute kernels, and prepares MaterialPropertyBlock
        /// across Metal (iOS), DirectX 12 / Vulkan (PC), and WebGPU (Web).
        /// </summary>
        public void Prewarm()
        {
            // 1. Warm up raster shaders in memory on standalone player builds (iOS, PC, Web)
            if (!Application.isEditor)
            {
                Shader.WarmupAllShaders();
            }

            // 2. Prewarm 1 VFX Graph instance to force driver compute & particle PSO compilation
            if (vfxPool.Count > 0)
            {
                VisualEffect vfx = vfxPool.Peek();
                if (vfx != null)
                {
                    // Move far off-camera so it does not draw on screen
                    vfx.transform.position = new Vector3(0f, -500f, 0f);
                    vfx.gameObject.SetActive(true);

                    if (vfx.HasVector4(ColorPropertyId)) vfx.SetVector4(ColorPropertyId, Vector4.one);
                    if (vfx.HasVector3(NormalPropertyId)) vfx.SetVector3(NormalPropertyId, Vector3.up);

                    vfx.Play();
                    vfx.Simulate(0.016f);
                    vfx.Stop();
                    vfx.gameObject.SetActive(false);
                }
            }

            // 3. Prewarm debris renderer & MaterialPropertyBlock
            if (spawnPhysicalSubBoxes && debrisPool.Count > 0)
            {
                GameObject debris = debrisPool.Peek();
                if (debris != null)
                {
                    var mr = debris.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Material baseMat = GetOrCreateDebrisMaterial();
                        if (baseMat != null) mr.sharedMaterial = baseMat;

                        debrisPropBlock ??= new MaterialPropertyBlock();
                        debrisPropBlock.Clear();
                        debrisPropBlock.SetColor(BaseColorPropId, Color.white);
                        debrisPropBlock.SetColor(ColorPropId, Color.white);
                        debrisPropBlock.SetColor(EmissionColorPropId, Color.white);
                        mr.SetPropertyBlock(debrisPropBlock);
                    }
                }
            }
        }

        private VisualEffect CreateNewVFXInstance()
        {
            GameObject go = new GameObject("VFX_BlockShatter_Instance");
            go.transform.SetParent(transform);
            VisualEffect vfx = go.AddComponent<VisualEffect>();
            if (shatterVfxAsset != null)
            {
                vfx.visualEffectAsset = shatterVfxAsset;
            }
            vfx.playRate = 1.0f;
            return vfx;
        }

        public Material GetOrCreateDebrisMaterial()
        {
            if (debrisMaterial != null) return debrisMaterial;

            if (cachedFallbackMaterial == null)
            {
                var shader = Shader.Find("Arcade/VFX_BlockDebris") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                {
                    cachedFallbackMaterial = new Material(shader) { name = "M_BlockDebris_Fallback" };
                }
            }
            return cachedFallbackMaterial;
        }

        private GameObject CreateDebrisPiece()
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = "SubBox_Debris";
            debris.transform.SetParent(transform);
            debris.transform.localScale = Vector3.one * 0.45f;

            // Remove default collider so particles don't interfere with ball physics
            Collider col = debris.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }

            var mr = debris.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = GetOrCreateDebrisMaterial();
                if (mat != null)
                {
                    mr.sharedMaterial = mat;
                }
            }

            return debris;
        }

        public void PlayBlockShatter(Vector3 position, Color blockColor, Vector3 hitNormal)
        {
            // 1. Play GPU VFX Graph burst
            VisualEffect effect = vfxPool.Count > 0 ? vfxPool.Dequeue() : CreateNewVFXInstance();
            effect.transform.position = position;
            effect.gameObject.SetActive(true);

            Color hdrColor = blockColor * 1.6f;
            hdrColor.a = 1.0f;

            if (effect.HasVector4(ColorPropertyId)) effect.SetVector4(ColorPropertyId, (Vector4)hdrColor);
            if (effect.HasVector3(NormalPropertyId)) effect.SetVector3(NormalPropertyId, hitNormal);

            effect.Play();
            activeVfxList.Add(new ActiveVFX { effect = effect, elapsed = 0f, duration = 1.2f });

            // 2. Spawn physical sub-box particles exploding in 3D
            if (spawnPhysicalSubBoxes)
            {
                SpawnSubBoxDebrisBurst(position, blockColor, hitNormal);
            }
        }

        private void SpawnSubBoxDebrisBurst(Vector3 centerPos, Color blockColor, Vector3 hitNormal)
        {
            // Subdivide 1x1 cube into 2x2x2 sub-boxes (offsets at -0.25 and +0.25 on each axis)
            float offset = 0.25f;
            float[] offsets = { -offset, offset };

            Material baseMat = GetOrCreateDebrisMaterial();
            debrisPropBlock ??= new MaterialPropertyBlock();

            foreach (float ox in offsets)
            {
                foreach (float oy in offsets)
                {
                    foreach (float oz in offsets)
                    {
                        GameObject debris = debrisPool.Count > 0 ? debrisPool.Dequeue() : CreateDebrisPiece();
                        Vector3 subPos = centerPos + new Vector3(ox, oy, oz);
                        Transform t = debris.transform;
                        t.position = subPos;
                        t.localScale = Vector3.one * 0.42f;
                        t.rotation = Random.rotation;

                        MeshRenderer mr = debris.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            if (mr.sharedMaterial != baseMat && baseMat != null)
                            {
                                mr.sharedMaterial = baseMat;
                            }

                            // Zero-allocation tinting via MaterialPropertyBlock (preserves SRP Batching)
                            debrisPropBlock.Clear();
                            debrisPropBlock.SetColor(BaseColorPropId, blockColor);
                            debrisPropBlock.SetColor(ColorPropId, blockColor);
                            debrisPropBlock.SetColor(EmissionColorPropId, blockColor * 1.3f);
                            mr.SetPropertyBlock(debrisPropBlock);
                        }

                        debris.SetActive(true);

                        // Velocity direction: outward spherical bias + incoming hit direction
                        Vector3 outward = (new Vector3(ox, oy, oz)).normalized;
                        Vector3 burstVel = (outward * 0.7f + hitNormal * 0.3f + Random.insideUnitSphere * 0.3f).normalized * debrisExplosionForce;
                        Vector3 rotSpeed = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-360f, 360f));

                        activeDebrisList.Add(new ActiveDebris
                        {
                            gameObject = debris,
                            transform = t,
                            velocity = burstVel,
                            rotationSpeed = rotSpeed,
                            startScale = t.localScale,
                            elapsed = 0f,
                            duration = 0.75f
                        });
                    }
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            // Freeze simulation when game is paused
            if (dt <= 0f) return;

            // 1. Zero-allocation update for active debris pieces
            for (int i = activeDebrisList.Count - 1; i >= 0; i--)
            {
                ActiveDebris item = activeDebrisList[i];
                if (item.gameObject == null || item.transform == null)
                {
                    activeDebrisList.RemoveAt(i);
                    continue;
                }

                item.elapsed += dt;
                float t = item.elapsed / item.duration;

                item.velocity += Gravity * dt;
                item.transform.position += item.velocity * dt;
                item.transform.Rotate(item.rotationSpeed * dt);

                // Smooth quadratic shrink out
                float scaleFactor = Mathf.Lerp(1f, 0f, t * t);
                item.transform.localScale = item.startScale * scaleFactor;

                if (item.elapsed >= item.duration)
                {
                    item.gameObject.SetActive(false);
                    debrisPool.Enqueue(item.gameObject);
                    activeDebrisList.RemoveAt(i);
                }
                else
                {
                    activeDebrisList[i] = item;
                }
            }

            // 2. Zero-allocation update for active VFX Graph bursts
            for (int i = activeVfxList.Count - 1; i >= 0; i--)
            {
                ActiveVFX item = activeVfxList[i];
                if (item.effect == null)
                {
                    activeVfxList.RemoveAt(i);
                    continue;
                }

                item.elapsed += dt;
                if (item.elapsed >= item.duration)
                {
                    item.effect.Stop();
                    item.effect.gameObject.SetActive(false);
                    vfxPool.Enqueue(item.effect);
                    activeVfxList.RemoveAt(i);
                }
                else
                {
                    activeVfxList[i] = item;
                }
            }
        }

        /// <summary>
        /// Instantly recycles all active debris and VFX instances back into their pools.
        /// </summary>
        public void ClearAllActive()
        {
            for (int i = activeDebrisList.Count - 1; i >= 0; i--)
            {
                ActiveDebris item = activeDebrisList[i];
                if (item.gameObject != null)
                {
                    item.gameObject.SetActive(false);
                    debrisPool.Enqueue(item.gameObject);
                }
            }
            activeDebrisList.Clear();

            for (int i = activeVfxList.Count - 1; i >= 0; i--)
            {
                ActiveVFX item = activeVfxList[i];
                if (item.effect != null)
                {
                    item.effect.Stop();
                    item.effect.gameObject.SetActive(false);
                    vfxPool.Enqueue(item.effect);
                }
            }
            activeVfxList.Clear();
        }
    }
}
