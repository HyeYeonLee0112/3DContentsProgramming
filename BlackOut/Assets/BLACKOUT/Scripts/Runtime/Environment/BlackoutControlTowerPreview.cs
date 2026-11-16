using UnityEngine;

namespace Blackout.Environment
{
    /// <summary>Environment art states only. Does not spend charge, damage actors or run boss timers.</summary>
    [DisallowMultipleComponent]
    public sealed class BlackoutControlTowerPreview : MonoBehaviour
    {
        [SerializeField] private GameObject leftWorklights, rightWorklights, leftTurretAlert, rightTurretAlert;
        [SerializeField] private GameObject coolingActive, coolingIdle, restorationActive, restorationIdle;
        [SerializeField] private bool leftPowered, rightPowered, cooling, restored;
        public bool LeftPowered => leftPowered;
        public bool RightPowered => rightPowered;
        public bool Cooling => cooling;
        public bool Restored => restored;

        public void Configure(GameObject left, GameObject right, GameObject leftAlert, GameObject rightAlert,
            GameObject coolingOn, GameObject coolingOff, GameObject restoredOn, GameObject restoredOff)
        {
            leftWorklights=left; rightWorklights=right; leftTurretAlert=leftAlert; rightTurretAlert=rightAlert;
            coolingActive=coolingOn; coolingIdle=coolingOff; restorationActive=restoredOn; restorationIdle=restoredOff;
            Apply();
        }

        public void SetState(bool left, bool right, bool cool, bool cityRestored)
        {
            leftPowered=left; rightPowered=right; cooling=cool; restored=cityRestored; Apply();
        }
        private void OnEnable() => Apply();
        private void Apply()
        {
            Set(leftWorklights,leftPowered); Set(rightWorklights,rightPowered);
            Set(leftTurretAlert,leftPowered); Set(rightTurretAlert,rightPowered);
            Set(coolingActive,cooling); Set(coolingIdle,!cooling);
            Set(restorationActive,restored); Set(restorationIdle,!restored);
        }
        private static void Set(GameObject target,bool active) { if(target)target.SetActive(active); }
        [ContextMenu("미리보기/정전")] public void PreviewBlackout() => SetState(false,false,false,false);
        [ContextMenu("미리보기/왼쪽 작업등과 포탑")] public void PreviewLeft() => SetState(true,false,false,false);
        [ContextMenu("미리보기/양쪽 작업등과 포탑")] public void PreviewWorklights() => SetState(true,true,false,false);
        [ContextMenu("미리보기/중앙 냉각 가동")] public void PreviewCooling() => SetState(true,true,true,false);
        [ContextMenu("미리보기/도시 송전 복구")] public void PreviewRestored() => SetState(true,true,false,true);
    }
}
