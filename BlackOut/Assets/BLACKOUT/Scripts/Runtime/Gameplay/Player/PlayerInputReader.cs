using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

//입력 스냅샷 구조체
public readonly struct PlayerInputSnapshot
{
    //필드
    public Vector2 Move { get; }
    public Vector2 Look { get; }
    public bool IsAiming { get; }
    public bool JumpPressed { get; }
    public bool FirePressed { get; }
    public bool InteractPressed { get; }

    // 아무 입력도 없는 기본값
    public static PlayerInputSnapshot None => new PlayerInputSnapshot(
        Vector2.zero, Vector2 .zero, false, false, false, false );

    //생성자
    public PlayerInputSnapshot(
        Vector2 move,
        Vector2 look,
        bool isAiming,
        bool jumpPressed,
        bool firePressed,
        bool interactPressed)
    {
        Move = move;
        Look = look;
        IsAiming = isAiming;
        JumpPressed = jumpPressed;
        FirePressed = firePressed;
        InteractPressed = interactPressed;

    }

}
public class PlayerInputReader : MonoBehaviour, BlackoutInputActions.IGameplayActions
{
    private BlackoutInputActions _actions;

    private Vector2 _move;
    private Vector2 _look;
    private bool _isAiming;
    private bool _jumpRequested;
    private bool _fireRequested;
    private bool _interactRequested;
    private bool _gameplayEnabled;


    void Awake()
    {
        _actions = new BlackoutInputActions();
        //Gameplay 그룹의 입력이 발생하면
        //현재 PlayerInputReader 객체의 메서드를 호출하도록 등록
        //this는 이 코드를 실행 중인 PlayerInputReader 객체
        _actions.Gameplay.AddCallbacks( this );
        /*Gameplay:액션 그룹
         BlackoutInputActions        입력 설정 전체
            └─ Gameplay                 액션 그룹(Action Map)
               ├─ Move                  실제 액션(Input Action)
               ├─ Look                  실제 액션
               ├─ Jump                  실제 액션
               ├─ Aim                   실제 액션
               ├─ Fire                  실제 액션
               └─ Interact              실제 액션
         */

    }
    private void OnDestroy()
    {
        _actions.Gameplay.RemoveCallbacks(this);
        _actions.Dispose();
    }

    private void OnEnable()
    {
        SetGameplayEnabled(true);
    }

    private void OnDisable()
    {
        SetGameplayEnabled(false);
    }

    public void SetGameplayEnabled(bool enabled)
    {
        _gameplayEnabled = enabled;

        if (enabled)
        {
            _actions.Gameplay.Enable();
            return;
        }

        _actions.Gameplay.Disable();
        ClearAllInput();

    }
    private void ClearAllInput()
    {
        _move = Vector2.zero;
        _look = Vector2.zero;
        _isAiming = false;
        _jumpRequested = false;
        _fireRequested = false;
        _interactRequested = false;
    }

    public PlayerInputSnapshot CaptureSnapshot()
    {
        if(!_gameplayEnabled)
        {
            return PlayerInputSnapshot.None;
        }

        PlayerInputSnapshot snapshot = new PlayerInputSnapshot(
            _move, _look, _isAiming, _jumpRequested, _fireRequested, _interactRequested);

        //일회 입력은 초기화
        _jumpRequested = false;
        _fireRequested = false;
        _interactRequested = false;

        return snapshot;
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        _isAiming = context.ReadValue<bool>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        _fireRequested = context.ReadValue<bool>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        _interactRequested = context.ReadValue<bool>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        _jumpRequested = context.ReadValue<bool>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _look = context.ReadValue<Vector2>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _move = context.ReadValue<Vector2>();
    }


}
