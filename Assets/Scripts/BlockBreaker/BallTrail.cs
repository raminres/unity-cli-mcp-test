using UnityEngine;
using UnityEngine.Rendering;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Manages dual-layer stacked tapering trails for the ball in BlockBreaker.
    /// Layer 1 (Outer): Wider, translucent halo using the rich base hue.
    /// Layer 2 (Inner): Narrower, high-opacity hot core with elevated lightness.
    /// </summary>
    public class BallTrail : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private Material trailMaterial;
        [SerializeField] private Color baseColor = new Color(0f, 0.95f, 1f, 1f); // Electric Cyan
        [SerializeField] private float outerStartWidth = 0.55f;
        [SerializeField] private float innerStartWidth = 0.25f;
        [SerializeField] private float outerDuration = 0.22f;
        [SerializeField] private float innerDuration = 0.16f;
        [SerializeField] private float outerStartAlpha = 0.40f;
        [SerializeField] private float innerStartAlpha = 0.85f;
        [SerializeField] [Range(0f, 1f)] private float innerLightnessBlend = 0.65f;

        [Header("Renderers")]
        [SerializeField] private TrailRenderer outerTrail;
        [SerializeField] private TrailRenderer innerTrail;

        public TrailRenderer OuterTrail
        {
            get
            {
                if (outerTrail == null) EnsureTrailsCreated();
                return outerTrail;
            }
        }

        public TrailRenderer InnerTrail
        {
            get
            {
                if (innerTrail == null) EnsureTrailsCreated();
                return innerTrail;
            }
        }
        public Color BaseColor => baseColor;
        public Color InnerCoreColor => Color.Lerp(baseColor, Color.white, innerLightnessBlend);
        public float OuterStartWidth => outerStartWidth;
        public float InnerStartWidth => innerStartWidth;
        public float OuterDuration => outerDuration;
        public float InnerDuration => innerDuration;

        private void Awake()
        {
            EnsureTrailsCreated();
        }

        public void Initialize(Material mat, Color initialColor)
        {
            if (mat != null) trailMaterial = mat;
            baseColor = initialColor;
            EnsureTrailsCreated();
            SetTrailColor(initialColor);
        }

        public void EnsureTrailsCreated()
        {
            if (trailMaterial == null)
            {
#if UNITY_EDITOR
                trailMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_BallTrail.mat");
#endif
                if (trailMaterial == null)
                {
                    Shader trailShader = Shader.Find("Arcade/VFX_BallTrail") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (trailShader != null)
                    {
                        trailMaterial = new Material(trailShader);
                    }
                }
            }

            // Outer Trail setup
            if (outerTrail == null)
            {
                Transform outerChild = transform.Find("Trail_Outer");
                if (outerChild != null)
                {
                    outerTrail = outerChild.GetComponent<TrailRenderer>();
                }
                if (outerTrail == null)
                {
                    GameObject outerObj = new GameObject("Trail_Outer");
                    outerObj.transform.SetParent(transform, false);
                    outerObj.transform.localPosition = Vector3.zero;
                    outerTrail = outerObj.AddComponent<TrailRenderer>();
                }
            }
            ConfigureTrailRenderer(outerTrail, outerDuration, outerStartWidth);

            // Inner Trail setup
            if (innerTrail == null)
            {
                Transform innerChild = transform.Find("Trail_Inner");
                if (innerChild != null)
                {
                    innerTrail = innerChild.GetComponent<TrailRenderer>();
                }
                if (innerTrail == null)
                {
                    GameObject innerObj = new GameObject("Trail_Inner");
                    innerObj.transform.SetParent(transform, false);
                    innerObj.transform.localPosition = Vector3.zero;
                    innerTrail = innerObj.AddComponent<TrailRenderer>();
                }
            }
            ConfigureTrailRenderer(innerTrail, innerDuration, innerStartWidth);

            UpdateTrailColors();
            SetEmitting(false);
        }

        private void ConfigureTrailRenderer(TrailRenderer tr, float time, float startWidth)
        {
            if (tr == null) return;

            tr.time = time;
            tr.minVertexDistance = 0.05f;
            tr.numCornerVertices = 4;
            tr.numCapVertices = 4;
            tr.alignment = LineAlignment.View;
            tr.generateLightingData = false;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.autodestruct = false;

            if (trailMaterial != null)
            {
                tr.sharedMaterial = trailMaterial;
            }

            tr.widthCurve = BuildTaperCurve(startWidth);
        }

        private AnimationCurve BuildTaperCurve(float startWidth)
        {
            // Smooth quadratic decay curve from startWidth to 0 at t=1
            Keyframe k0 = new Keyframe(0f, startWidth, 0f, -startWidth * 1.5f);
            Keyframe k1 = new Keyframe(1f, 0f, 0f, 0f);
            return new AnimationCurve(k0, k1);
        }

        public void SetTrailColor(Color newColor)
        {
            baseColor = newColor;
            if (outerTrail == null || innerTrail == null)
            {
                EnsureTrailsCreated();
            }
            UpdateTrailColors();
        }

        private void UpdateTrailColors()
        {
            if (outerTrail != null)
            {
                Gradient outerGrad = new Gradient();
                outerGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(baseColor, 0.0f),
                        new GradientColorKey(baseColor, 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(outerStartAlpha, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                outerTrail.colorGradient = outerGrad;
            }

            if (innerTrail != null)
            {
                Color innerColor = Color.Lerp(baseColor, Color.white, innerLightnessBlend);
                Gradient innerGrad = new Gradient();
                innerGrad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(innerColor, 0.0f),
                        new GradientColorKey(baseColor, 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(innerStartAlpha, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                innerTrail.colorGradient = innerGrad;
            }
        }

        public void SetEmitting(bool emitting)
        {
            if (outerTrail == null || innerTrail == null)
            {
                EnsureTrailsCreated();
            }
            if (outerTrail != null) outerTrail.emitting = emitting;
            if (innerTrail != null) innerTrail.emitting = emitting;
        }

        public void Clear()
        {
            if (outerTrail != null) outerTrail.Clear();
            if (innerTrail != null) innerTrail.Clear();
        }
    }
}
