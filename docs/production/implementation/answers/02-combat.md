# 조준·종료·피해 참고 구현

각 실습의 필드와 호출부 계약을 먼저 작성한다. 전체 파일이라고 표시한 IDamageable·HealthHud 외에는 메서드 교체/추가 예제다. 기존 파일 전체에 덮어쓰지 않는다. 실제 Unity 컴파일·실행은 이번 문서 작업에서 확인하지 않았다. 아래 메서드는 작은 규칙의 비교 자료이며 전체 게임 기능을 대신하지 않는다.

<a id="aim"></a>
## T06 — 목표점 결정

계약: Gun의 `public Vector3 ResolveAim(Ray aimRay)`다. `_range`는 양의 유한 값, `_aimMask`는 자기 Player를 제외한 카메라 목표 마스크다. 결과는 월드 좌표이며 실제 발사는 총구 경로로 다시 검사한다.

```csharp
public Vector3 ResolveAim(Ray aimRay)
{
    if (Physics.Raycast(aimRay, out RaycastHit hit, _range,
        _aimMask, QueryTriggerInteraction.Ignore))
        return hit.point;

    return aimRay.GetPoint(_range);
}
```

Ray는 시작점과 방향이다. hit.point는 첫 충돌점이다. 아무것도 없을 때도 최대거리 점을 정하므로 총구의 발사 방향을 계산할 수 있다. 총구 겹침·발사점 근처 역방향 문제는 T06의 TryGetShotDirection에서 별도로 거절한다.

### T06 — 몸 회전과 총구 방향

**형태: 각 클래스의 메서드 전체 구현.** Facing은 _body·_turnSpeed, Gun은 _muzzle·_aimMask·_blockMask·_range·_muzzleClearance를 [T06 계약](../06-aim.md)대로 먼저 선언·검사한다.

```csharp
// PlayerFacing 내부:
public void Tick(Vector3 moveDirection, Vector3 aimForward, bool aiming, float dt)
{
    if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
    Vector3 direction = Vector3.ProjectOnPlane(
        aiming ? aimForward : moveDirection, Vector3.up);
    if (direction.sqrMagnitude < 0.0001f) return;

    Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
    _body.rotation = Quaternion.RotateTowards(
        _body.rotation, target, _turnSpeed * dt);
}

// PulseGun 내부:
public bool TryGetShotDirection(Ray aimRay, out Vector3 direction)
{
    direction = Vector3.zero;
    float lengthSquared = aimRay.direction.sqrMagnitude;
    float originSquared = aimRay.origin.sqrMagnitude;
    if (!isActiveAndEnabled || lengthSquared < 0.0001f ||
        float.IsNaN(lengthSquared) || float.IsInfinity(lengthSquared) ||
        float.IsNaN(originSquared) || float.IsInfinity(originSquared))
        return false;

    if (Physics.CheckSphere(_muzzle.position, _muzzleClearance, _blockMask,
        QueryTriggerInteraction.Ignore))
        return false;

    Vector3 toTarget = ResolveAim(aimRay) - _muzzle.position;
    if (toTarget.sqrMagnitude < 0.0001f ||
        Vector3.Dot(toTarget, aimRay.direction) <= 0f)
        return false;

    direction = toTarget.normalized;
    return true;
}
```

몸은 수평 방향으로만 회전한다. 총구 앞에 벽이 보인다고 무조건 발사 실패로 만들지는 않는다. 총구가 벽 밖이면 T07 탄환이 그 벽에 먼저 맞는다. 시작 구가 벽 안이거나 목표가 뒤쪽인 경우만 이 메서드가 거절한다.

[T06으로](../06-aim.md)

<a id="finish"></a>
## T07 — 종료를 한 번으로 제한

계약: Projectile의 `public void Finish()`와 `private void Release()`다. `_finished`는 Launch에서 false로 초기화하는 private bool이다. T11 이전의 제거 경로이며, 풀 도입 때 Release의 Destroy를 반환 콜백으로 바꾼다.

```csharp
public void Finish()
{
    if (_finished) return;
    _finished = true;
    Release();
}

private void Release()
{
    Destroy(gameObject);
}
```

Destroy는 즉시 모든 참조를 없애는 함수가 아니다. 같은 프레임의 다른 호출도 들어올 수 있으므로 _finished를 먼저 바꾼다. T08의 ResolveHit는 자체 가드 뒤 피해를 적용하고 Release를 호출한다. 이미 가드를 세운 뒤 Finish를 다시 호출하면 제거가 생략되므로 두 경로를 구분한다.

[T07로](../07-fire.md)

<a id="damage"></a>
## T08 — Health의 피해 적용

계약: `public bool TryApplyDamage(float amount)`와 기존 `Current`, `IsDead`, `Changed`, `Damaged`, `Died` 멤버다. Current는 Health 내부만 변경한다. 잘못된 피해·이미 사망은 false다. 구독자는 표시·취소만 수행하고 이 알림 도중 Health를 다시 Restore/공격하지 않는 정책으로 시작한다.

```csharp
public bool TryApplyDamage(float amount)
{
    if (IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
        return false;

    Current = Mathf.Max(0f, Current - amount);
    bool diedNow = Current <= 0f;

    Changed?.Invoke();
    Damaged?.Invoke();
    if (diedNow) Died?.Invoke();
    return true;
}
```

값 변경 후 알리므로 HUD는 새 체력을 읽는다. 다음 피해는 IsDead 검사에서 거절되므로 사망 중복을 막는다. Restore는 값을 바꾸고 Changed만 알린다. 이벤트 안에서 다른 규칙을 재호출하는 재진입이 필요해지면 변경 큐나 별도 처리 시점을 설계해야 한다.

### 빠뜨리면 안 되는 검증

첫 피해·과도한 피해·0·음수·NaN·이미 사망·복원 후 재사망을 검사한다. 한 생존 구간의 Died 수를 세고, 화면 재활성화로 구독 수가 늘지 않는지 확인한다.

[T08로](../08-health.md)

### T08 — 탄환 명중 경로

계약: Projectile에 `private void ResolveHit(Collider other)`를 추가한다. T07의 `_damage`, `_finished`, Release를 사용한다. FixedUpdate는 첫 Sweep 충돌에서 이 함수만 호출하고, 수명 만료는 Finish를 호출한다. 실제 코드 파일에 Combat의 IDamageable namespace를 연결한다.

```csharp
private void ResolveHit(Collider other)
{
    if (_finished) return;
    _finished = true;
    try
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();
        target?.TryApplyDamage(_damage);
    }
    finally
    {
        Release();
    }
}
```

피해 처리에서 예외가 나도 탄환이 끝없이 남지 않도록 finally에서 정리한다. 예외를 삼키지는 않으므로 Console에서 원인을 찾아야 한다. Release가 풀 반환으로 바뀌어도 가드·피해 순서는 유지된다.


<a id="fire"></a>
## T07 — 생성·비행의 부분 메서드

**형태: PulseGun.TryFire와 PulseProjectile.Launch/FixedUpdate의 전체 메서드.** 클래스·필드·Awake·마스크는 [T07 계약](../07-fire.md)대로 작성한다. Finish·Release는 위의 [종료 예제](#finish)를 사용한다. 아직 Health를 참조하지 않는다.

```csharp
// PulseGun 내부. _owner는 Inspector에서 연결한 Player_Root다.
public bool TryFire(Ray aimRay, bool charged)
{
    if (!isActiveAndEnabled || Time.time < _nextShotTime)
        return false;
    if (!TryGetShotDirection(aimRay, out Vector3 direction))
        return false;

    PulseProjectile projectile = Instantiate(
        _projectilePrefab, _muzzle.position, Quaternion.LookRotation(direction));
    try
    {
        if (!projectile.isActiveAndEnabled)
        {
            Destroy(projectile.gameObject);
            return false;
        }
        projectile.Launch(direction, _damage, _owner);
    }
    catch
    {
        Destroy(projectile.gameObject);
        throw;
    }

    _nextShotTime = Time.time + _interval;
    return true;
}

// PulseProjectile 내부:
public void Launch(Vector3 direction, float damage, GameObject owner)
{
    float lengthSquared = direction.sqrMagnitude;
    if (lengthSquared < 0.0001f || float.IsNaN(lengthSquared) ||
        float.IsInfinity(lengthSquared))
        throw new System.ArgumentException("발사 방향이 유효하지 않습니다.", nameof(direction));
    if (damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
        throw new System.ArgumentOutOfRangeException(nameof(damage));
    if (owner == null)
        throw new System.ArgumentNullException(nameof(owner));

    _direction = direction.normalized;
    _damage = damage;
    _owner = owner;
    _remaining = _life;
    _finished = false;
}

private void FixedUpdate()
{
    if (_finished) return;
    float dt = Mathf.Min(Time.fixedDeltaTime, _remaining);
    if (dt <= 0f)
    {
        Finish();
        return;
    }

    float distance = _speed * dt;
    if (Physics.SphereCast(transform.position, _radius, _direction,
        out RaycastHit hit, distance, _hitMask, QueryTriggerInteraction.Ignore))
    {
        // T07: 충돌 기록을 확인한 뒤 종료한다.
        // T08: 이 줄의 Finish()를 ResolveHit(hit.collider)로 교체한다.
        Finish();
        return;
    }

    transform.position += _direction * distance;
    _remaining -= dt;
    if (_remaining <= 0f) Finish();
}
```

charged 매개변수는 T10 전까지 사용하지 않는다. _owner는 발사 주체 정보이며 자동 충돌 제외 기능이 아니다. _hitMask에서 자기 레이어와 PlayerProjectile을 제외한다. 속도·수명·반경·발사 간격·피해는 각 Awake에서 양의 유한 값인지 검사한다.

이 Sweep은 시작 겹침이나 상대의 과거 이동 경로 전체를 해결하지 않는다. 총구 검사 반경을 탄환 이상으로 두고 정적 벽 조건부터 재현한다. 생성 실패·설정 오류는 예외와 Console로 확인하며, 정상적인 재사용 대기는 false다.

[T07 검증으로](../07-fire.md)

<a id="shot-order"></a>
## T07 — Controller 호출 순서

**형태: Update/LateUpdate 메서드 교체, OnDisable에 정리 한 줄 추가.** _pendingFire를 추가하고 CameraRig·Motor·Facing·Gun 필드와 참조 검사를 앞 단계에서 마친다.

```csharp
private void Update()
{
    PlayerInputSnapshot snapshot = _input.CaptureSnapshot();
    LastInput = _gate.Allowed ? snapshot : PlayerInputSnapshot.None;
    _cameraRig.ApplyLook(LastInput.Look);
    _motor.Tick(LastInput.Move, _cameraRig.PlanarForward,
        _cameraRig.PlanarRight, LastInput.JumpPressed, Time.deltaTime);
    _facing.Tick(_motor.PlanarVelocity, _cameraRig.PlanarForward,
        LastInput.IsAiming, Time.deltaTime);
    _pendingFire = LastInput.FirePressed;
}

private void LateUpdate()
{
    _cameraRig.LateTick(Time.deltaTime);
    bool requested = _pendingFire;
    _pendingFire = false;
    if (requested && _gate.Allowed && _cameraRig.HasValidAim)
        _gun.TryFire(_cameraRig.AimRay, false);
}
```

기존 OnDisable에서 LastInput=None 뒤 _pendingFire=false도 실행한다. Input Snapshot은 Update에서 한 번만 소비한다. T08 이후에는 아래 사망 연결을 반영하며 위 메서드로 다시 덮어쓰지 않는다.

[T07 호출 순서로](../07-fire.md)

<a id="recovery"></a>
## T08 — 인터페이스·초기화·회복·복원

**형태: IDamageable은 전체 파일**, 나머지는 Health에 넣는 메서드다. Health는 MonoBehaviour, IDamageable을 구현하며 [T08 필드·이벤트 계약](../08-health.md)을 사용한다. TryApplyDamage는 위 [피해 예제](#damage)와 결합한다.

```csharp
namespace Blackout.Gameplay.Combat
{
    public interface IDamageable
    {
        bool TryApplyDamage(float amount);
    }
}
```

```csharp
// Health 내부:
private void Awake()
{
    if (_maximum <= 0f || float.IsNaN(_maximum) || float.IsInfinity(_maximum))
    {
        Debug.LogError("Health: Maximum은 양의 유한 값이어야 합니다.", this);
        enabled = false;
        return;
    }
    Current = _maximum;
}

public bool TryHeal(float amount)
{
    if (IsDead || Current >= Maximum || amount <= 0f ||
        float.IsNaN(amount) || float.IsInfinity(amount))
        return false;

    Current = Mathf.Min(Maximum, Current + amount);
    Changed?.Invoke();
    return true;
}

public void Restore(float value)
{
    if (float.IsNaN(value) || float.IsInfinity(value) ||
        value < 0f || value > Maximum)
        throw new System.ArgumentOutOfRangeException(nameof(value));

    Current = value;
    Changed?.Invoke();
}
```

T08의 Restore는 Changed로 표시를 갱신하지만 Died·Damaged는 발행하지 않는다. T22에서는 중간 표시를 막기 위해 전체 복원 적용 후 알리는 경로로 확장한다. 여기서 플레이어 조작 권한까지 자동 해제하지 않는다.

[T08 실습으로](../08-health.md)


<a id="hud"></a>
## T08 — HealthHud 전체 파일

**형태: 전체 파일.** Presentation/UI/HealthHud.cs에 둔다. [T08 HUD 멤버 계약과 Inspector](../08-health.md)을 먼저 확인한다. Slider는 Min=0, Max=1, Interactable=false다.

```csharp
using UnityEngine;
using UnityEngine.UI;
using Blackout.Gameplay.Combat;

namespace Blackout.Presentation.UI
{
    public sealed class HealthHud : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Slider _bar;

        private void Awake()
        {
            if (_health == null || _bar == null)
            {
                Debug.LogError("HealthHud: Health 또는 Slider 참조가 없습니다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _health.Changed += Refresh;
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (_health != null) _health.Changed -= Refresh;
        }

        private void Refresh()
        {
            _bar.value = _health.Maximum > 0f
                ? Mathf.Clamp01(_health.Current / _health.Maximum)
                : 0f;
        }
    }
}
```

OnEnable의 첫 표시가 상대 Health.Awake보다 빨라도 Start에서 초기값을 다시 반영한다. 재활성화는 현재값을 다시 읽는다. 알림 때마다 새 람다를 구독하지 않고 같은 Refresh를 등록·해제한다. Health는 Slider·Canvas·HUD 타입을 참조하지 않는다.

[T08 HUD 검증으로](../08-health.md)

<a id="death"></a>
## T08 — Controller의 사망 연결

**형태: 필드·메서드 추가/교체.** _health 필드를 추가하고 Awake의 필수 참조 검사에 포함한다. 기존 Update/LateUpdate/OnDisable은 아래로 교체하며 같은 이름의 메서드를 추가로 만들지 않는다. OnEnable·OnPlayerDied·BlockControl을 새로 작성한다.

```csharp
[SerializeField] private Health _health;

private void OnEnable()
{
    if (_health != null) _health.Died += OnPlayerDied;
}

private void OnDisable()
{
    if (_health != null) _health.Died -= OnPlayerDied;
    LastInput = PlayerInputSnapshot.None;
    _pendingFire = false;
}

private void OnPlayerDied()
{
    BlockControl();
}

private void BlockControl()
{
    _gate.SetAllowed(false);
    _pendingFire = false;
    _motor.StopMotion();
}

private void Update()
{
    if (!_health.isActiveAndEnabled || _health.IsDead) BlockControl();

    PlayerInputSnapshot snapshot = _input.CaptureSnapshot();
    LastInput = _gate.Allowed ? snapshot : PlayerInputSnapshot.None;
    _pendingFire = LastInput.FirePressed;
    if (!_gate.Allowed)
    {
        _motor.StopMotion();
        return;
    }

    _cameraRig.ApplyLook(LastInput.Look);
    _motor.Tick(LastInput.Move, _cameraRig.PlanarForward,
        _cameraRig.PlanarRight, LastInput.JumpPressed, Time.deltaTime);
    _facing.Tick(_motor.PlanarVelocity, _cameraRig.PlanarForward,
        LastInput.IsAiming, Time.deltaTime);
}

private void LateUpdate()
{
    _cameraRig.LateTick(Time.deltaTime);
    if (!_health.isActiveAndEnabled || _health.IsDead) BlockControl();

    bool requested = _pendingFire;
    _pendingFire = false;
    if (requested && _gate.Allowed && _cameraRig.HasValidAim)
        _gun.TryFire(_cameraRig.AimRay, false);
}
```

OnEnable은 구독만 한다. 초기 OnEnable에서 상대 HP를 읽어 차단하면 상대 Awake가 아직 실행되지 않은 0을 사망으로 오해할 수 있다. 실제 조회는 Update/LateUpdate에서 하고, 사망 이벤트는 즉시 차단한다. 비활성 중 사망 이벤트를 놓쳐도 다음 Update에서 발견한다.

Gate가 닫힌 동안에도 Reader를 소비하지만 이동·발사는 하지 않는다. 사망 뒤 Root/Controller를 재활성화해도 Gate를 true로 바꾸지 않는다. Health.Restore만 호출하는 것도 조작 재개가 아니다. 이 단계에서는 Play 재시작으로 새 실행을 검증한다.

T20 통합 때 OnPlayerDied와 전체 허용 조정은 GameSession으로 옮기고 Controller는 허용 결과를 사용한다. T16의 Driver 통합 때에는 Update/LateUpdate의 내부 호출 순서를 그대로 옮기고 자동 호출을 함께 제거한다.

[T08 사망 검사로](../08-health.md)
