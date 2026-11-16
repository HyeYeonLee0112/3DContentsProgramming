# T11 — 사격 피드백과 재사용 탄환 검증하기

발사가 전달되어도 소리·반응이 없으면 성공했는지 알기 어렵다. 피드백을 붙이고, 반복 생성이 실제로 비용을 만드는지 측정한 뒤 탄환을 재사용한다.

선행: [T09](09-animation.md), [T10](10-charge.md) · 다음: [T12](12-power-rules.md)

## 책임·소유

Gun은 발사 성공만 알린다. ShotFeedback은 사운드·섬광·짧은 조준 반응을 재생한다. ProjectilePool은 재사용 가능한 탄환을 소유한다. 풀에서 꺼낸 개체는 Launch에서 초기화하고 Finish에서 반환한다.

## 변경 계약

| 파일 | 필요한 멤버 | 핵심 규칙 |
|---|---|---|
| Gun | `public event System.Action ShotFired` | TryFire 성공 뒤 한 번; 거절 때 재생 금지 |
| `Presentation/VFX/ShotFeedback.cs` | `[SerializeField] private PulseGun _gun`; `[SerializeField] private ParticleSystem _muzzleFlash`; `[SerializeField] private AudioSource _audio`; `[SerializeField] private AudioClip _shotClip`; `private void OnEnable()/OnDisable()`; `private void Play()` | 구독·해제·참조 검사, PlayOneShot |
| `Gameplay/Combat/ProjectilePool.cs` | `[SerializeField] private PulseProjectile _prefab`; `[SerializeField] private int _maxRetained = 64`; `private UnityEngine.Pool.ObjectPool<PulseProjectile> _pool` | 보관 한도, 최대 활성 탄환 수와 구분 |
| Pool | `private void Awake()`; `public PulseProjectile Rent()`; `public void Return(PulseProjectile projectile)`; `private void OnDestroy()` | 생성/대여/반환 콜백과 풀 정리 |
| Projectile 추가 | `private System.Action<PulseProjectile> _returnToPool`; `public void BindRelease(System.Action<PulseProjectile> release)` | 반납 경로 주입; new MonoBehaviour 금지 |
| Projectile.Launch/Release 변경 | 기존 공개 인자 유지 | Launch에서 상태·Trail 초기화; Release는 반환 콜백 또는 Destroy; Finish/ResolveHit의 가드는 유지 |
| Gun.TryFire 변경 | Instantiate 대신 Pool.Rent 후 Launch | T07 생성 경로를 교체; 둘 다 호출 금지 |

## 질문·의사코드

maxRetained가 64면 65번째 동시 발사가 반드시 거절되는가? 재사용 탄환의 Trail이 이전 위치에서 이어지면 어디서 지워야 하는가?

```text
동일 발사 패턴 측정 → 생성 비용 확인
풀 대여 → 위치·회전·시간·피해·주인·종료 flag·Trail 초기화 → 발사
명중·만료 → 종료 flag → 반환
풀 용량 초과 반환 → 파괴
```

## TODO 실습

```csharp
public void BindRelease(System.Action<PulseProjectile> release)
{
    // TODO: 이 탄환이 어디로 돌아갈지 보관한다.
}
```

힌트: ObjectPool 콜백 역할을 먼저 종이에 적기 → actionOnRelease에서는 비활성화만 → OnDisable에서 다시 Release하지 않기.

## 따라하기·Unity 연결

1. 풀 도입 전 같은 30초 사격을 Development Build로 측정하고 GC Alloc·생성 수를 기록한다.
2. Pool 오브젝트와 탄환 프리팹을 연결한다. 초기 대여는 프리팹 생성과 release 콜백 설정 후 반환하도록 만든다.
3. Launch가 이전 상태를 모두 초기화하도록 목록을 작성한다. OnEnable에서 자동 발사하지 않는다. 활성화와 Launch 사이에 준비 안 된 상태로 움직이지 않도록 초기화 순서를 정한다.
4. 종료 가드를 세운 뒤 반환한다. 두 번 반환은 개발 빌드에서 발견할 수 있도록 collectionCheck와 자체 종료 가드를 사용한다.
5. AudioMixer에 SFX 그룹을 만들고 총 AudioSource를 연결한다. 피해·시설·적 경고의 우선순위를 듣고 조절한다. 음원·효과 출처는 원장에 추가한다.
6. 탄환을 100회 재사용하고 Trail·속도·주인·피해·수명이 남지 않는지 본다. 도입 전과 같은 조건으로 측정해 이득과 비용을 비교한다.

## 완료 기준

- [ ] 성공한 발사에만 소리·섬광이 한 번 반응한다.
- [ ] 100회 재사용 뒤 과거 상태와 중복 반환이 없다.
- [ ] 풀의 보관 한도와 활성 수 정책을 구분해 설명한다.
- [ ] 도입 전후 GC·프레임 시간·메모리 조건을 기록했다.
- [ ] 볼륨을 꺼도 명중·위험을 시각적으로 구분한다.

## 선택과 한계

풀은 반복 생성 비용을 줄일 수 있지만 보관 메모리와 초기화 책임이 늘어난다. 효과가 적으면 단순 생성 방식을 유지하는 선택도 가능하다. [ObjectPool 공식 API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html)의 콜백과 실제 수명 규칙을 함께 확인한다.
