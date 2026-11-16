# T04 — 중력·점프·천장·착지를 구분하기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

평지 이동 다음에는 공중과 지면을 구분해야 한다. CharacterController는 우리가 전달한 변위로 충돌 이동하며 중력을 자동 적용하지 않는다. [공식 Move 설명](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html)

선행: [T03](03-movement.md) · 다음: [T05](05-camera.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [CharacterController.Move](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html) — 중력·충돌 반환값 | 중력은 직접 계산하고 접촉 결과로 정리 |
| BLACKOUT용 설계 판단 | [Motor 상태의 주인](../BLACKOUT_TechnicalDesign.md#ownership) | 수직 속도·StopMotion을 Motor에 모음 |

중력 -20, 점프 높이 1.2와 바닥 유지 속도 -2는 실험 출발값이다. 공식 문서가 지정한 BLACKOUT 정답 수치가 아니다.

## 책임과 소유

PlayerMotor가 수직 속도·접지 판정을 소유한다. 설정 에셋에는 중력·목표 점프 높이만 둔다. 입력 Reader는 점프 가능 여부를 판단하지 않는다.

## 변경 계약

| 대상 | 추가·변경 멤버 | 핵심 로직 |
|---|---|---|
| Config | `[SerializeField] private float _gravity = -20f`; `public float Gravity => _gravity` | 음수의 유한 값; 실험 출발값 |
| Config | `[SerializeField] private float _jumpHeight = 1.2f`; `public float JumpHeight => _jumpHeight` | 0 초과 유한 높이 |
| Motor | `private float _verticalSpeed` | 개체별 수직 속도 |
| Motor | `public bool IsGrounded => _controller.isGrounded` | 마지막 Move의 접촉 결과 |
| Motor.Tick 변경 | 기존 매개변수 유지 | 수평+수직 변위를 합쳐 Move 한 번; Below/Above 결과로 속도 정리 |
| Motor | `public void StopMotion()` | 수직 속도와 PlanarVelocity 0; 나중 복원에서 호출 |
| Motor.Awake 변경 | 기존 참조 검사 뒤 | gravity<0, height>0 검증 |

## 먼저 생각하기

공중에서 Space를 다시 누르면 점프해야 하는가? 천장에 부딪혔는데 위쪽 속도가 남으면 어떤 일이 생기는가?

```text
이전 접지 + 아래로 가는 속도 → 작은 하향 속도로 지면 유지
접지 중 점프 요청 → 목표 높이에서 초기 속도 계산
중력을 dt만큼 누적 → 수평+수직 이동
Move 결과 Above면 상승 속도 삭제, Below면 착지 정리
```

## TODO 실습

기존 Tick 안에서 Move 직전에 추가할 계산을 직접 작성한다. 반환 예제는 점프 속도 식만 설명한다.

```csharp
// TODO: _controller.isGrounded와 jump로 점프 시작을 판단한다.
// TODO: _verticalSpeed에 _config.Gravity * dt를 누적한다.
// TODO: Move 결과의 CollisionFlags.Above/Below를 검사한다.
```

힌트: 에너지식에서 초기 속도는 `sqrt(-2 × gravity × height)` → `Mathf.Sqrt` → gravity가 음수인지 먼저 검증.

[점프 계산 참고 구현](answers/01-input-movement.md#jump)

## 따라하기·연결

1. T03 Config 에셋에 추가 필드가 보이는지 확인한다. 이전 속도 값을 잃지 않는다.
2. Controller는 기존 Snapshot의 JumpPressed를 같은 Tick에 전달한다. 별도 Update에서 다시 점프를 읽지 않는다.
3. PlayerLab의 평지에서 한 번 점프 → 공중 연타 → 착지 후 점프를 확인한다.
4. 낮은 천장·허용 경사·허용 밖 경사·계단을 각각 시험한다. Controller의 Slope Limit과 Step Offset을 실제 통로 치수와 함께 조정한다.
5. Move를 여러 번 호출해 접지 결과가 뒤집히지 않도록 한 Tick의 수평·수직 이동을 합친다.
6. 30fps·60fps에서 점프 최고 높이와 착지의 차이를 기록한다. 큰 dt의 적분 오차를 완전히 없앴다고 주장하지 않는다.

## 04-1. 기존 파일을 누적해서 확장하기

새 Motor를 하나 더 만들지 않는다. T03 Config에 Gravity·JumpHeight를 추가하고 기존 Tick의 이동 계산을 **교체**한다.

1. Config의 Gravity는 음수의 유한 값, JumpHeight는 양수의 유한 값이어야 한다. Awake의 설정 검사에도 두 조건을 추가한다.
2. Motor에 verticalSpeed·IsGrounded·StopMotion을 추가한다. IsGrounded는 “마지막 Move 결과”이지 앞으로 닿을 바닥의 예측값이 아니다.
3. 이전 접지 상태로 점프 시작을 판단하고, 수직 속도를 갱신한 뒤 수평·수직을 합쳐 한 번 Move한다.
4. Move가 돌려준 Above/Below는 비트 플래그다. 동등 비교 대신 해당 비트가 들어 있는지 확인한다.
5. Controller의 Tick 호출은 T03과 동일하다. Reader에서 점프를 다시 읽거나 Motor에 별도 Update를 추가하지 않는다.

### 단계별 힌트

<details>
<summary>힌트 1 — 매 프레임 기억할 것</summary>

중력은 위치에 바로 더할 숫자가 아니다. 먼저 수직 속도를 누적하고 그 속도로 이번 변위를 구한다.

</details>

<details>
<summary>힌트 2 — 점프와 천장</summary>

접지 중 받은 한 번 요청만 상승을 시작한다. 천장에 맞은 뒤에는 상승 속도를 지워 다음 프레임부터 중력으로 내려오게 한다.

</details>

<details>
<summary>힌트 3 — 사용할 값</summary>

초기 점프 속도는 `Mathf.Sqrt(-2f * gravity * height)`다. `_controller.Move`의 반환값에서 CollisionFlags.Above와 Below를 검사한다.

</details>

[Tick 전체 메서드 교체와 StopMotion 참고 구현](answers/01-input-movement.md#jump)

## 04-2. 점프 검사 순서

| 장소·입력 | 기록할 값 | 기대 결과 |
|---|---|---|
| 평지에서 한 번 누름 | 점프 시작·최고 높이·착지 | 한 번 상승 후 착지 |
| 공중에서 연타 | 시작 횟수 | 추가 점프 없음 |
| 낮은 천장 | Above·verticalSpeed | 상승 속도가 남지 않음 |
| 계단·경사 | Slope Limit·Step Offset·접지 | 설정 범위에서 이동, 범위 밖 행동 기록 |
| 조작 없이 30/60fps 비교 | 실제 fps·높이·시간 | 차이를 기록하고 적분 오차를 설명 |

Play 직후에는 첫 Move 전까지 접지가 확정되지 않을 수 있다. 바닥에 안정적으로 선 다음 입력하는 경우와 첫 프레임 입력을 구분한다. 극단적인 저프레임의 관통·점프 높이 오차가 남으면 성공으로 덮지 말고 dt와 재현 조건을 남긴다.

## 코드 해설과 다음 확장

Config의 값은 Inspector에서 입력하고 Motor가 읽는다. verticalSpeed는 Motor의 실행 중 값이다. StopMotion은 다른 코드가 내부 필드에 직접 대입하지 않고 속도를 정리하는 공개 경로다. T08 사망과 후속 복원에서 사용한다.

지금은 코요테 타임·점프 버퍼·이동 발판을 넣지 않는다. 기본 접지의 실패를 먼저 구분하고 같은 검증 장소에서 개선 전후를 비교한다.

## 검증과 자주 생기는 오류

| 증상 | 먼저 확인 |
|---|---|
| 공중 무한 점프 | 접지 조건과 일회 입력 |
| 바닥에서 떨림 | 시작 위치 겹침·Skin Width·접지 속도 |
| 천장에 붙음 | Above 충돌 뒤 양의 속도 정리 |
| 경사에 오르다 날아감 | 기준 벡터의 y 성분·Controller 설정 |

## 완료 기준

- [ ] 접지 중 한 번 점프하고 공중 연타로 다시 뛰지 않는다.
- [ ] 천장 충돌 후 즉시 하강할 수 있다.
- [ ] 계단·경사·착지에서 관통과 반복 점프가 없다.
- [ ] 30/60fps의 차이와 검증 조건을 기록했다.

## 선택 이유와 한계

수직 속도를 Motor 한 곳에서 관리하면 사망·복원 때 초기화할 위치가 분명하다. Rigidbody 방식은 물리 반응이 중심일 때 비교할 수 있다. 코요테 타임·점프 버퍼·움직이는 발판은 기본 접지가 안정된 뒤 추가한다. 발판은 [T16](16-lift.md)에서 다룬다.

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
