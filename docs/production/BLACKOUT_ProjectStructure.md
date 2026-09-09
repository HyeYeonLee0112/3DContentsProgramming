# BLACKOUT — Unity 프로젝트 폴더 구조

> 대상 프로젝트: `BlackOut`  
> 기준: Unity 6 · URP · 소규모 TPS 학습 프로젝트  
> 원칙: `Resources` 폴더를 사용하지 않고, 프로젝트 코드와 프리팹의 책임을 명확하게 나눈다.

## 1. 관리 원칙

- 직접 작성하거나 수정한 파일은 `Assets/BLACKOUT` 아래에 둔다.
- 내려받은 외부 에셋은 제작자가 제공한 폴더 구조를 가급적 유지한다.
- 외부 원본 프리팹과 Material을 직접 수정하지 않는다.
- 게임에서 사용할 결과물은 `Assets/BLACKOUT/Prefabs`에 Prefab 또는 Prefab Variant로 만든다.
- 실행 중 파일 로딩은 `Resources.Load`에 의존하지 않고 Inspector 참조를 우선 사용한다.
- 아직 사용하지 않는 기능의 빈 폴더는 미리 만들지 않는다.

## 2. 최종 확장 구조

```text
Assets/
├─ BLACKOUT/
│  ├─ Art/
│  │  ├─ Animations/
│  │  │  ├─ Clips/
│  │  │  ├─ Controllers/
│  │  │  └─ Masks/
│  │  ├─ Materials/
│  │  ├─ Models/
│  │  └─ Textures/
│  ├─ Audio/
│  │  ├─ BGM/
│  │  ├─ Mixers/
│  │  └─ SFX/
│  ├─ Data/
│  │  ├─ Enemies/
│  │  ├─ Levels/
│  │  ├─ Player/
│  │  ├─ Power/
│  │  └─ Weapons/
│  ├─ Prefabs/
│  │  ├─ Characters/
│  │  ├─ Enemies/
│  │  ├─ Environment/
│  │  ├─ Facilities/
│  │  ├─ Interactables/
│  │  ├─ Projectiles/
│  │  ├─ UI/
│  │  ├─ VFX/
│  │  └─ Weapons/
│  ├─ Scenes/
│  │  ├─ Bootstrap/
│  │  ├─ Levels/
│  │  └─ Sandbox/
│  ├─ Scripts/
│  │  ├─ Editor/
│  │  └─ Runtime/
│  │     ├─ Foundation/
│  │     │  ├─ Events/
│  │     │  ├─ Interfaces/
│  │     │  └─ StateMachine/
│  │     ├─ Gameplay/
│  │     │  ├─ AI/
│  │     │  ├─ Checkpoints/
│  │     │  ├─ Combat/
│  │     │  ├─ Facilities/
│  │     │  ├─ Interaction/
│  │     │  ├─ Player/
│  │     │  └─ Power/
│  │     ├─ Infrastructure/
│  │     │  ├─ Bootstrap/
│  │     │  ├─ SaveSystem/
│  │     │  ├─ SceneManagement/
│  │     │  └─ Settings/
│  │     └─ Presentation/
│  │        ├─ Audio/
│  │        ├─ Camera/
│  │        ├─ UI/
│  │        └─ VFX/
│  ├─ Settings/
│  ├─ Tests/
│  │  ├─ EditMode/
│  │  └─ PlayMode/
│  ├─ UI/
│  │  ├─ Fonts/
│  │  ├─ Icons/
│  │  └─ Styles/
│  └─ VFX/
│     ├─ Graphs/
│     ├─ Materials/
│     └─ Textures/
└─ 외부 에셋 제작자 폴더/
```

위 구조는 완성 시점의 예상 구조다. 폴더가 필요해지는 주차에만 해당 부분을 추가한다.

## 3. 현재 생성할 최소 구조

W02의 플레이어 이동, TPS 카메라, 임시 HUD와 테스트 장면에 필요한 폴더만 먼저 만든다.

```text
Assets/BLACKOUT/
├─ Prefabs/
│  ├─ Characters/
│  └─ UI/
├─ Scenes/
│  └─ Sandbox/
├─ Scripts/
│  └─ Runtime/
│     ├─ Gameplay/
│     │  └─ Player/
│     └─ Presentation/
│        ├─ Camera/
│        └─ UI/
└─ Settings/
```

### 현재 폴더의 책임

| 폴더 | 저장 대상 |
|---|---|
| `Prefabs/Characters` | 플레이어 루트와 게임용 Striker14 Prefab Variant |
| `Prefabs/UI` | 임시 HUD와 조준점 Prefab |
| `Scenes/Sandbox` | 이동·카메라·HUD를 독립 검증하는 장면 |
| `Scripts/Runtime/Gameplay/Player` | 이동, 회전, 점프 등 플레이 규칙 |
| `Scripts/Runtime/Presentation/Camera` | TPS 카메라 추적과 충돌 표현 |
| `Scripts/Runtime/Presentation/UI` | 체력·전력·조준점 화면 표시 |
| `Settings` | 프로젝트가 직접 관리하는 입력·렌더링 설정 에셋 |

## 4. 프리팹과 외부 원본의 관계

```text
외부 Striker14 모델 원본
    ↓ 자식 모델로 참조
Assets/BLACKOUT/Prefabs/Characters/Player_Striker14.prefab
    ↓ 프로젝트 기능 연결
Collider + Animator + Player 코드 + 전력 장치
```

원본 모델에 게임 코드를 직접 붙이지 않는다. 프로젝트 프리팹이 원본 모델을 참조하도록 만들어, 원본을 다시 가져와도 게임 설정이 사라지지 않게 한다.

## 5. 새 폴더를 추가하는 시점

- 첫 발사체를 구현할 때 `Gameplay/Combat`, `Prefabs/Weapons`, `Prefabs/Projectiles`를 추가한다.
- 첫 적을 구현할 때 `Gameplay/AI`, `Prefabs/Enemies`를 추가한다.
- 전력 주입을 구현할 때 `Gameplay/Power`, `Prefabs/Facilities`, `Data/Power`를 추가한다.
- 체크포인트를 구현할 때 `Gameplay/Checkpoints`와 필요한 저장 계층을 추가한다.
- 실제 자동 테스트를 작성할 때만 `Tests/EditMode` 또는 `Tests/PlayMode`를 추가한다.

`Managers`, `Misc`, `Others`, `Temp`처럼 책임이 불분명한 공용 폴더는 만들지 않는다.
