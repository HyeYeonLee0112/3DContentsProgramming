# T08 — 피해·체력·사망과 화면을 연결하기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

한 적의 몸에 Collider가 여러 개 있어도 한 탄환의 피해는 한 번이어야 한다. 체력의 주인을 정하고 화면은 그 결과를 표시한다.

선행: [T07](07-fire.md) · 다음: [T09](09-animation.md), [T10](10-charge.md), [T12](12-power-rules.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [Unity 공식 설계 책](https://unity.com/kr/resources/design-patterns-solid-ebook) — Observer·MVP 체력 예제 | 체력과 표시 분리 |
| 직접 근거 | [공식 이벤트 가이드](https://unity.com/how-to/scriptableobjects-event-channels-game-code) — Listening for events | 구독·해제 수명 |
| 유사 사례 | [Chop Chop 캐릭터 코드](https://github.com/UnityTechnologies/open-project-1/tree/main/UOP1_Project/Assets/Scripts/Characters) | 공격·피해·소리 책임 비교 |
| BLACKOUT용 설계 판단 | [사망·중단 규칙](../BLACKOUT_TechnicalDesign.md#lifecycle) | C# 이벤트와 초기 Controller 연결 |

지금은 Slider 하나로 표시한다. 전체 MVP 구조·ScriptableObject 이벤트 채널·전역 EventBus를 강제하지 않는다.

## 책임·생성·종료

Health가 현재 체력을 소유하고 피해·사망 규칙을 처리한다. HUD는 Health를 구독해 표시한다. Health는 HUD를 모른다. 플레이어와 표적은 각자 Health 컴포넌트를 가진다.

## 구현 계약

| 파일 | 필요한 멤버 | 핵심 로직·실패 |
|---|---|---|
| `Gameplay/Combat/IDamageable.cs` | `bool TryApplyDamage(float amount)` | 살아 있고 유효한 피해일 때만 true |
| `Gameplay/Combat/Health.cs` | `[SerializeField] private float _maximum = 100f`; `public float Current { get; private set; }`; `public float Maximum => _maximum`; `public bool IsDead => Current <= 0f` | 개체별 체력; 유한·양수 최대치 |
| Health | `public event System.Action Changed, Damaged, Died` | 변경/실제 피해/사망 한 번 알림 |
| Health | `private void Awake()`; `public bool TryApplyDamage(float amount)` | 초기화; NaN·0이하·죽은 대상은 false; 0 아래로 내려가지 않음 |
| Health | `public void Restore(float value)` | 0~Maximum 유한 값 복원·Changed; 보상·Died 재발행 금지 |
| Health | `public bool TryHeal(float amount)` | 양의 회복, 살아 있고 안 찬 경우만; Changed |
| HUD 추가 | `private void Awake()` | Health·Slider 참조 누락 로그 후 자신 비활성화 |
| `Presentation/UI/HealthHud.cs` | `[SerializeField] private Health _health`; `[SerializeField] private UnityEngine.UI.Slider _bar`; `private void OnEnable()/OnDisable()`; `private void Start()`; `private void Refresh()` | 구독·해제 쌍; 켤 때 현재값·Start에서 모든 Awake 후 최초값 표시 |
| Controller 추가 | `[SerializeField] private Health _health`; `private void OnEnable()/OnDisable()`; `private void OnPlayerDied()` | OnEnable은 Died 구독만; OnDisable은 해제·입력/발사 의도 정리 |
| Controller 변경 | `private void BlockControl()`; Awake·Update·LateUpdate 변경 | Awake 참조 검사; Update/LateUpdate에서 사망 조회; BlockControl은 Gate false·pending 삭제·Motor.StopMotion |
| Projectile | `private void ResolveHit(Collider other)` 추가 | 종료 가드 설정→부모 IDamageable 피해→Release; 벽이면 피해 없이 Release |
| Projectile.FixedUpdate 변경 | 첫 충돌에서 ResolveHit(hit.collider) 호출 | T07의 명중 시 Finish 호출을 교체; 수명 만료는 기존 Finish |

T07의 Finish와 명중 처리에서 `_finished`를 이중으로 검사하다 피해가 사라지지 않게 순서를 적는다. **중복 가드 설정 → 한 번 피해 → Release**를 ResolveHit에서 처리한다. 여기서 가드가 있는 Finish를 다시 호출하지 않는다.

## 질문·의사코드

체력이 이미 0일 때 추가 피해가 Died를 또 발행하면 잔류 전력은 몇 개 생기는가?

```text
피해량·생존 검증 → 이전 체력 보관 → 0 이상으로 차감
Changed → Damaged → 살아있음에서 죽음으로 바뀌었을 때만 Died
```

## TODO 실습

```csharp
public bool TryApplyDamage(float amount)
{
    // TODO: 무효 피해와 이미 죽은 대상을 거절한다.
    // TODO: 값을 바꾸고 해당 이벤트를 순서대로 보낸다.
    return false;
}
```

힌트: 이벤트는 실제 상태 변경 뒤 → 구독자 호출 전에 사망 판정을 확정 → `float.IsNaN/IsInfinity` 확인.

[피해 처리 참고 구현](answers/02-combat.md#damage)

## 따라하기·Inspector

1. 고정 표적 Root에 Health, 자식 몸·머리에 Collider를 붙여 같은 Health를 찾게 한다.
2. Projectile에서 구체적인 로봇 클래스 대신 IDamageable만 호출한다.
3. Canvas에 Slider를 두고 Min=0, Max=1로 만든다. HUD.Refresh는 Current/Maximum을 표시한다. Health의 Awake보다 HUD OnEnable이 먼저라면 Start의 최초 Refresh도 추가하거나 초기 연결 이후 Bind하는 방식을 택해 초기 순서 문제를 확인한다.
4. 플레이어 Health의 Died를 Controller가 구독해 Gate를 닫고 pending을 지우는 최소 연결을 만든다. 전체 적 정지는 T20에서 중앙으로 옮긴다. 중복 GameFlow를 미리 만들지 않는다.
5. Health에서 직접 UI 텍스트나 Instantiate를 호출하지 않는다. 사망 보상은 T14의 별도 담당이다.

## 08-1. 표적의 체력부터 연결하기

1. IDamageable은 컴포넌트가 아니라 호출 약속이다. `Blackout.Gameplay.Combat` namespace에 인터페이스로 작성한다.
2. Health는 MonoBehaviour이며 IDamageable을 구현한다. Awake에서 maximum을 검사하고 Current를 최대치로 초기화한다. 플레이어와 표적에 각각 붙여 현재값이 공유되지 않는지 본다.
3. TryApplyDamage는 값이 바뀐 뒤 Changed·Damaged·필요하면 Died를 보낸다. 죽음 여부는 알림 전에 확정한다.
4. TryHeal은 살아 있고 유효한 양의 회복·실제 회복 가능량이 있을 때 성공한다. Restore는 유한한 0~Maximum 값만 받으며 범위 밖은 ArgumentOutOfRangeException이다. Died·보상은 발행하지 않는다.
5. 알림 구독자는 표시·취소를 처리한다. 알림 도중 Restore나 추가 공격을 재진입시키지 않는다. 후속 복원은 세션의 안전 시점에 처리한다.

[IDamageable 전체 파일·Health 초기화·회복·복원 참고 구현](answers/02-combat.md#recovery)

## 08-2. 탄환 명중 경로 교체

T07의 첫 충돌에서 Finish 호출을 ResolveHit(hit.collider)로 **교체**한다. 수명 만료는 계속 Finish다.

```text
ResolveHit: 종료면 반환 → 종료 가드 설정
            → 부모 IDamageable에 피해 한 번 → finally에서 Release
Finish:    종료면 반환 → 종료 가드 설정 → Release
```

ResolveHit에서 가드를 세운 뒤 Finish를 호출하면 제거를 건너뛴다. 피해·종료 경로의 차이를 직접 설명할 수 있어야 한다.

[피해·회복·복원·명중의 부분 메서드 참고 구현](answers/02-combat.md#damage)

### 단계별 힌트

<details>
<summary>힌트 1 — 여러 Collider</summary>

몸과 머리 Collider에서 동일한 부모 Health를 찾는다. 탄환은 첫 명중에서 종료한다.

</details>

<details>
<summary>힌트 2 — 체력바 담당</summary>

Health는 값이 바뀌었다고 알리고 HUD가 Current/Maximum을 읽는다.

</details>

<details>
<summary>힌트 3 — 구독과 해제</summary>

`+=`와 `-=`에 같은 메서드를 사용한다. 최초 OnEnable이 상대 Awake보다 먼저일 수 있으므로 Start에서 현재값을 다시 표시한다.

</details>

## 08-3. HUD 연결

1. T06 Canvas에 Slider를 추가한다. Fill은 남기고 Handle은 제거한다. Interactable을 끄고 Min=0, Max=1로 둔다.
2. HealthHud는 `Blackout.Presentation.UI` namespace다. Health와 `UnityEngine.UI.Slider`를 Inspector로 연결한다.
3. Awake는 자기 참조 검사, OnEnable은 Changed 구독·표시, OnDisable은 동일 메서드 해제, Start는 초기 Health 준비 후 다시 표시한다.
4. Refresh는 비율을 Slider에 전달할 뿐 Health를 바꾸지 않는다. 표적·플레이어 HUD에 각각 올바른 Health를 넣는다.
5. HUD를 비활성·재활성 10회 반복해 한 피해에 중복 반응이 없는지 본다.

[HealthHud 전체 파일 참고 구현](answers/02-combat.md#hud)

## 08-4. 사망과 이전 입력 차단

Controller.OnEnable은 Died를 구독하고 OnDisable은 해제한다. Allowed=true를 자동 대입하지 않는다. Update/LateUpdate에서는 이미 사망했는지 다시 확인해 비활성 중 놓친 사망도 차단한다.

BlockControl은 Gate를 닫고 pending을 비우며 Motor.StopMotion을 호출한다. OnPlayerDied도 즉시 호출하므로 최종 발사 직전 사망도 취소된다. Update는 Gate가 닫혀도 Snapshot을 소비한 뒤 이동을 건너뛴다.

[Controller 사망 연결의 추가·교체 부분](answers/02-combat.md#death)

Gate 시작값 true는 초기 실습 임시 정책이다. 아직 부활 UI는 없다. 플레이어 재시작은 Play를 종료하고 다시 실행한다. Restore 후 재사망 검사는 표적에서 한다. T20에서 사망 조정을 GameSession으로 옮기고 T22에서 복원 성공 후에만 재개한다.

## 증상별 문제 해결

| 증상 | 먼저 확인 |
|---|---|
| 시작 체력이 0으로 보임 | 최대치·Health Awake 오류·HUD Start Refresh |
| 명중해도 피해 없음 | 부모 IDamageable·Hit Mask·Finish만 호출하는지 |
| 피해 중복 | 종료 가드·중복 Trigger 경로 |
| HUD 재활성화 때 반응 증가 | 구독/해제 쌍 |
| Restore가 보상 생성 | Died나 일반 피해 경로 호출 |
| 죽은 플레이어가 재활성화 후 발사 | Allowed 자동 true·pending·LateUpdate 사망 검사 |

## 검증·완료 기준

- [ ] 두 Collider를 가진 표적이 한 탄환에 한 번만 피해를 받는다.
- [ ] 0 이하·NaN·이미 죽은 대상 피해는 상태를 바꾸지 않는다.
- [ ] Died는 한 생존 구간에서 한 번이며 Restore는 보상을 만들지 않는다.
- [ ] HUD를 10회 켰다 꺼도 표시·구독이 중복되지 않는다.
- [ ] 사망 뒤 플레이어 입력이 막히며 증거가 있다.

## 선택과 한계

좁은 인터페이스 덕분에 총은 표적 종류를 몰라도 된다. 현재 float 하나의 피해는 방어력·공격자·피격 방향을 표현하지 못한다. 그런 요구가 생기면 DamageInfo로 확장하고 지금 모든 전투 정보를 미리 넣지 않는다.

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
