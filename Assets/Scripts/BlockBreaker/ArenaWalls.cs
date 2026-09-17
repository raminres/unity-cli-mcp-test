using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Coordinates the modular arena boundary walls, cleanly decoupling 3D aesthetic models
    /// from the flat, non-penetrating physics colliders for consistent, glitch-free bounces.
    /// </summary>
    public class ArenaWalls : MonoBehaviour
    {
        [Header("Wall Sections")]
        [SerializeField] private GameObject sideLeft;
        [SerializeField] private GameObject sideRight;
        [SerializeField] private GameObject top;
        [SerializeField] private GameObject chamferLeft;
        [SerializeField] private GameObject chamferRight;
        [SerializeField] private GameObject killZone;

        public GameObject SideLeft => sideLeft;
        public GameObject SideRight => sideRight;
        public GameObject Top => top;
        public GameObject ChamferLeft => chamferLeft;
        public GameObject ChamferRight => chamferRight;
        public GameObject KillZone => killZone;

        public BoxCollider SideLeftCollider => sideLeft != null ? sideLeft.GetComponentInChildren<BoxCollider>() : null;
        public BoxCollider SideRightCollider => sideRight != null ? sideRight.GetComponentInChildren<BoxCollider>() : null;
        public BoxCollider TopCollider => top != null ? top.GetComponentInChildren<BoxCollider>() : null;
        public BoxCollider ChamferLeftCollider => chamferLeft != null ? chamferLeft.GetComponentInChildren<BoxCollider>() : null;
        public BoxCollider ChamferRightCollider => chamferRight != null ? chamferRight.GetComponentInChildren<BoxCollider>() : null;
        public BoxCollider KillZoneCollider => killZone != null ? killZone.GetComponent<BoxCollider>() : null;

        public MeshRenderer SideLeftRenderer => sideLeft != null ? sideLeft.GetComponentInChildren<MeshRenderer>() : null;
        public MeshRenderer SideRightRenderer => sideRight != null ? sideRight.GetComponentInChildren<MeshRenderer>() : null;
        public MeshRenderer TopRenderer => top != null ? top.GetComponentInChildren<MeshRenderer>() : null;
        public MeshRenderer ChamferLeftRenderer => chamferLeft != null ? chamferLeft.GetComponentInChildren<MeshRenderer>() : null;
        public MeshRenderer ChamferRightRenderer => chamferRight != null ? chamferRight.GetComponentInChildren<MeshRenderer>() : null;

        public void EnsureReferences()
        {
            if (sideLeft == null) sideLeft = transform.Find("side left")?.gameObject;
            if (sideRight == null) sideRight = transform.Find("side right")?.gameObject;
            if (top == null) top = transform.Find("top")?.gameObject;
            if (chamferLeft == null) chamferLeft = transform.Find("chamfer left")?.gameObject;
            if (chamferRight == null) chamferRight = transform.Find("chamfer right")?.gameObject;
            if (killZone == null) killZone = transform.Find("KillZone")?.gameObject;
        }

        private void Awake()
        {
            EnsureReferences();
        }
    }
}
