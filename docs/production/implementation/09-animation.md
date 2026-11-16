# T09 — 이동 코드와 몸의 애니메이션을 맞추기

제자리 걷기나 총을 쏠 때 다리가 멈추는 현상은 코드와 애니메이션의 책임이 섞일 때 생긴다. 실제 이동 결과로 다리를 움직이고 조준은 상체에 더한다.

선행: [T08](08-health.md) · 다음: [T11](11-feedback-pool.md) · 손 보정: [E02](E02-rigging.md)

## 개념·파일 책임

Humanoid Avatar는 사람 뼈대를 공통 기준으로 대응시킨다. Blend Tree는 속도 등 값에 따라 동작을 섞고 Avatar Mask는 상체처럼 적용 부위를 제한한다. 최종 위치는 기존 Motor가 소유한다. Animator의 Apply Root Motion은 이 구성에서 끈다.

## 구현 계약

| 대상 | 작성할 멤버·설정 | 역할 |
|---|---|---|
| `Presentation/Player/PlayerAnimationPresenter.cs` | `[SerializeField] private Animator _animator`; `[SerializeField] private PlayerMotor _motor`; `[SerializeField] private PlayerController _player`; `[SerializeField] private Health _health` | Inspector 참조; 누락 시 한 번 오류·중지 |
| Presenter | `private void Awake()`; `private void LateUpdate()`; `private void OnEnable()/OnDisable()`; `private void OnDied()` | 참조 검사; 실제 속도·접지·조준 전달; 사망 구독·해제 |
| Animator Parameters | `Speed` float, `Grounded` bool, `Aiming` bool, `Dead` bool | 문서와 코드에서 정확히 같은 이름 |
| Animator Layers | Base Locomotion, UpperBody | 이동과 조준의 적용 범위 |

처음에는 문자열 이름으로 정확도를 확인한다. 실제 호출·측정상 필요하면 static readonly Animator.StringToHash 필드를 추가하고 계약에 기록한다. 사격·피격 Trigger는 현재 네 파라미터 동작 후 추가한다.

## 먼저 생각하기·의사코드

입력 W가 눌렸지만 벽에 막혔을 때 Speed는 얼마여야 하는가? 루트 모션과 Motor가 동시에 위치를 바꾸면 무엇을 기준으로 디버깅하는가?

```text
임포트 Avatar 검사 → 위치 변경자는 Motor 한 곳
실제 PlanarVelocity 크기 → Speed
IsGrounded·IsAiming·IsDead → Animator
상체 Mask로 조준 겹치기
```

## TODO 실습

```csharp
private void LateUpdate()
{
    // TODO: _motor.PlanarVelocity로 Speed를 설정한다.
    // TODO: Grounded·Aiming을 각 소유자의 값으로 설정한다.
}
```

힌트: 입력 크기와 실제 속도 구별 → Animator.SetFloat/SetBool → 허리 위에만 Mask 적용.

## 따라하기·연결

1. Striker14의 원본·라이선스·Rig 탭을 확인한다. Humanoid Configure에서 필요한 뼈 대응과 어깨·손·발 변형을 시험한다. 승인 전에는 캡슐/대역 모델을 유지한다.
2. 무료 사용 가능한 Idle·Walk·Run·Jump·Aim·Hit·Death 클립의 출처를 기록한다. Animation 탭의 루프·Root Transform 설정을 클립 성격에 맞춘다.
3. `Art/Animations/Controllers/Player.controller`를 만들고 Idle/Walk/Run Blend Tree에 실제 m/s 기준 임계값을 넣는다.
4. Visual의 Animator를 연결하고 Apply Root Motion을 끈다. Player_Root는 위치·충돌, Visual은 표시를 맡는다.
5. UpperBody에 Avatar Mask와 조준 상태를 둔다. 가중치를 0→1로 시험해 다리에 영향을 주는지 확인한다.
6. 재생 중 피격·사망이 이동·조준과 충돌하는지 본다. 상태 전환 중 발사 피해를 Animation Event만으로 확정하지 않는다. 실제 규칙은 Gun·Health에 남긴다.

## 검증·완료 기준

- [ ] Avatar 매핑과 출처를 확인하고 모델 검증 결과를 기록했다.
- [ ] 벽에 막혔을 때 실제 이동과 발 움직임의 차이를 점검했다.
- [ ] 걷는 중 조준해도 하체 이동이 유지된다.
- [ ] 사망 전환 중 이동 코드·Root Motion이 서로 싸우지 않는다.
- [ ] 어깨·손·발 미끄러짐의 남은 문제를 영상으로 남겼다.

## 구조의 이득·비용

표현 컴포넌트를 분리하면 모델을 바꿔도 이동 규칙을 유지할 수 있다. 파라미터·마스크·임포트 관리 비용이 생긴다. 단일 임시 동작으로 먼저 검증해도 되지만 최종 TPS에서는 상하체 분리가 필요해지는 이유를 직접 확인한다.
