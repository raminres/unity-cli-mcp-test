using System.Collections.Generic;
using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Manages an off-screen pool of world-space floating score popups.
    /// Spawns directly at brick collision coordinates, drifting upward and fading over 0.65s.
    /// Provides immediate, tactile visual feedback for points, combos, bomb chains, and clutch scores.
    /// </summary>
    public class FloatingScoreManager : MonoBehaviour
    {
        public static FloatingScoreManager Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private int initialPoolSize = 16;
        [SerializeField] private float floatSpeed = 2.2f;
        [SerializeField] private float lifetime = 0.65f;
        [SerializeField] private Font customFont;

        private readonly List<FloatingScoreItem> activeItems = new List<FloatingScoreItem>();
        private readonly Queue<FloatingScoreItem> pool = new Queue<FloatingScoreItem>();
        private Transform poolContainer;

        private class FloatingScoreItem
        {
            public GameObject gameObject;
            public TextMesh textMesh;
            public MeshRenderer renderer;
            public float elapsed;
            public Vector3 initialPos;
            public Color baseColor;
        }

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

        private void OnEnable()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnBlockPointsAwarded -= HandleBlockPointsAwarded;
                ArcadeGameManager.Instance.OnBlockPointsAwarded += HandleBlockPointsAwarded;
            }
        }

        private void OnDisable()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnBlockPointsAwarded -= HandleBlockPointsAwarded;
            }
        }

        private void InitializePool()
        {
            if (poolContainer == null)
            {
                var containerGo = new GameObject("_Pool_FloatingScores");
                containerGo.transform.SetParent(transform);
                containerGo.transform.position = new Vector3(0f, -500f, 0f);
                poolContainer = containerGo.transform;
            }

            if (customFont == null)
            {
#if UNITY_EDITOR
                customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/FT_Montserrat.ttf");
#endif
                if (customFont == null)
                {
                    customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                var item = CreateScoreItem();
                item.gameObject.SetActive(false);
                pool.Enqueue(item);
            }
        }

        private FloatingScoreItem CreateScoreItem()
        {
            var go = new GameObject("FloatingScore_Item");
            go.transform.SetParent(poolContainer);

            var tm = go.AddComponent<TextMesh>();
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = 32;
            tm.characterSize = 0.085f;
            if (customFont != null)
            {
                tm.font = customFont;
            }

            var mr = go.GetComponent<MeshRenderer>();
            if (customFont != null && customFont.material != null)
            {
                mr.sharedMaterial = customFont.material;
            }

            return new FloatingScoreItem
            {
                gameObject = go,
                textMesh = tm,
                renderer = mr,
                elapsed = 0f,
                initialPos = Vector3.zero,
                baseColor = Color.white
            };
        }

        public void HandleBlockPointsAwarded(Vector3 worldPos, int points, int comboMult, string tag)
        {
            // Primary flying score label animation is handled directly in UI Toolkit by ArcadeUIManager.AnimateFlyingScore.
        }

        public void SpawnScorePopup(Vector3 worldPos, int points, int comboMult, string tag)
        {
            FloatingScoreItem item = pool.Count > 0 ? pool.Dequeue() : CreateScoreItem();

            // Set text formatted string
            if (!string.IsNullOrEmpty(tag))
            {
                item.textMesh.text = $"+{points}\n{tag}";
            }
            else if (comboMult > 1)
            {
                item.textMesh.text = $"+{points} x{comboMult}!";
            }
            else
            {
                item.textMesh.text = $"+{points}";
            }

            // Determine vibrant color tint
            Color color;
            if (!string.IsNullOrEmpty(tag) && tag.Contains("CLUTCH"))
            {
                color = new Color(1f, 0.78f, 0.1f); // Radiant Amber
            }
            else if (!string.IsNullOrEmpty(tag) && tag.Contains("BOMB"))
            {
                color = new Color(1f, 0.35f, 0.1f); // Fiery Orange
            }
            else if (comboMult >= 4)
            {
                color = new Color(1f, 0.18f, 0.55f); // Hot Magenta
            }
            else if (comboMult >= 2)
            {
                color = new Color(0f, 0.95f, 1f); // Electric Cyan
            }
            else
            {
                color = new Color(1f, 0.92f, 0.4f); // Golden Yellow
            }

            item.baseColor = color;
            item.textMesh.color = color;
            item.elapsed = 0f;

            // Place in front of bricks (Z = -0.8f)
            Vector3 spawnPos = new Vector3(worldPos.x, worldPos.y + 0.3f, -0.8f);
            item.initialPos = spawnPos;
            item.gameObject.transform.position = spawnPos;
            item.gameObject.transform.localScale = Vector3.one;
            item.gameObject.SetActive(true);

            activeItems.Add(item);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                var item = activeItems[i];
                item.elapsed += dt;
                float progress = Mathf.Clamp01(item.elapsed / lifetime);

                // Float upward with subtle easing
                float yOffset = floatSpeed * Mathf.Sin(progress * (Mathf.PI * 0.5f));
                item.gameObject.transform.position = new Vector3(item.initialPos.x, item.initialPos.y + yOffset, item.initialPos.z);

                // Gentle pop scale-up then settle
                float scale = 1f + 0.25f * Mathf.Sin(progress * Mathf.PI);
                item.gameObject.transform.localScale = Vector3.one * scale;

                // Alpha fadeout towards end of lifetime
                float alpha = 1f - (progress * progress);
                Color c = item.baseColor;
                c.a = alpha;
                item.textMesh.color = c;

                if (item.elapsed >= lifetime)
                {
                    activeItems.RemoveAt(i);
                    item.gameObject.SetActive(false);
                    item.gameObject.transform.SetParent(poolContainer);
                    item.gameObject.transform.position = new Vector3(0f, -500f, 0f);
                    pool.Enqueue(item);
                }
            }
        }
    }
}
