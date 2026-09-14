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

        private PaddleController paddle;
        private bool isBlasterActive = false;
        private float blasterTimer = 0f;
        private float nextFireTime = 0f;

        private bool isHyperBeamActive = false;
        private float hyperBeamTimer = 0f;
        private GameObject hyperBeamObject;
        private static Material hyperBeamMaterial;

        public bool IsBlasterActive => isBlasterActive;
        public float BlasterTimeRemaining => Mathf.Max(0f, blasterTimer);
        public bool IsHyperBeamActive => isHyperBeamActive;
        public float HyperBeamTimeRemaining => Mathf.Max(0f, hyperBeamTimer);
        public float BeamWidth => beamWidth;
        public float HyperBeamDuration => hyperBeamDuration;

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
            EnsureHyperBeamObject();
            UpdateHyperBeamPosition();
            if (hyperBeamObject != null)
            {
                hyperBeamObject.SetActive(true);
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayLaserShoot();
            }

            // Immediately vaporize any blocks in current alignment
            PerformHyperBeamSlice();
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
                    isHyperBeamActive = false;
                    if (hyperBeamObject != null)
                    {
                        hyperBeamObject.SetActive(false);
                    }
                }
                else
                {
                    UpdateHyperBeamPosition();
                    PerformHyperBeamSlice();
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
            if (hyperBeamObject == null)
            {
                hyperBeamObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hyperBeamObject.name = "VFX_Railgun_HyperBeam";
                hyperBeamObject.transform.SetParent(transform);

                // Disable default collider so ball doesn't bounce off beam mesh
                var col = hyperBeamObject.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                var rend = hyperBeamObject.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = GetOrCreateHyperBeamMaterial();
                }
            }
        }

        private void UpdateHyperBeamPosition()
        {
            if (hyperBeamObject == null) return;
            // Center the beam vertically from paddle top deck (Y = -6) up to ceiling (Y = 24.5)
            float centerY = (beamHeight * 0.5f) + 0.4f;
            float lossyX = transform.lossyScale.x > 0.001f ? transform.lossyScale.x : 1f;
            float lossyY = transform.lossyScale.y > 0.001f ? transform.lossyScale.y : 1f;
            hyperBeamObject.transform.localPosition = new Vector3(0f, centerY, 0f);
            hyperBeamObject.transform.localScale = new Vector3(beamWidth / lossyX, beamHeight / lossyY, 1.2f);
        }

        private void PerformHyperBeamSlice()
        {
            Vector3 beamCenter = transform.position + new Vector3(0f, (beamHeight * 0.5f) + 0.4f, 0f);
            Vector3 halfExtents = new Vector3(beamWidth * 0.5f, beamHeight * 0.5f, 1.5f);

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

        private static Material GetOrCreateHyperBeamMaterial()
        {
            if (hyperBeamMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Arcade/VFX_ParticleBurst")
                    ?? Shader.Find("Sprites/Default");

                hyperBeamMaterial = new Material(shader)
                {
                    name = "M_Railgun_HyperBeam_URP"
                };

                Color beamCol = new Color(1f, 0.15f, 0.3f, 0.75f); // High-luminance neon ruby
                hyperBeamMaterial.SetColor("_BaseColor", beamCol);
                hyperBeamMaterial.SetColor("_Color", beamCol);
                if (hyperBeamMaterial.HasProperty("_EmissionColor"))
                {
                    hyperBeamMaterial.EnableKeyword("_EMISSION");
                    hyperBeamMaterial.SetColor("_EmissionColor", beamCol * 3.0f);
                }
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
                    isHyperBeamActive = false;
                    if (hyperBeamObject != null)
                    {
                        hyperBeamObject.SetActive(false);
                    }
                }
            }
        }
    }
}
