# T07 — 한 클릭에 한 발, 첫 충돌에 한 번 끝내기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

탄환을 만들기만 하면 벽을 뚫거나 여러 충돌체에 피해를 중복 적용할 수 있다. 비행과 종료의 주인을 하나로 정하고 수명을 만든다.

선행: [T06](06-aim.md) · 다음: [T08](08-health.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [SphereCast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SphereCast.html) — direction·maxDistance | 이번 이동 구간의 부피 검사 |
| 직접 근거 | [Unity 실행 순서](https://docs.unity.com/en-us/engine/6000.3/manual/scripting/managing-update-order/execution-order) | 입력·카메라·물리의 시점 구분 |
| 유사 사례 | [Unity 공식 설계 샘플](https://assetstore.unity.com/packages/templates/packs/design-patterns-solid-coding-sample-project-289616) — Object Pool | 나중에 반환 방식 비교 |
| BLACKOUT용 설계 판단 | [한 프레임의 약속](CONTRACTS.md) | 풀 없이 시작, 최종 카메라 뒤 발사 |

수동 Sweep은 교차하는 모든 움직이는 표적의 연속 충돌을 보장하지 않는다. 정적 얇은 벽 검증과 한계를 구분한다.

## 책임·계약

총은 발사 가능 시간·생성을, 탄환은 이동·첫 명중·종료를 소유한다. T08 전에는 명중 대상 이름과 종료만 확인한다.

| 대상 파일 | 직접 작성할 멤버 | 의미·실패 |
|---|---|---|
| PulseGun 추가 | `[SerializeField] private PulseProjectile _projectilePrefab`; `[SerializeField] private float _interval = 0.25f, _damage = 10f`; `private float _nextShotTime` | 양의 실험값·프리팹 |
| Gun.Awake 변경 | 기존 메서드를 확장 | T06 참조와 양의 range·interval·damage 검증 |
| Gun 추가 | `[SerializeField] private GameObject _owner` | 발사 주체인 Player_Root를 Inspector에서 연결 |
| Projectile | `private void Awake()` | speed·life·radius가 양의 유한 값인지 확인; 오류 로그 후 비활성 |
| Gun | `public bool TryFire(Ray aimRay, bool charged)` | 재사용 시간·총구 검사 후 생성; 성공 때만 다음 시간 갱신; charged는 T10 전 미사용 |
| `Gameplay/Combat/PulseProjectile.cs` | `[SerializeField] private float _speed = 25f, _life = 3f, _radius = 0.05f`; `[SerializeField] private LayerMask _hitMask` | 탄환 수치·충돌 대상 |
| Projectile | `private Vector3 _direction`; `private float _damage, _remaining`; `private GameObject _owner`; `private bool _finished` | 개별 비행 상태 |
| Projectile | `public void Launch(Vector3 direction, float damage, GameObject owner)` | 0 방향·잘못된 수치 거절; 상태 초기화 |
| Projectile | `private void FixedUpdate()` | 이전 위치→다음 위치까지 SphereCast; 첫 명중 또는 수명 만료 시 Finish |
| Projectile | `public void Finish()` | 먼저 _finished를 세운 뒤 Destroy; 중복 호출 무시 |
| Projectile | `private void Release()` | 실제 제거만 수행; 초기 버전은 Destroy(gameObject), 종료 가드와 분리 |
| Controller 추가 | `private bool _pendingFire` | 이번 Update의 의도; LateUpdate에서 한 번 비움 |
| Controller.Update 변경 | LastInput.FirePressed를 _pendingFire에 저장 | 즉시 발사·입력 재소비 없음 |
| Controller.LateUpdate 변경 | LateTick 뒤 허용·HasValidAim 검사 후 TryFire | 먼저 요청을 비워 중복·예외 재실행 방지 |
| Controller.OnDisable 변경 | LastInput=None, _pendingFire=false | 과거 발사 정리 |

## 먼저 생각하기

속도×물리 간격이 얇은 벽 두께보다 길 때, 다음 위치의 겹침만 검사하면 충분한가?

```text
총: 시간·방향 확인 → Instantiate → Launch → 다음 발사 시간
탄환: 이번 이동 거리 전체를 검사 → 맞으면 한 번 Finish
       안 맞으면 이동 → 남은 시간 감소 → 만료 시 Finish
```

## TODO 실습

```csharp
public void Finish()
{
    // TODO: 이미 끝났다면 반환한다.
    // TODO: 종료 상태를 먼저 기록한 뒤 Release()를 호출한다.
}
```

힌트: 종료 표시를 늦게 하면 다른 콜백이 다시 명중 가능 → `Physics.SphereCast`의 distance는 이번 변위의 크기 → Trigger 무시.

[중복 종료 방지 참고 구현](answers/02-combat.md#finish)

## 따라하기·Unity 연결

1. 작은 구와 Trail로 탄환 프리팹을 만든다. 이 단계는 수동 Sweep 검사 방식이므로 Rigidbody 이동·OnTrigger 피해를 동시에 추가하지 않는다.
2. 탄환은 PlayerProjectile 레이어, 검사 마스크는 World·Enemy로 설정한다. Enemy 탄환은 뒤 단계에서 Player를 검사하도록 별도 설정한다.
3. 총 프리팹에 Projectile·Muzzle을 연결하고 Controller.LateUpdate의 LateTick 다음에서 TryFire를 호출한다. 쿨다운 거절은 false이며 오류 로그를 매번 찍지 않는다.
4. Spawn 위치가 벽 안인 경우를 T06의 시작 겹침 검사에서 거절한다. 방향 계산이 성공했어도 처음부터 벽을 관통해 생성하지 않는다.
5. 평지 정적 벽에서 저속→고속 순서로 시험한다. 움직이는 표적과 서로 교차하는 고속 물체는 별도 CCD/상대 이동 검토가 필요한 한계다.
6. 탄환 100발을 발사하고 몇 초 후 활성 탄환 수가 0인지 확인한다. 소리·연출 개선과 풀링은 T11이다.

## 07-1. 총과 탄환을 순서대로 작성하기

1. PulseProjectile을 먼저 작성해 컴파일한다. namespace는 `Blackout.Gameplay.Combat`다. 아직 IDamageable·Health는 참조하지 않는다.
2. Awake에서 수치를 검사한다. Launch는 0/비유한 방향·유효하지 않은 피해·없는 owner를 프로그래머 오류로 거절한다. 방향 정규화 후 수명·종료 상태를 초기화한다.
3. FixedUpdate는 남은 수명보다 길게 이동하지 않도록 이번 dt를 제한한다. 현재 위치부터 다음 위치까지 구의 전체 이동 경로를 검사한다.
4. 맞으면 T07에서는 개발 기록으로 위치·이름을 확인한 뒤 Finish한다. 맞지 않으면 이동하고 남은 수명을 줄인다.
5. PulseGun의 기존 Awake에 prefab·owner·발사 간격·피해 검사를 추가한다. Instantiate/Destroy로 시작하고 풀은 T11에서 비교한다.

TryFire는 무효 Ray·사용 불가 컴포넌트·재사용 대기·총구 겹침에 false를 반환한다. 성공한 Launch 뒤에만 nextShotTime을 갱신한다. Instantiate 뒤 Launch에서 예외가 나면 생성한 탄환을 정리하고 예외를 다시 알려 설정 문제를 숨기지 않는다.

### 단계별 힌트

<details>
<summary>힌트 1 — 어떤 시간이 기준인가</summary>

발사 간격은 게임 시간, 비행은 FixedUpdate 경과 시간으로 계산한다. 렌더 프레임마다 물리 스텝이 한 번 오는 것은 아니다.

</details>

<details>
<summary>힌트 2 — 얇은 벽</summary>

다음 위치에서 겹치는지만 보면 중간 벽을 건너뛸 수 있다. 이전 위치부터 이번 이동량 전체를 검사한다.

</details>

<details>
<summary>힌트 3 — 종료 기록</summary>

종료 상태를 먼저 세운 뒤 실제 제거를 요청한다. Destroy의 제거 시점에 의존해 중복을 막지 않는다.

</details>

[발사·비행·종료의 부분 메서드 참고 구현](answers/02-combat.md#fire)

## 07-2. 발사 의도와 실제 발사 연결

```text
Controller.Update
  Snapshot 한 번 → LastInput → 시선·이동·몸 회전
  _pendingFire = LastInput.FirePressed

Controller.LateUpdate
  CameraRig.LateTick(dt)
  요청을 로컬로 옮기고 _pendingFire=false
  Gate 허용 + HasValidAim + 요청 있음 → Gun.TryFire(최종 AimRay, false)
```

호출 사이에 Gate가 닫히면 발사하지 않는다. T08은 사망 알림에서도 pending을 즉시 비운다. 미래 Driver가 생기기 전에도 최종 카메라를 사용하는 약속을 지킨다.

[Controller Update/LateUpdate 변경 부분](answers/02-combat.md#shot-order)

## 07-3. 프리팹과 레이어 확인

1. 작은 구 표시와 필요하면 Trail만 남긴다. 구의 기본 SphereCollider는 제거한다. 수동 SphereCast와 Rigidbody·OnTrigger 피해를 동시에 사용하지 않는다.
2. 프리팹의 Hit Mask는 World·Enemy로 둔다. Player·PlayerProjectile과 Trigger는 제외한다. Owner 지정은 자기 충돌을 자동 제외하지 않는다.
3. Gun의 Projectile Prefab·Muzzle·Owner를 연결한다. Owner는 WeaponSocket이 아닌 Player_Root다.
4. `_muzzleClearance >= _radius`가 되게 설정한다. 발사 직전과 탄환 시작 구의 겹침을 확인한다.
5. 정적 벽 저속 → 고속 얇은 벽 → 연타 간격 → 100발 수명 만료 순서로 본다.

## 증상별 문제 해결

| 증상 | 먼저 확인 |
|---|---|
| 급회전 사격 방향이 늦음 | Update에서 즉시 쏘는 코드 잔존 |
| 클릭 한 번에 여러 발 | pending 초기화·입력 수집·Tick 중복 |
| 얇은 벽 관통 | 경로 길이·반경·World 마스크 |
| 원점으로 튐 | Launch 시점·방향·부모 Transform |
| 없어지지 않음 | remaining 감소·Finish·Release |
| 사망 직전 추가 발사 | LateUpdate Gate 검사·T08 즉시 취소 |

## 실패·완료 기준

- [ ] 한 클릭으로 한 발만 발사되며 연타 시 정한 간격을 지킨다.
- [ ] 얇은 정적 벽과 모서리를 통과하지 않는다.
- [ ] 명중 또는 수명 만료 후 한 번 종료된다.
- [ ] 100발 후 떠도는 탄환·새 Console 오류가 없다.
- [ ] 첫 전투 빌드와 짧은 Profiler 기준 기록을 남겼다.

## 좋은 이유와 비용

비행 종료를 한 곳으로 모으면 나중에 Destroy를 풀 반환으로 바꾸기 쉽다. 직접 Sweep은 움직이는 대상의 시간 차이를 해결해 주지 않는다. 물리 Rigidbody 탄환은 대안이지만 두 이동 체계를 섞지 않고 별도 비교한다.

## 실습 기록 — 실제 확인 후 작성

아래는 빈 기록 양식이다. 문서 검토와 구현 결과를 구분하며 실행하지 않은 항목은 미확인으로 남긴다.

```text
태스크 / 확인 날짜:
직접 작성·변경한 파일 / Inspector 설정:
재현 순서:
예상 결과 / 실제 관찰:
정상·실패·취소·중복·재활성화 결과:
Console 첫 오류 또는 오류 없음 확인 조건:
영상·스크린샷·빌드 위치:
원인 근거 / 선택한 해결 / 추가 비용 / 남은 한계:
```
