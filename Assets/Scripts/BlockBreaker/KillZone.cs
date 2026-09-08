using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Component designating a trigger volume as the bottom death/life-loss boundary.
    /// Avoids reliance on Unity tag definitions to prevent CompareTag errors.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }
}
