using System.Collections;
using Arcade.Audio;
using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Mounts to the paddle to control twin laser blaster cannons (Powerup mode)
    /// and the wide-aperture Hyper-Beam Railgun (Clutch Overcharge mode).
    /// </summary>
    public class PaddleLaserController : MonoBehaviour
    {
        [Header("Blaster Powerup Settings")]
        [SerializeField] private float fireRateInterval = 0.32f;
        [SerializeField] private float mountSpacingRatio = 0.38f; // Offset fraction of paddle width

        [Header("Railgun Hyper-Beam Settings")]
        [SerializeField] private float beamWidth = 3.2f;
        [SerializeField] private float beamHeight = 31f;
        [SerializeField] private float hyperBeamDuration = 5.0f;
        [SerializeField] private float beamSurgeDuration = 0.35f; // Duration for beam to extend from paddle to arena ceiling
        [SerializeField] private Material hyperBeamMaterialAsset;

        private PaddleController paddle;
        private bool isBlasterActive = false;
        private float blasterTimer = 0f;
        private float nextFireTime = 0f;

        private bool isHyperBeamActive = false;
        private float hyperBeamTimer = 0f;
        private float currentBeamHeight = 0f;
        private float beamSurgeProgress = 0f;
        private GameObject hyperBeamObject;
        private static Material hyperBeamMaterial;

        public Material HyperBeamMaterialAsset => hyperBeamMaterialAsset;

        public bool IsBlasterActive => isBlasterActive;
        public float BlasterTimeRemaining => Mathf.Max(0f, blasterTimer);
        public bool IsHyperBeamActive => isHyperBeamActive;
        public float HyperBeamTimeRemaining => Mathf.Max(0f, hyperBeamTimer);
        public float BeamWidth => beamWidth;
        public float HyperBeamDuration => hyperBeamDuration;
        public float BeamSurgeDuration => beamSurgeDuration;
        public float CurrentBeamHeight => currentBeamHeight;

        private void Awake()
        {
            if (paddle == null) paddle = GetComponent<PaddleController>();
            DeactivateAllWeapons();
        }

        private void OnEnable()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                ArcadeGameManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        private void Start()
        {
            if (paddle == null) paddle = GetComponent<PaddleController>();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                ArcadeGameManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
            DeactivateAllWeapons();
        }

        public void HandleGameStateChangedDirect(GameState state)
        {
            HandleGameStateChanged(state);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state != GameState.Playing && state != GameState.Paused)
            {
                DeactivateAllWeapons();
            }
        }

        public void ActivateLaserBlaster(float duration = 10f)
        {
            isBlasterActive = true;
            blasterTimer = Mathf.Max(blasterTimer, duration);
            nextFireTime = 0f; // Fire immediately upon collection
        }

        public void DeactivateLaserBlaster()
        {
            isBlasterActive = false;
            blasterTimer = 0f;
        }

        public void DeactivateHyperBeam()
        {
            isHyperBeamActive = false;
            hyperBeamTimer = 0f;
            beamSurgeProgress = 0f;
            currentBeamHeight = 0f;
            if (hyperBeamObject != null)
            {
                hyperBeamObject.SetActive(false);
            }
        }

        public void DeactivateAllWeapons()
        {
            DeactivateLaserBlaster();
            DeactivateHyperBeam();
        }

        public void FireRailgunHyperBeam(float duration = 5.0f)
        {
            isHyperBeamActive = true;
            hyperBeamTimer = duration > 0f ? duration : hyperBeamDuration;
            beamSurgeProgress = 0f;
            currentBeamHeight = 0.5f;

            EnsureHyperBeamObject();
            UpdateHyperBeamPosition(currentBeamHeight);
            if (hyperBeamObject != null)
            {
                hyperBeamObject.SetActive(true);
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayLaserShoot();
            }

            // Immediately test any blocks in initial muzzle alignment
            PerformHyperBeamSlice(currentBeamHeight);
        }

        private void Update()
        {
            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State != GameState.Playing)
            {
                DeactivateAllWeapons();
                return;
            }

            float dt = Time.deltaTime;

            // Handle Laser Blaster Powerup
            if (isBlasterActive)
            {
                blasterTimer -= dt;
                if (blasterTimer <= 0f)
                {
                    DeactivateLaserBlaster();
                }
                else
                {
                    nextFireTime -= dt;
                    if (nextFireTime <= 0f)
                    {
                        FireTwinBlasters();
                        nextFireTime = fireRateInterval;
                    }
                }
            }

            // Handle Hyper-Beam Railgun
            if (isHyperBeamActive)
            {
                hyperBeamTimer -= dt;
                if (hyperBeamTimer <= 0f)
                {
                    DeactivateHyperBeam();
                }
                else
                {
                    // Progressive surge animation from paddle to ceiling
                    if (beamSurgeProgress < 1.0f)
                    {
                        beamSurgeProgress = Mathf.Min(1.0f, beamSurgeProgress + (dt / beamSurgeDuration));
                        float smoothT = Mathf.SmoothStep(0f, 1f, beamSurgeProgress);
                        currentBeamHeight = Mathf.Lerp(0.5f, beamHeight, smoothT);
                    }
                    else
                    {
                        currentBeamHeight = beamHeight;
                    }

                    UpdateHyperBeamPosition(currentBeamHeight);
                    PerformHyperBeamSlice(currentBeamHeight);
                }
            }
        }

        private void FireTwinBlasters()
        {
            if (paddle == null) paddle = GetComponent<PaddleController>();
            float halfWidth = paddle != null ? paddle.Width * 0.5f * mountSpacingRatio : 1.2f;
            Vector3 center = transform.position;

            Vector3 leftSpawn = new Vector3(center.x - halfWidth, center.y + 0.45f, 0f);
            Vector3 rightSpawn = new Vector3(center.x + halfWidth, center.y + 0.45f, 0f);

            LaserBolt.Spawn(leftSpawn);
            LaserBolt.Spawn(rightSpawn);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayLaserShoot();
            }
        }

        private void EnsureHyperBeamObject()
        {
            if (hyperBeamObject != null)
            {
                var filter = hyperBeamObject.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name.Contains("Cube"))
                {
                    DestroyImmediate(hyperBeamObject);
                    hyperBeamObject = null;
                }
            }

            if (hyperBeamObject == null)
            {
                hyperBeamObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                hyperBeamObject.name = "VFX_Railgun_HyperBeam";
                hyperBeamObject.transform.SetParent(transform);

                // Disable default collider so ball doesn't bounce off beam mesh
                var col = hyperBeamObject.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                var rend = hyperBeamObject.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = GetOrCreateHyperBeamMaterial(hyperBeamMaterialAsset);
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows = false;
                }
            }
        }

        private void UpdateHyperBeamPosition(float height)
        {
            if (hyperBeamObject == null) return;
            // Center the beam vertically from paddle top strike deck (Y = -6.0) upwards
            float centerY = (height * 0.5f) + 0.38f;
            float lossyX = transform.lossyScale.x > 0.001f ? transform.lossyScale.x : 1f;
            float lossyY = transform.lossyScale.y > 0.001f ? transform.lossyScale.y : 1f;
            hyperBeamObject.transform.localPosition = new Vector3(0f, centerY, -0.3f);
            hyperBeamObject.transform.localScale = new Vector3(beamWidth / lossyX, height / lossyY, 1f);
        }

        private void PerformHyperBeamSlice(float height)
        {
            Vector3 beamCenter = transform.position + new Vector3(0f, (height * 0.5f) + 0.38f, 0f);
            Vector3 halfExtents = new Vector3(beamWidth * 0.5f, height * 0.5f, 1.5f);

            Collider[] hits = Physics.OverlapBox(beamCenter, halfExtents, Quaternion.identity);
            for (int i = 0; i < hits.Length; i++)
            {
                var block = hits[i].GetComponent<Block>() ?? hits[i].GetComponentInParent<Block>();
                if (block != null && !block.IsDestroyed)
                {
                    block.TakeHit(Vector3.down);

                    if (BlockVFXManager.Instance != null)
                    {
                        BlockVFXManager.Instance.PlayPowerupCollect(hits[i].transform.position, new Color(1f, 0.2f, 0.35f, 1f));
                    }
                }
            }
        }

        public static Material GetOrCreateHyperBeamMaterial(Material assignedAsset = null)
        {
            if (assignedAsset != null)
            {
                hyperBeamMaterial = assignedAsset;
                return hyperBeamMaterial;
            }

            if (hyperBeamMaterial != null) return hyperBeamMaterial;

#if UNITY_EDITOR
            var editorMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_LaserHyperBeam.mat");
            if (editorMat != null)
            {
                hyperBeamMaterial = editorMat;
                return hyperBeamMaterial;
            }
#endif

            var shader = Shader.Find("Arcade/VFX_LaserHyperBeam")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Arcade/VFX_BallTrail")
                ?? Shader.Find("Sprites/Default");

            hyperBeamMaterial = new Material(shader)
            {
                name = "M_Railgun_HyperBeam_URP"
            };

            Color beamCol = new Color(1f, 0.15f, 0.35f, 0.85f);
            hyperBeamMaterial.SetColor("_BaseColor", beamCol);
            hyperBeamMaterial.SetColor("_Color", beamCol);

#if UNITY_EDITOR
            var gradientTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/TX_LaserHyperBeam_Gradient.png");
            if (gradientTex != null)
            {
                hyperBeamMaterial.SetTexture("_MainTex", gradientTex);
                hyperBeamMaterial.SetTexture("_BaseMap", gradientTex);
            }
#endif

            if (hyperBeamMaterial.HasProperty("_EmissionColor"))
            {
                hyperBeamMaterial.EnableKeyword("_EMISSION");
                hyperBeamMaterial.SetColor("_EmissionColor", beamCol * 3.0f);
            }
            return hyperBeamMaterial;
        }

        private void OnDisable()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
            DeactivateAllWeapons();
        }

        public void SimulateStepForTesting(float dt)
        {
            if (isBlasterActive)
            {
                blasterTimer -= dt;
                if (blasterTimer <= 0f)
                {
                    DeactivateLaserBlaster();
                }
                else
                {
                    nextFireTime -= dt;
                    if (nextFireTime <= 0f)
                    {
                        FireTwinBlasters();
                        nextFireTime = fireRateInterval;
                    }
                }
            }

            if (isHyperBeamActive)
            {
                hyperBeamTimer -= dt;
                if (hyperBeamTimer <= 0f)
                {
                    DeactivateHyperBeam();
                }
                else
                {
                    if (beamSurgeProgress < 1.0f)
                    {
                        beamSurgeProgress = Mathf.Min(1.0f, beamSurgeProgress + (dt / beamSurgeDuration));
                        float smoothT = Mathf.SmoothStep(0f, 1f, beamSurgeProgress);
                        currentBeamHeight = Mathf.Lerp(0.5f, beamHeight, smoothT);
                    }
                    else
                    {
                        currentBeamHeight = beamHeight;
                    }

                    UpdateHyperBeamPosition(currentBeamHeight);
                    PerformHyperBeamSlice(currentBeamHeight);
                }
            }
        }
    }
}
