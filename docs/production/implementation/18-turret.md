# T18 — 전력을 끄면 준비 중 공격도 멈추는 포탑

조명을 껐는데 포탑의 예약 발사는 계속되면 전력 규칙이 믿을 수 없어진다. 시설의 현재 상태를 행동 허용 조건으로 사용한다.

선행: [T15](15-facilities.md), [T17](17-pursuer.md) · 다음: [T19](19-encounters.md)

## 책임·계약

| 파일 | 필요한 멤버 | 역할·소유 |
|---|---|---|
| `Gameplay/AI/TurretState.cs` | `enum TurretState { Off, Searching, Telegraph, Firing, Recovery, Dead }` | 동작 상태 |
| `Gameplay/AI/PoweredTurret.cs` | `[SerializeField] private FacilityPower _power`; `[SerializeField] private Health _health`; `[SerializeField] private Transform _target, _yawPivot, _pitchPivot`; `[SerializeField] private PulseGun _gun`; `private float _timer`; `private int _burstRemaining`; `public TurretState State { get; private set; }` | 회전·예고·연사 진행 |
| Turret | `private void Awake()`; `private void OnEnable()/OnDisable()`; `private void Update()`; `private void RefreshPower()`; `public void CancelAction()` | 필수 참조 검사; 통전/사망 구독; 회수 즉시 중단 |
| Gun 설정 | 적 전용 탄환 프리팹·마스크 | Player·World 검사, 적 본체 제외 |

## 생각할 질문과 의사코드

예고 때 보였지만 발사 순간 벽 뒤로 숨은 플레이어를 맞혀도 되는가? 전력 복구 후 과거 연사의 두 번째 탄환부터 재개해도 되는가?

```text
Off → 통전·생존 → Searching
유효 표적 → Telegraph → 현재 시야 재확인 → Firing
탄마다 현재 통전·생존·시야 검사
정전·사망·비활성 → 타이머와 연사 개수 삭제 → Off/Dead
```

## TODO 실습

```csharp
private void RefreshPower()
{
    // TODO: 죽음·정전이면 CancelAction으로 기존 공격을 끝낸다.
    // TODO: 켜졌으면 새 탐색에서 시작한다.
}
```

힌트: 공격 타이머만 0으로 바꾸고 burstRemaining을 남기지 않기 → 회전 축 로컬/월드 구분 → 적 탄환의 owner 설정.

## 따라하기·Unity 연결

1. 분리된 받침/Yaw/Pitch/총열을 대역 큐브로 만든다. 에셋 도입은 축·라이선스 검증 후 교체한다.
2. 포탑의 FacilityPower를 보안 문과 같은 회로에 연결한다. 통전값을 별도 bool로 복제해 수동 갱신하지 않는다.
3. 먼저 Off/Searching만 검증하고, 조준선 표시와 예고 타이머, 마지막 연사를 추가한다.
4. Muzzle이 총열 끝에 있고 선과 실제 탄환 방향이 맞는지 본다. 적 전용 Gun 설정을 복사해 플레이어 설정 에셋을 함께 바꾸지 않는다.
5. 예고 90%·첫 발 직후·연사 중간에 전력을 회수한다. 이미 발사된 탄환은 유지할지 제거할지 정책을 기록한다. 이번 최소 버전은 준비 중/미발사 탄환만 중단하고 이미 날아간 탄환은 기존 수명대로 끝낸다.
6. 회로 전력 복구 후 새 탐색→예고로 재개되는지 확인한다.

## 완료 기준

- [ ] 같은 전력으로 문과 포탑 상태가 바뀐다.
- [ ] 정전 중 탐지·예고·새 발사가 멈춘다.
- [ ] 예고·연사 중 회수해도 남은 예약 발사가 없다.
- [ ] 재통전 후 과거 공격을 이어서 실행하지 않는다.
- [ ] 이미 발사된 탄환 정책과 검증 영상을 남겼다.

## 구조의 이유·비용

시설 상태를 한 곳에서 소유하면 불빛과 공격 허용이 어긋날 가능성이 줄어든다. 이벤트 수명과 중단 경로를 관리해야 한다. 단순 매 프레임 IsPowered 확인도 대안이며, 이벤트를 쓴다고 전역 이벤트 버스가 필요한 것은 아니다.
