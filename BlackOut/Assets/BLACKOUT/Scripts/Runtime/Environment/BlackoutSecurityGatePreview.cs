using UnityEngine;

namespace Blackout.Environment
{
    /// <summary>Environment art preview only. Does not consume power cells or implement combat.</summary>
    [DisallowMultipleComponent]
    public sealed class BlackoutSecurityGatePreview : MonoBehaviour
    {
        [SerializeField] private Transform southDoor;
        [SerializeField] private Transform northDoor;
        [SerializeField] private GameObject lockedIndicators;
        [SerializeField] private GameObject poweredIndicators;
        [SerializeField] private GameObject turretActiveIndicators;
        [SerializeField] private bool powered;

        public bool Powered => powered;

        public void Configure(Transform south, Transform north, GameObject locked, GameObject live, GameObject turret)
        {
            southDoor = south; northDoor = north;
            lockedIndicators = locked; poweredIndicators = live; turretActiveIndicators = turret;
            SetPowered(false);
        }

        [ContextMenu("Preview / Blackout (closed, turret off)")]
        public void PreviewBlackout() => SetPowered(false);

        [ContextMenu("Preview / Powered (open, turret on)")]
        public void PreviewPowered() => SetPowered(true);

        public void SetPowered(bool value)
        {
            powered = value;
            // Positions are local to the gate assembly, not tied to scene coordinates.
            if (southDoor) southDoor.localPosition = new Vector3(0, 0, powered ? -3.05f : 0);
            if (northDoor) northDoor.localPosition = new Vector3(0, 0, powered ? 3.05f : 0);
            if (lockedIndicators) lockedIndicators.SetActive(!powered);
            if (poweredIndicators) poweredIndicators.SetActive(powered);
            if (turretActiveIndicators) turretActiveIndicators.SetActive(powered);
        }

        private void OnValidate() => SetPowered(powered);
    }
}
