using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Spawns GPU VFX Graph bursts and physical 3D sub-cube debris particles when blocks shatter.
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

        private readonly Queue<VisualEffect> vfxPool = new Queue<VisualEffect>();
        private readonly Queue<GameObject> debrisPool = new Queue<GameObject>();

        private static readonly int ColorPropertyId = Shader.PropertyToID("BlockColor");
        private static readonly int NormalPropertyId = Shader.PropertyToID("HitNormal");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializePool();
        }

        private void InitializePool()
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
                for (int i = 0; i < poolSize * debrisPiecesPerBlock; i++)
                {
                    GameObject debris = CreateDebrisPiece();
                    debris.SetActive(false);
                    debrisPool.Enqueue(debris);
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

        private GameObject CreateDebrisPiece()
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = "SubBox_Debris";
            debris.transform.SetParent(transform);
            debris.transform.localScale = Vector3.one * 0.45f;

            // Remove default collider so particles don't interfere with ball physics
            Collider col = debris.GetComponent<Collider>();
            if (col != null) Destroy(col);

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
            StartCoroutine(RecycleVfxRoutine(effect, 1.2f));

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

            foreach (float ox in offsets)
            {
                foreach (float oy in offsets)
                {
                    foreach (float oz in offsets)
                    {
                        GameObject debris = debrisPool.Count > 0 ? debrisPool.Dequeue() : CreateDebrisPiece();
                        Vector3 subPos = centerPos + new Vector3(ox, oy, oz);
                        debris.transform.position = subPos;
                        debris.transform.localScale = Vector3.one * 0.42f;
                        debris.transform.rotation = Random.rotation;

                        MeshRenderer mr = debris.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            mr.material.color = blockColor;
                            if (mr.material.HasProperty("_EmissionColor"))
                            {
                                mr.material.EnableKeyword("_EMISSION");
                                mr.material.SetColor("_EmissionColor", blockColor * 1.3f);
                            }
                        }

                        debris.SetActive(true);

                        // Velocity direction: outward spherical bias + incoming hit direction
                        Vector3 outward = (new Vector3(ox, oy, oz)).normalized;
                        Vector3 burstVel = (outward * 0.7f + hitNormal * 0.3f + Random.insideUnitSphere * 0.3f).normalized * debrisExplosionForce;
                        Vector3 rotSpeed = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-360f, 360f));

                        StartCoroutine(AnimateDebrisPiece(debris, burstVel, rotSpeed, 0.75f));
                    }
                }
            }
        }

        private IEnumerator AnimateDebrisPiece(GameObject debris, Vector3 velocity, Vector3 rotationSpeed, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = debris.transform.localScale;
            Vector3 gravity = new Vector3(0f, -14f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                velocity += gravity * Time.deltaTime;
                debris.transform.position += velocity * Time.deltaTime;
                debris.transform.Rotate(rotationSpeed * Time.deltaTime);

                // Shrink out smoothly towards the end
                float scaleFactor = Mathf.Lerp(1f, 0f, Mathf.Pow(t, 2f));
                debris.transform.localScale = startScale * scaleFactor;

                yield return null;
            }

            debris.SetActive(false);
            debrisPool.Enqueue(debris);
        }

        private IEnumerator RecycleVfxRoutine(VisualEffect effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null)
            {
                effect.Stop();
                effect.gameObject.SetActive(false);
                vfxPool.Enqueue(effect);
            }
        }
    }
}
