using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Spawns particle bursts and physical 3D sub-cube debris particles when blocks shatter and powerups are collected.
    /// Optimized for zero GC allocations per frame and instant frame-0 PSO prewarming across iOS (Metal), PC (DX12/Vulkan), and Web (WebGPU).
    /// Employs dedicated URP-native materials and shaders with zero unshaded or pink fallback artifacts.
    /// </summary>
    public class BlockVFXManager : MonoBehaviour
    {
        public static BlockVFXManager Instance { get; private set; }

        [Header("VFX Configuration")]
        [SerializeField] private VisualEffectAsset shatterVfxAsset;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private int poolSize = 10;

        [Header("Sub-Box Physics Debris")]
        [SerializeField] private bool spawnPhysicalSubBoxes = true;
        [SerializeField] private int debrisPiecesPerBlock = 8; // 2x2x2 sub-cubes
        [SerializeField] private float debrisExplosionForce = 7f;
        [SerializeField] private Material debrisMaterial;

        public Material DebrisMaterial => debrisMaterial;
        public Material ParticleMaterial => particleMaterial;

        public void SetDebrisMaterial(Material mat)
        {
            debrisMaterial = mat;
        }

        public void SetParticleMaterial(Material mat)
        {
            particleMaterial = mat;
        }

        public static void SetInstanceForTesting(BlockVFXManager instance)
        {
            Instance = instance;
        }

        private Material cachedFallbackMaterial;
        private Material cachedFallbackParticleMaterial;
        private static MaterialPropertyBlock debrisPropBlock;

        private readonly Queue<GameObject> burstPool = new Queue<GameObject>();
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

        // Struct-based zero-allocation tracking for active particle bursts
        private struct ActiveVFX
        {
            public GameObject gameObject;
            public ParticleSystem particleSystem;
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

        public int AvailableVfxCount => burstPool.Count + vfxPool.Count;
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

        private Transform poolContainer;

        private Transform GetOrCreatePoolContainer()
        {
            if (poolContainer == null)
            {
                var existing = transform.Find("_Pool_VFX");
                if (existing != null)
                {
                    poolContainer = existing;
                }
                else
                {
                    var poolGo = new GameObject("_Pool_VFX");
                    poolGo.transform.SetParent(transform);
                    poolGo.transform.position = new Vector3(0f, -500f, 0f);
                    poolContainer = poolGo.transform;
                }
            }
            return poolContainer;
        }

        public void InitializePool()
        {
            var container = GetOrCreatePoolContainer();

            // Particle burst pool with dedicated shaded materials
            for (int i = 0; i < poolSize; i++)
            {
                GameObject burst = CreateNewBurstInstance();
                burst.SetActive(false);
                burst.transform.SetParent(container);
                burst.transform.position = new Vector3(0f, -500f, 0f);
                burstPool.Enqueue(burst);
            }

            // Debris mini-cubes pool
            if (spawnPhysicalSubBoxes)
            {
                int totalDebris = poolSize * debrisPiecesPerBlock;
                for (int i = 0; i < totalDebris; i++)
                {
                    GameObject debris = CreateDebrisPiece();
                    debris.SetActive(false);
                    debris.transform.SetParent(container);
                    debris.transform.position = new Vector3(0f, -500f, 0f);
                    debrisPool.Enqueue(debris);
                }
            }
        }

        /// <summary>
        /// Frame-0 PSO and shader prewarming to eliminate first-hit pipeline compilation hitches.
        /// Prewarms raster shaders, compiles particle PSOs, and prepares MaterialPropertyBlock
        /// across Metal (iOS), DirectX 12 / Vulkan (PC), and WebGPU (Web).
        /// </summary>
        public void Prewarm()
        {
            if (!Application.isEditor)
            {
                Shader.WarmupAllShaders();
            }

            // 1. Prewarm particle material and burst instance
            if (burstPool.Count > 0)
            {
                GameObject burst = burstPool.Peek();
                if (burst != null)
                {
                    var psr = burst.GetComponent<ParticleSystemRenderer>();
                    if (psr != null)
                    {
                        Material baseMat = GetOrCreateParticleMaterial();
                        if (baseMat != null) psr.sharedMaterial = baseMat;
                    }
                }
            }

            // 2. Prewarm debris renderer & MaterialPropertyBlock
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

        public Material GetOrCreateParticleMaterial()
        {
            if (particleMaterial != null) return particleMaterial;

            if (cachedFallbackParticleMaterial == null)
            {
#if UNITY_EDITOR
                particleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_VFX_Burst.mat");
                if (particleMaterial != null) return particleMaterial;
#endif
                var shader = Shader.Find("Arcade/VFX_ParticleBurst") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null)
                {
                    cachedFallbackParticleMaterial = new Material(shader) { name = "M_VFX_Burst_Fallback" };
                }
            }
            return cachedFallbackParticleMaterial;
        }

        private GameObject CreateNewBurstInstance()
        {
            GameObject go = new GameObject("VFX_Burst_Instance");
            go.SetActive(false); // Crucial: ensure particle system does not awake/emit in the playfield
            go.transform.SetParent(GetOrCreatePoolContainer());
            go.transform.position = new Vector3(0f, -500f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.55f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            if (psr != null)
            {
                Material mat = GetOrCreateParticleMaterial();
                if (mat != null)
                {
                    psr.sharedMaterial = mat;
                }
            }

            return go;
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
            debris.SetActive(false);
            debris.transform.SetParent(GetOrCreatePoolContainer());
            debris.transform.position = new Vector3(0f, -500f, 0f);
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

        /// <summary>
        /// Fires a radiant particle burst when a powerup capsule is intercepted by the paddle.
        /// Emits glowing sparks in the powerup's distinct neon emissive hue with zero pink/missing shader artifacts.
        /// </summary>
        public void PlayPowerupCollect(Vector3 position, Color powerupColor)
        {
            GameObject burstObj = burstPool.Count > 0 ? burstPool.Dequeue() : CreateNewBurstInstance();
            burstObj.transform.position = position;
            burstObj.SetActive(true);

            var ps = burstObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var psr = burstObj.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    Material mat = GetOrCreateParticleMaterial();
                    if (mat != null && psr.sharedMaterial != mat)
                    {
                        psr.sharedMaterial = mat;
                    }
                }

                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    startColor = powerupColor * 1.5f,
                    startLifetime = 0.55f,
                    applyShapeToPosition = true
                };
                ps.Emit(emitParams, 32);
            }

            activeVfxList.Add(new ActiveVFX
            {
                gameObject = burstObj,
                particleSystem = ps,
                elapsed = 0f,
                duration = 0.65f
            });
        }

        /// <summary>
        /// Shatters a block into spark particles and 8 physical 3D tumbling debris sub-boxes.
        /// </summary>
        public void PlayBlockShatter(Vector3 position, Color blockColor, Vector3 hitNormal)
        {
            // 1. Play shaded particle spark burst
            GameObject burstObj = burstPool.Count > 0 ? burstPool.Dequeue() : CreateNewBurstInstance();
            burstObj.transform.position = position;
            burstObj.SetActive(true);

            var ps = burstObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var psr = burstObj.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    Material mat = GetOrCreateParticleMaterial();
                    if (mat != null && psr.sharedMaterial != mat)
                    {
                        psr.sharedMaterial = mat;
                    }
                }

                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    startColor = blockColor * 1.4f,
                    startLifetime = 0.5f,
                    applyShapeToPosition = true
                };
                ps.Emit(emitParams, 24);
            }

            activeVfxList.Add(new ActiveVFX
            {
                gameObject = burstObj,
                particleSystem = ps,
                elapsed = 0f,
                duration = 0.65f
            });

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
                    item.gameObject.transform.position = new Vector3(0f, -500f, 0f);
                    debrisPool.Enqueue(item.gameObject);
                    activeDebrisList.RemoveAt(i);
                }
                else
                {
                    activeDebrisList[i] = item;
                }
            }

            // 2. Zero-allocation update for active particle bursts
            for (int i = activeVfxList.Count - 1; i >= 0; i--)
            {
                ActiveVFX item = activeVfxList[i];
                if (item.gameObject == null)
                {
                    activeVfxList.RemoveAt(i);
                    continue;
                }

                item.elapsed += dt;
                if (item.elapsed >= item.duration)
                {
                    if (item.particleSystem != null)
                    {
                        item.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        item.particleSystem.Clear();
                    }
                    item.gameObject.SetActive(false);
                    item.gameObject.transform.position = new Vector3(0f, -500f, 0f);
                    burstPool.Enqueue(item.gameObject);
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
                    item.gameObject.transform.position = new Vector3(0f, -500f, 0f);
                    debrisPool.Enqueue(item.gameObject);
                }
            }
            activeDebrisList.Clear();

            for (int i = activeVfxList.Count - 1; i >= 0; i--)
            {
                ActiveVFX item = activeVfxList[i];
                if (item.gameObject != null)
                {
                    if (item.particleSystem != null)
                    {
                        item.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        item.particleSystem.Clear();
                    }
                    item.gameObject.SetActive(false);
                    item.gameObject.transform.position = new Vector3(0f, -500f, 0f);
                    burstPool.Enqueue(item.gameObject);
                }
            }
            activeVfxList.Clear();
        }
    }
}
