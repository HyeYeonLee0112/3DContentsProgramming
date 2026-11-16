# 입력·이동·점프 참고 구현

이 문서는 핵심 메서드의 비교 답안이다. 각 절에서 **전체 파일 / 메서드 전체 교체 / 호출부 추가**를 구분한다. 전체 파일이라고 표시한 절 이외에는 기존 멤버를 보존한다. 멤버 표의 필드와 생명주기, Inspector 연결을 먼저 작성한다. 실제 컴파일·플레이 결과는 학습자가 확인하고 기록한다.

<a id="input"></a>
## T01 — 요청 콜백과 포커스

계약: 기존 PlayerInputReader의 `OnAim`, `OnFire`, `OnJump`, `OnInteract`를 아래 공개 void 메서드로 교체한다. 포커스 메서드는 private void로 추가한다. 입력은 `InputAction.CallbackContext`, 상태 변화는 기존 요청 flag다. `ClearAllInput`은 기존 메서드다.

```csharp
public void OnAim(InputAction.CallbackContext context)
{
    _isAiming = context.ReadValueAsButton();
}

public void OnFire(InputAction.CallbackContext context)
{
    if (context.performed) _fireRequested = true;
}

public void OnJump(InputAction.CallbackContext context)
{
    if (context.performed) _jumpRequested = true;
}

public void OnInteract(InputAction.CallbackContext context)
{
    if (context.performed) _interactRequested = true;
}

private void OnApplicationFocus(bool hasFocus)
{
    if (!hasFocus) ClearAllInput();
}
```

ReadValueAsButton은 현재 버튼 누름을 bool로 읽는 패키지 API다. ReadValue<bool>로 버튼 내부 값 형식을 추측하지 않는다. performed는 Input System이 판단한 행동 실행 시점이다. canceled에서는 요청을 지우지 않는다. 스냅샷이 복사 후 요청을 지우므로 다음 소비에는 false다. 포커스 상실은 의도치 않은 과거 요청을 버리는 별도 규칙이다. T10 충전 확장에서는 이 OnFire를 다시 바꾼다.

[T01 검증으로](../01-input.md)

<a id="wiring"></a>
## T02 — Gate와 Controller 전체 파일

**형태: 각각 전체 파일.** [T02의 멤버 계약](../02-player-wiring.md)을 먼저 작성한다. 파일은 Gameplay/Player, namespace는 Blackout.Gameplay.Player다. 아직 Motor·Camera·Gun·Health를 참조하지 않는다.

```csharp
using UnityEngine;

namespace Blackout.Gameplay.Player
{
    public sealed class PlayerControlGate : MonoBehaviour
    {
        // T02~T08 검증 장면의 시작 정책. T20에서 세션 정책으로 교체한다.
        public bool Allowed { get; private set; } = true;

        public void SetAllowed(bool allowed)
        {
            Allowed = allowed;
        }
    }
}
```

```csharp
using UnityEngine;

namespace Blackout.Gameplay.Player
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerControlGate _gate;
        public PlayerInputSnapshot LastInput { get; private set; }

        private void Awake()
        {
            if (_input == null || _gate == null)
            {
                Debug.LogError("PlayerController: Input 또는 Gate 참조가 없습니다.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            PlayerInputSnapshot snapshot = _input.CaptureSnapshot();
            LastInput = _gate.Allowed ? snapshot : PlayerInputSnapshot.None;
        }

        private void OnDisable()
        {
            LastInput = PlayerInputSnapshot.None;
        }
    }
}
```

두 클래스는 Add Component로 붙이고 Reader·Gate를 Inspector에서 연결한다. Update·OnDisable은 Unity가, CaptureSnapshot·SetAllowed는 작성한 코드가 호출한다. 초기화 때 다른 컴포넌트의 Awake 결과를 읽지 않는다.

이 답안은 T02 시점이다. T03 이후의 필드·메서드 추가를 마친 Controller를 위 전체 파일로 되돌리면 안 된다. Gate의 기본 true는 실습 정책이며 사망 이후 OnEnable에서 다시 true로 만들지 않는다.

[T02 확인 절차로](../02-player-wiring.md)

<a id="movement"></a>
## T03 — 수평 이동

**형태: Config는 전체 파일, Tick은 메서드 전체 교체.** Config를 Gameplay/Player/PlayerMovementConfig.cs에 둔다. T04부터 추가될 Gravity·JumpHeight는 아직 없다.

```csharp
using UnityEngine;

namespace Blackout.Gameplay.Player
{
    [CreateAssetMenu(menuName = "BLACKOUT/Player Movement")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _speed = 4f;
        public float Speed => _speed;
    }
}
```

Motor의 Awake·필드·namespace는 [T03 계약](../03-movement.md)대로 작성한다. 아래는 Motor 전체 클래스가 아니다.

계약: PlayerMotor의 `public void Tick(Vector2 move, Vector3 forward, Vector3 right, bool jump, float dt)` 전체 메서드다. `_controller`, `_config`, `PlanarVelocity`는 T03 표대로 선언한다. jump는 다음 단계까지 사용하지 않는다. config와 방향 입력의 유효성은 초기 검사와 호출자가 보장한다.

```csharp
public void Tick(Vector2 move, Vector3 forward, Vector3 right,
    bool jump, float dt)
{
    if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt))
    {
        PlanarVelocity = Vector3.zero;
        return;
    }

    Vector2 input = Vector2.ClampMagnitude(move, 1f);
    Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
    if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
    flatForward.Normalize();
    Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
    Vector3 desired = Vector3.ClampMagnitude(
        flatForward * input.y + flatRight * input.x, 1f);

    Vector3 before = transform.position;
    _controller.Move(desired * _config.Speed * dt);
    PlanarVelocity = Vector3.ProjectOnPlane(
        (transform.position - before) / dt, Vector3.up);
}
```

이 구현은 투영된 forward에서 직각 right를 재계산하므로 매개변수 right를 직접 사용하지 않는다. 두 축의 직교성을 보장하는 선택이다. 인터페이스를 좁히려면 후속 리뷰에서 right 인자를 제거하고 호출부도 함께 바꿀 수 있다.

입력의 크기는 보존하되 최대 1로 제한한다. 모든 입력을 normalized로 만들면 작은 아날로그 입력도 항상 최고 속도로 이동하는 문제가 생긴다. PlanarVelocity는 명령한 속도 대신 충돌 후 실제 위치 차이를 사용한다.

[T03 검증으로](../03-movement.md)

<a id="jump"></a>
## T04 — Tick의 수직 계산 추가

계약: T03 Tick 전체를 아래로 교체한다. `_verticalSpeed`와 Config.Gravity·JumpHeight는 T04 표에서 추가한다. `public void StopMotion()`도 추가한다. 정상 설정은 gravity<0, height>0이며 생성 시 확인한다.

```csharp
public void Tick(Vector2 move, Vector3 forward, Vector3 right,
    bool jump, float dt)
{
    if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt))
    {
        PlanarVelocity = Vector3.zero;
        return;
    }

    Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
    if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
    flatForward.Normalize();
    Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
    Vector2 input = Vector2.ClampMagnitude(move, 1f);
    Vector3 desired = Vector3.ClampMagnitude(
        flatForward * input.y + flatRight * input.x, 1f);

    bool groundedBefore = _controller.isGrounded;
    if (groundedBefore && _verticalSpeed < 0f) _verticalSpeed = -2f;
    if (groundedBefore && jump)
        _verticalSpeed = Mathf.Sqrt(-2f * _config.Gravity * _config.JumpHeight);
    _verticalSpeed += _config.Gravity * dt;

    Vector3 before = transform.position;
    Vector3 velocity = desired * _config.Speed + Vector3.up * _verticalSpeed;
    CollisionFlags flags = _controller.Move(velocity * dt);

    if ((flags & CollisionFlags.Above) != 0 && _verticalSpeed > 0f)
        _verticalSpeed = 0f;
    if ((flags & CollisionFlags.Below) != 0 && _verticalSpeed < 0f)
        _verticalSpeed = -2f;

    PlanarVelocity = Vector3.ProjectOnPlane(
        (transform.position - before) / dt, Vector3.up);
}

public void StopMotion()
{
    _verticalSpeed = 0f;
    PlanarVelocity = Vector3.zero;
}
```

`-2f`는 바닥 접촉을 유지하려는 출발 실험값이다. 모든 경사에서 정답인 상수가 아니다. CollisionFlags는 마지막 이동의 위/아래 접촉 정보를 비트로 담는다. dt 적분의 최고 높이 오차·이동 발판·점프 버퍼는 이 코드의 한계다. T16에서 외부 변위를 추가할 때 StopMotion도 함께 확장한다.

[T04 검증으로](../04-jump.md)


<a id="camera"></a>
## T05 — 카메라의 각도·최종 배치

**형태: 속성·메서드 부분 구현.** [T05의 전체 멤버 표](../05-camera.md)를 먼저 작성한다. 이 코드는 CameraRig 클래스 전체가 아니다.

필요한 멤버는 _target, _camera, _obstacles, _sensitivity, _distance, _radius, _yaw, _pitch, _currentDistance, HasValidAim이다. Awake에서는 참조·설정 검사 후 _yaw를 실제 카메라 Euler Y, _pitch를 DeltaAngle(0, Euler X)의 제한값, _currentDistance를 _distance로 초기화한다.

```csharp
public Vector3 PlanarForward =>
    Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;

public Vector3 PlanarRight =>
    Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;

public Ray AimRay =>
    _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

public bool HasValidAim { get; private set; }

public void ApplyLook(Vector2 delta)
{
    _yaw = Mathf.Repeat(_yaw + delta.x * _sensitivity, 360f);
    _pitch = Mathf.Clamp(_pitch - delta.y * _sensitivity, -30f, 70f);
}

public void LateTick(float dt)
{
    HasValidAim = false;
    if (!isActiveAndEnabled || dt <= 0f ||
        float.IsNaN(dt) || float.IsInfinity(dt))
        return;

    Vector3 origin = _target.position;
    if (Physics.CheckSphere(origin, _radius, _obstacles,
        QueryTriggerInteraction.Ignore))
        return;

    Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    Vector3 backward = rotation * Vector3.back;
    float allowedDistance = _distance;

    if (Physics.SphereCast(origin, _radius, backward, out RaycastHit hit,
        _distance, _obstacles, QueryTriggerInteraction.Ignore))
        allowedDistance = Mathf.Max(0f, hit.distance - 0.03f);

    if (allowedDistance < _currentDistance)
        _currentDistance = allowedDistance;
    else
        _currentDistance = Mathf.MoveTowards(
            _currentDistance, allowedDistance, 8f * dt);

    Vector3 position = origin + backward * _currentDistance;
    if (Physics.CheckSphere(position, _radius, _obstacles,
        QueryTriggerInteraction.Ignore))
        return;

    _camera.transform.SetPositionAndRotation(position, rotation);
    HasValidAim = true;
}
```

ApplyLook는 각도만 누적한다. 위치·실제 회전 변경은 LateTick에서 한 번 확정한다. 장애물 거리의 수축은 즉시, 복귀는 초당 8m 출발값으로 처리한다. 시작 겹침이면 직전 카메라 위치를 남기고 HasValidAim=false로 발사를 막는다. 이것은 겹침의 완전한 자동 복구가 아니다.

0.03m 여유와 8m/s는 조정할 실험값이다. Near Clip·반경·통로 배치·카메라 시작 겹침은 [T05 재현 경로](../05-camera.md)에서 확인한다. 마우스 delta에 dt를 다시 곱하지 않는다.

Controller의 변경은 다음과 같다. **호출부 추가/교체**이며 이 시점에는 총을 참조하지 않는다.

```csharp
// Update에서 Snapshot을 소비하고 LastInput을 결정한 다음:
_cameraRig.ApplyLook(LastInput.Look);
_motor.Tick(LastInput.Move, _cameraRig.PlanarForward,
    _cameraRig.PlanarRight, LastInput.JumpPressed, Time.deltaTime);

// Controller에 새로 추가하는 메서드:
private void LateUpdate()
{
    _cameraRig.LateTick(Time.deltaTime);
}
```

_cameraRig 필드와 Awake의 필수 참조 검사도 추가한다. CameraRig에는 자동 LateUpdate를 두지 않는다. Controller가 LateTick을 호출하므로 순서를 바꾸려면 호출 지점 한 곳을 확인하면 된다.

[T05 확인 절차로](../05-camera.md)
