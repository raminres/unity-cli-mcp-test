using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Controls the dynamic playfield background plane/quad, randomly assigning
    /// high-resolution cosmic gradient textures (TX_Background_Gradient_A through D)
    /// across levels to keep gameplay environments fresh and visually stunning.
    /// </summary>
    public class LevelBackgroundController : MonoBehaviour
    {
        [Header("Background Textures")]
        [SerializeField] private Texture2D[] backgroundTextures;

        [Header("Rendering Components")]
        [SerializeField] private MeshRenderer meshRenderer;

        private MaterialPropertyBlock propBlock;
        private int currentTextureIndex = -1;
        private Texture2D currentTexture;

        private static readonly int BaseMapProp = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");

        public static LevelBackgroundController Instance { get; private set; }

        public Texture2D[] BackgroundTextures => backgroundTextures;
        public int CurrentTextureIndex => currentTextureIndex;
        public Texture2D CurrentTexture => currentTexture;
        public MeshRenderer TargetRenderer => meshRenderer;

        private void Awake()
        {
            Instance = this;

            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

#if UNITY_EDITOR
            if (backgroundTextures == null || backgroundTextures.Length == 0)
            {
                LoadEditorTextures();
            }
#endif

            if (currentTexture == null && backgroundTextures != null && backgroundTextures.Length > 0)
            {
                RandomizeBackground(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetTextures(Texture2D[] textures)
        {
            backgroundTextures = textures;
            if (backgroundTextures != null && backgroundTextures.Length > 0)
            {
                RandomizeBackground(false);
            }
        }

        /// <summary>
        /// Randomly selects one of the gradient textures.
        /// If avoidSameAsCurrent is true and multiple textures exist, picks a different texture from the current one.
        /// </summary>
        public void RandomizeBackground(bool avoidSameAsCurrent = true)
        {
            if (backgroundTextures == null || backgroundTextures.Length == 0) return;

            if (backgroundTextures.Length == 1)
            {
                ApplyTexture(backgroundTextures[0], 0);
                return;
            }

            int newIndex = currentTextureIndex;
            int attempts = 0;
            while (attempts < 10)
            {
                newIndex = Random.Range(0, backgroundTextures.Length);
                if (!avoidSameAsCurrent || newIndex != currentTextureIndex || backgroundTextures.Length <= 1)
                {
                    break;
                }
                attempts++;
            }

            ApplyTexture(backgroundTextures[newIndex], newIndex);
        }

        /// <summary>
        /// Explicitly assigns the background gradient by index.
        /// </summary>
        public void SetBackgroundByIndex(int index)
        {
            if (backgroundTextures == null || backgroundTextures.Length == 0) return;
            index = Mathf.Clamp(index, 0, backgroundTextures.Length - 1);
            ApplyTexture(backgroundTextures[index], index);
        }

        /// <summary>
        /// Explicitly assigns a custom background texture.
        /// </summary>
        public void SetBackground(Texture2D texture)
        {
            int index = -1;
            if (backgroundTextures != null)
            {
                for (int i = 0; i < backgroundTextures.Length; i++)
                {
                    if (backgroundTextures[i] == texture)
                    {
                        index = i;
                        break;
                    }
                }
            }
            ApplyTexture(texture, index);
        }

        private void ApplyTexture(Texture2D texture, int index)
        {
            currentTexture = texture;
            currentTextureIndex = index;

            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null && texture != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetTexture(BaseMapProp, texture);
                propBlock.SetTexture(MainTexProp, texture);
                meshRenderer.SetPropertyBlock(propBlock);
            }
        }

#if UNITY_EDITOR
        private void LoadEditorTextures()
        {
            var texA = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_A.png");
            var texB = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_B.png");
            var texC = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_C.png");
            var texD = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_D.png");

            var list = new System.Collections.Generic.List<Texture2D>();
            if (texA != null) list.Add(texA);
            if (texB != null) list.Add(texB);
            if (texC != null) list.Add(texC);
            if (texD != null) list.Add(texD);

            backgroundTextures = list.ToArray();
        }
#endif
    }
}
