# T05 — 돌아보고 벽을 피하는 TPS 카메라 만들기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

플레이어가 벽 뒤로 가면 카메라도 벽 안으로 들어갈 수 있다. 시선 각도를 계산하는 일과 최종 카메라 위치를 배치하는 일을 나눈다.

선행: [T04](04-jump.md) · 다음: [T06](06-aim.md) · 비교 실험: [E01](E01-cinemachine.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [SphereCast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SphereCast.html) — 시작 겹침 주의 | 부피 검사와 한계 |
| 직접 근거 | [Unity 실행 순서](https://docs.unity.com/en-us/engine/6000.3/manual/scripting/managing-update-order/execution-order) — Limitations | 서로 다른 LateUpdate 순서를 가정하지 않기 |
| BLACKOUT용 설계 판단 | [T05~T07 호출 순서](CONTRACTS.md) | 작은 LateTick을 Controller에서 호출; 최종 카메라 뒤 발사 |

수동 카메라는 첫 비교 기준이다. Cinemachine의 실제 Camera를 사용하게 되면 패키지의 갱신 시점에 맞춰 다시 검증한다.

## 개념·파일 책임

yaw는 좌우 회전각, pitch는 위아래 회전각이다. `ThirdPersonCameraRig`가 각도와 충돌 거리를 소유한다. 플레이어 체력·무기는 알지 않는다. 대상 Transform은 Inspector에서 연결하고 카메라가 직접 플레이어를 이동시키지 않는다.

파일: `Scripts/Runtime/Presentation/Camera/ThirdPersonCameraRig.cs`.

## 구현 계약

| 작성할 멤버 | 의미·실패 정책 |
|---|---|
| `[SerializeField] private Transform _target`; `[SerializeField] private UnityEngine.Camera _camera` | CameraTarget·실제 카메라 |
| `[SerializeField] private LayerMask _obstacles` | World 등 벽 레이어; Player 제외 |
| `[SerializeField] private float _sensitivity = 0.1f, _distance = 3f, _radius = 0.2f` | 양의 유한 실험값 |
| `private float _yaw, _pitch, _currentDistance` | 개별 카메라 상태 |
| `public Vector3 PlanarForward { get; }`; `public Vector3 PlanarRight { get; }` | yaw 기준 정규 수평 방향 |
| `public Ray AimRay { get; }` | `_camera.ViewportPointToRay(new Vector3(0.5f,0.5f,0))` 결과 |
| `public void ApplyLook(Vector2 delta)` | 마우스 delta로 각도만 갱신; pitch 제한; 실제 배치는 LateTick |
| `private void Awake()` | 참조·범위 확인, 현재 방향에서 초기각 설정 |
| `public void LateTick(float dt)` | 이동 완료 뒤 목표 거리 검사·배치; Controller만 호출 |
| `public bool HasValidAim { get; private set; }` | 마지막 배치가 유효한지; 시작 겹침이면 false |
| Controller 추가 | `[SerializeField] private ThirdPersonCameraRig _cameraRig`; `private void LateUpdate()` | Awake에서 참조·활성 상태 검사; LateUpdate에서 LateTick 한 번 호출 |
| Controller 변경 | `_movementBasis`를 제거하고 `_cameraRig` 사용; ApplyLook 뒤 Motor에 PlanarForward/Right 전달 | T03의 임시 기준 Transform을 대체 |

## 생각할 질문과 의사코드

마우스 delta는 이미 해당 입력 갱신의 이동량이다. 여기에 dt를 또 곱하면 감도는 프레임률에 어떻게 달라지는가? 나중의 게임패드 회전 속도와 구분한다.

```text
Controller.Update: delta × sensitivity → yaw/pitch → pitch clamp
Controller.LateUpdate → CameraRig.LateTick: 목표점에서 원하는 카메라 위치까지 SphereCast
  장애물 있음 → 표면 앞 안전 거리로 당김
  장애물 없음 → 원래 거리로 복귀
```

## TODO 실습

```csharp
public void ApplyLook(Vector2 delta)
{
    // TODO: 마우스 이동량으로 각도를 누적하고 pitch를 제한한다.
    // TODO: 실제 위치·회전 반영은 LateTick에 남긴다.
}
```

힌트: 높이 제한은 약 -30~70도에서 시험 → Quaternion.Euler → 카메라는 점이 아니므로 SphereCast.

## 따라하기·연결

1. `MainCam`이 실제 Camera인지 확인하고 Rig를 붙인다. 움직이는 Player_Root의 회전에 종속되어 원치 않게 돌면 카메라를 Root 밖으로 둔다.
2. `_target`에 CameraTarget, `_camera`에 MainCam, `_obstacles`에 벽·기둥 레이어를 연결한다. Trigger는 `QueryTriggerInteraction.Ignore`로 검사에서 제외한다.
3. 먼저 충돌 없이 회전·추적을 만든다. Controller.Update에서 ApplyLook를 호출하고 Rig는 자체 Update/LateUpdate를 만들지 않는다. Controller.LateUpdate에서 LateTick(Time.deltaTime)을 한 번 호출한다.
4. 목표점에서 후방 원하는 위치로 SphereCast한다. 맞으면 hit.distance에서 작은 여유 거리를 뺀다. 접촉 시 즉시 당기고 복귀만 천천히 보간하면 벽 관통 위험을 줄인다.
5. 시작 구가 이미 벽과 겹친 경우 SphereCast만으로 해결된다고 가정하지 않는다. 목표점 겹침을 별도 Overlap 검사로 진단하고 안전한 가까운 위치를 정한다. 해결 못 한 조건은 기록한다.
6. 카메라 Near Clip 면의 모서리까지 벽을 통과하지 않는지 통로와 상하 끝각에서 시험한다. `_radius`는 이 화면 범위를 고려해 조절한다.

## 05-1. 각도 계산과 실제 배치를 나누기

1. 파일은 `Presentation/Camera/ThirdPersonCameraRig.cs`, namespace는 `Blackout.Presentation.Camera`다. Camera 타입과 namespace 이름이 겹치므로 카메라 필드는 `UnityEngine.Camera`로 선언한다.
2. Awake는 참조·유한한 양수 설정을 검사하고 실제 카메라의 시작 Euler 각도에서 yaw/pitch를 얻는다. pitch는 DeltaAngle로 -180~180 범위로 해석한 뒤 -30~70도로 제한한다. currentDistance는 distance로 시작한다.
3. ApplyLook는 yaw/pitch만 갱신한다. PlanarForward/Right는 yaw에서 계산한 수평 단위 벡터다. 아직 이전 위치의 AimRay로 발사를 확정하지 않는다.
4. LateTick은 목표점에서 카메라 방향의 반대쪽으로 SphereCast해 최종 위치·회전을 배치한다. 벽에 다가갈 때 즉시 당기고, 벽이 사라졌을 때만 초당 8m의 출발 실험값으로 복귀한다.
5. 시작 구가 벽과 겹치면 HasValidAim을 false로 두고 배치를 보류한다. 안전 위치를 보장한 것으로 주장하지 않는다. CameraTarget·반경·통로 배치를 수정한 뒤 다시 검사한다. T07은 이 값이 false이면 발사하지 않는다.

`LateTick`의 `dt<=0` 또는 비유한 값도 HasValidAim=false로 처리한다. 범위가 작은데 반경을 무조건 줄여 벽 회피를 통과시키지 말고 Near Clip의 실제 화면 모서리까지 함께 본다.

### 단계별 힌트

<details>
<summary>힌트 1 — 바뀐 카메라가 필요한 시점</summary>

입력 각도는 이동 방향 계산 전에 필요하다. 카메라 위치는 이동한 대상 좌표가 나온 뒤에 필요하다. 서로 다른 단계다.

</details>

<details>
<summary>힌트 2 — 벽에서 보간</summary>

허용 거리보다 먼 위치로 천천히 다가가게 하면 벽 안을 잠깐 보여준다. 줄어드는 거리는 즉시 반영하고 늘어날 때만 부드럽게 처리한다.

</details>

<details>
<summary>힌트 3 — 시작 겹침과 실제 Ray</summary>

SphereCast는 시작부터 겹친 장애물을 해결하지 못한다. 먼저 CheckSphere로 진단하고, AimRay는 최종 배치된 실제 Camera의 ViewportPointToRay에서 얻는다.

</details>

[ApplyLook·LateTick 부분 메서드와 호출부 참고 구현](answers/01-input-movement.md#camera)

## 05-2. Controller의 호출부 교체

T03의 임시 movementBasis 필드·참조 검사를 cameraRig로 바꾸고 해당 namespace를 using으로 연결한다. 아래는 **기존 메서드에 반영할 호출부**이며 Controller 전체 파일은 아니다.

```csharp
// Update: LastInput을 한 번 결정한 다음
_cameraRig.ApplyLook(LastInput.Look);
_motor.Tick(LastInput.Move, _cameraRig.PlanarForward,
    _cameraRig.PlanarRight, LastInput.JumpPressed, Time.deltaTime);

// 별도 LateUpdate 메서드 안
_cameraRig.LateTick(Time.deltaTime);
```

CameraRig에 자동 LateUpdate가 남아 있으면 제거한다. 이 단계는 두 클래스의 LateUpdate 순서 설정에 의존하지 않는다. T16에서 Driver로 옮길 때 Controller의 자동 호출도 함께 제거한다.

## 05-3. Inspector와 재현 경로

1. 실제 Camera·AudioListener를 가진 MainCam을 Player_Root와 별개의 장면 오브젝트로 둔다. CameraTarget은 Root의 자식이며 발밑이 아닌 시선 높이로 둔다.
2. Rig의 Target·Camera·Obstacles를 연결한다. 검사 마스크는 World를 포함하고 Player·총·Trigger를 제외한다.
3. 같은 경로로 평지 → 벽 정면 후퇴 → 모서리 급회전 → 좁은 통로 → 상하 끝각을 시험한다.
4. 시작 CameraTarget을 벽과 겹쳐 HasValidAim=false 조건을 확인한다. 다시 안전한 위치로 옮기면 정상 배치가 재개되어야 한다.
5. 검증 중 장애물 구·목표점·이상 위치를 Scene 도구로 확인하고 영상에 남긴다. 화면에서는 막히는데 마스크 검사는 비어 있으면 벽 레이어·Collider부터 본다.

## 증상별 문제 해결

| 증상 | 먼저 확인 |
|---|---|
| 회전이 두 배로 빠름 | Rig에 독립 입력·자동 갱신이 남았는지 |
| 이동 방향이 한 프레임 늦음 | ApplyLook보다 Motor 호출이 앞서는지 |
| 벽 안을 잠깐 보여줌 | 수축 거리까지 보간하는지 |
| 카메라가 멈춤 | HasValidAim·시작 겹침·dt·참조 상태 |
| 화면 모서리만 관통 | Near Clip 크기와 검사 반경 |

## 검증

| 조건 | 기대 |
|---|---|
| 급회전·모서리 후퇴 | 카메라가 벽 안을 보여주지 않음 |
| 위·아래 끝각 | 뒤집히지 않음 |
| Player 근접 | 자기 몸을 장애물로 맞히지 않음 |
| 프레임률 변경 | 마우스 감도가 과하게 바뀌지 않음 |

## 완료 기준

- [ ] 회전과 최종 위치 계산의 시점을 설명한다.
- [ ] 통로·벽·상하 조작을 직접 검증했다.
- [ ] 카메라 회전 후에도 지상 이동이 수평이다.
- [ ] 시작 겹침·Near Clip의 한계를 기록했다.

## 구조의 이득·비용

카메라를 교체해도 이동에는 수평 방향만 주면 된다. 직접 구현은 충돌 예외와 감쇠를 관리하는 비용이 있다. Cinemachine 비교는 [E01](E01-cinemachine.md)에서 같은 검증 경로로 진행한다.

기록: 벽 회피 영상 / 검사 마스크 / 감도 / 남은 겹침 조건.

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
