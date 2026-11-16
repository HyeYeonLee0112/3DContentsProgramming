# T06 — 몸 방향과 조준점·총구의 차이 이해하기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

화면 가운데 적이 보여도 총구 바로 앞은 벽일 수 있다. 먼저 화면에서 목표를 정하고, 총구에서 그 목표까지 실제 길이 열렸는지 따로 확인한다.

선행: [T05](05-camera.md) · 다음: [T07](07-fire.md) · 심화: 로드맵 D01

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [ViewportPointToRay](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.ViewportPointToRay.html) — viewport 좌표 | 실제 카메라 중심 Ray 생성 |
| 직접 근거 | [Raycast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.Raycast.html) — 거리·마스크 | 첫 목표점 검사 |
| BLACKOUT용 설계 판단 | [카메라·총·회전 책임](../BLACKOUT_TechnicalDesign.md#responsibilities) | 카메라 목표와 총구 경로를 따로 판단 |

이 조준 방식 전체가 Unity의 유일한 TPS 정답은 아니다. 엄폐 뒤 사격과 가까운 총구의 실패를 추적하기 위한 선택이다.

## 책임·소유권

`PlayerFacing`은 몸의 수평 회전만 담당한다. `PulseGun`이 목표점과 총구 장애물을 판단한다. 카메라의 AimRay는 시선 정보이며 그 자체가 피해 판정은 아니다.

## 구현 계약

| 파일 | 필요한 멤버 | 역할·실패 |
|---|---|---|
| `Gameplay/Player/PlayerFacing.cs` | `[SerializeField] private Transform _body`; `[SerializeField] private float _turnSpeed = 540f` | 회전 대상·초당 회전각 |
| Facing | `private void Awake()` | body 누락 검사 |
| Facing | `public void Tick(Vector3 moveDirection, Vector3 aimForward, bool aiming, float dt)` | 조준이면 시선, 아니면 이동 방향; 길이 0이면 기존 방향 유지 |
| `Gameplay/Combat/PulseGun.cs` 초기 | `[SerializeField] private Transform _muzzle`; `[SerializeField] private LayerMask _aimMask`; `[SerializeField] private LayerMask _blockMask`; `[SerializeField] private float _range = 60f` | 총구·카메라 목표/총구 검사 대상 |
| Gun 추가 | `[SerializeField] private float _muzzleClearance = 0.07f`; `private void Awake()` | 양의 유한 거리·반경·총구 참조 검사; T07까지 탄환 타입 참조 없음 |
| Gun | `public Vector3 ResolveAim(Ray aimRay)` | 카메라 첫 충돌점, 없으면 최대 거리 점 |
| Gun | `public bool TryGetShotDirection(Ray aimRay, out Vector3 direction)` | 총구 겹침·너무 짧은 방향 거절; 유효 방향 반환 |
| Controller 변경 | `[SerializeField] private PlayerFacing _facing`; `[SerializeField] private PulseGun _gun` | Awake에 필수 참조·활성 상태 검사 추가 |
| Controller.Update 변경 | Motor.Tick 뒤 Facing.Tick 호출 | `_motor.PlanarVelocity`, `_cameraRig.PlanarForward`, `LastInput.IsAiming`, dt 전달; 발사 없음 |

## 질문·의사코드

두 검사의 시작점이 같아도 되는가? 캐릭터의 몸을 카메라 pitch만큼 기울이면 바닥 이동은 어떻게 보이는가?

```text
조준 아님 → 이동 방향의 y를 제거해 몸 회전
조준 중 → 카메라 앞의 y를 제거해 몸 회전
AimRay 첫 충돌 또는 최대거리 → 목표점
총구 겹침 확인 → 목표점-총구 벡터 → 정규화
총구에서 목표까지 막힌 곳이 있으면 탄환이 그곳에 먼저 충돌
```

## TODO 실습

```csharp
public Vector3 ResolveAim(Ray aimRay)
{
    // TODO: _aimMask에서 첫 충돌점을 반환한다.
    // TODO: 맞지 않았다면 _range만큼 앞의 점을 반환한다.
    return default;
}
```

힌트: `Physics.Raycast` → `Ray.GetPoint` → `Quaternion.RotateTowards`. 거리와 방향을 혼동하지 않는다.

[목표점 계산 참고 구현](answers/02-combat.md#aim)

## 따라하기·Inspector

1. Visual이 모델과 Animator의 부모인지 확인하고 Facing의 body를 연결한다. Root 위치는 Motor만 바꾼다.
2. WeaponSocket 아래 게임용 총 프리팹과 Muzzle 빈 Transform을 만든다. Muzzle의 파란 Z축이 총구 앞을 향하는지 Scene 도구로 확인한다.
3. Gun에 Muzzle과 두 검사 마스크를 연결한다. Player·자기 무기·잔류 전력 Trigger는 목표/막힘 검사에서 제외한다.
4. 조준점은 Canvas 중앙의 작은 이미지로 먼저 만든다. 장식보다 화면 중앙과 Ray가 일치하는지 확인한다.
5. Debug.DrawLine으로 카메라 선과 총구 선을 다른 색으로 그린다. 총구가 벽 안이면 CheckSphere 등의 겹침 검사로 발사를 거절할 조건을 정한다.
6. 이 단계는 선과 방향만 검증한다. T07에서 실제 탄환과 연결한다. 빠른 이동 중 Ray 시점은 [공통 계약](CONTRACTS.md)의 한 프레임 약속을 따른다.

## 06-1. 파일을 만들고 회전부터 연결하기

1. PlayerFacing은 `Blackout.Gameplay.Player`, PulseGun은 `Blackout.Gameplay.Combat` namespace를 사용한다. Controller에 Combat using을 추가한다.
2. Facing은 body·turnSpeed의 참조·양수 유한 값을 검사한다. 0에 가까운 방향으로 LookRotation하지 않는다.
3. Controller.Update에서 ApplyLook → Motor.Tick → Facing.Tick 순서를 지킨다. 조준 중에는 cameraRig.PlanarForward, 평소에는 Motor.PlanarVelocity를 회전 기준으로 사용한다.
4. Facing에 연결할 Body는 Visual이다. CharacterController가 붙은 Root까지 마우스 pitch로 기울이지 않는다.
5. 모델의 앞이 +Z와 다르면 Visual 아래 모델 자식에서 보정한다. 이동·카메라의 기준까지 뒤집지 않는다.

## 06-2. 카메라 목표와 실제 총구 방향 구하기

1. T06의 Gun은 **아직 발사하지 않는다.** 총구·마스크·거리와 두 방향 계산 메서드만 만든다.
2. ResolveAim은 화면 중심 Ray의 첫 충돌점 또는 최대거리 점을 반환한다.
3. TryGetShotDirection은 실패 시 direction=Vector3.zero로 시작한다. 총구 반경의 시작 겹침, 너무 가까운 목표, 카메라 시선 뒤로 향하는 목표는 false다.
4. 정상이라면 목표점에서 총구 위치를 뺀 뒤 정규화한다. 총구 앞 경로의 벽은 T07 탄환이 먼저 맞아야 한다.
5. 실제 중심 Ray 검사는 Controller.LateUpdate의 LateTick **다음**에서 한다. T06은 결과만 Scene 디버그 선으로 확인하고 T07에서 발사 의도와 연결한다.

### 단계별 힌트

<details>
<summary>힌트 1 — 두 시작점</summary>

카메라는 볼 대상을 고른다. 탄환은 총구에서 출발한다. 같은 목표를 향해도 두 선이 같을 필요는 없다.

</details>

<details>
<summary>힌트 2 — 무효 방향</summary>

총구가 벽 안에 있거나 목표가 총구 뒤쪽이면 normalized만으로 정상 발사를 만들 수 없다.

</details>

<details>
<summary>힌트 3 — 사용할 API</summary>

`Raycast`, `CheckSphere`, `Vector3.Dot`, `Quaternion.LookRotation`, `Quaternion.RotateTowards`의 입력·반환을 확인한다.

</details>

[Facing.Tick·목표점·총구 방향의 부분 메서드 참고 구현](answers/02-combat.md#aim)

## 06-3. Inspector와 표시

| 필드·대상 | 연결 |
|---|---|
| Facing.Body | Player_Root 아래 Visual |
| Gun.Muzzle | WeaponSocket 아래 총구의 빈 Transform |
| Gun.Aim Mask | World·Enemy, 자기 Player 제외 |
| Gun.Block Mask | World·Enemy, 자기 Player·Trigger 제외 |
| Muzzle Clearance | T07 탄환 Radius 이상; 0.07은 출발 실험값 |
| Controller.Facing/Gun | 해당 컴포넌트 |

조준점은 Screen Space Overlay Canvas 중앙의 작은 이미지로 둔다. Anchor와 Pivot은 중앙 0.5, 위치 0, 크기는 8×8 정도에서 확인한다. CanvasScaler는 Scale With Screen Size, 기준 1920×1080으로 시작한다. 최종 HUD 아트가 아니라 중심 Ray를 확인하는 최소 표시다.

카메라가 본 적, 총구 앞 벽, 총구 시작 겹침을 다른 상황으로 찍는다. `Debug.DrawLine`은 영구 UI가 아니며 Scene의 Gizmos 설정에 따라 보인다.

## 증상별 문제 해결

| 증상 | 먼저 확인 |
|---|---|
| 몸이 위아래로 눕는다 | Facing에서 y 제거, Body가 Visual인지 |
| 벽 뒤의 적을 바로 맞힐 것 같은 선 | 카메라 선과 총구 선을 따로 보는지 |
| 가까이서 뒤로 향한다 | 목표까지 거리·시선과 Dot 검사 |
| 자기 몸만 목표로 잡힌다 | Player 레이어와 Aim Mask |
| 총만 반대로 향한다 | Muzzle +Z와 모델 자식 보정 |

## 검증·완료 기준

- [ ] 비조준 이동과 조준 회전이 서로 다른 기준을 쓴다.
- [ ] 총구가 벽 안일 때 정상 방향인 척 발사 준비하지 않는다.
- [ ] 벽 모서리에서 화면 목표와 총구 선의 차이를 설명한다.
- [ ] 조준점·두 선·마스크를 보이는 증거를 남겼다.

## 구조의 이유와 한계

시선과 실제 발사를 나누면 벽 뒤 사격 원인을 추적하기 쉽다. 검사 두 번과 레이어 관리 비용이 생긴다. 가까운 거리나 Hitscan 게임에서는 다른 정책도 가능하지만 BLACKOUT의 시각적 발사체와 총구 위치를 일관되게 유지한다.

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
