# BLACKOUT — 무료 에셋 선정·설치 가이드

> 조사일: 2026-09-09  
> 원칙: 유료 절대 금지 · 장난감/카툰 화풍 금지 · 원본 배포 페이지 직접 링크 · Unity 6 URP에서 직접 검증 후 채택

## 먼저 알아둘 점

현재 `Modern Industrial Pack`과 `Striker14` 원본은 `BlackOut/Assets/ThirdParty`에 들어와 있다. 새 주환경 `Sci-Fi Construction Kit (Modular)`와 나머지 후보는 아직 설치되지 않았다. 이 문서는 “다 받아라” 목록이 아니라, 통일성이 가장 좋은 1차 조합과 탈락 조건을 기록한 도입 계획이다.

에셋 스토어의 `FREE`는 조사일 기준이다. 설치 직전에 가격과 라이선스를 다시 확인하고, Sketchfab 자료는 다운로드 화면의 라이선스 문구를 캡처해 `docs/assets/licenses/`에 보관한다.

## 1. 권장 조합

**낡은 도시 화물역의 사실적인 산업 설비**를 기준으로 잡는다. 환경은 Unity Asset Store의 한 팩을 뼈대로 쓰고, 캐릭터와 기계는 Sketchfab의 CC BY 원본만 보강한다. 모든 모델에 건메탈·안전 황색·청록 전력·적색 센서 규칙을 다시 적용한다.

| ID | 역할 | 1차 선정 에셋 | 무료/라이선스 | 시각 적합 | 기술 준비 | 판정 |
|---|---|---|---|:--:|:--:|---|
| ENV-01 | 주 환경 | [Sci-Fi Construction Kit (Modular)](https://assetstore.unity.com/packages/3d/environments/sci-fi/sci-fi-construction-kit-modular-159280) · Sickhead Games | FREE · Standard Unity Asset Store EULA | 5/5 | 2/5 | **채택 · 설치/Unity 6 URP 검증 전** |
| CHR-01 | 주인공 | [Jumpsuit Low Poly Game Ready (Striker14)](https://sketchfab.com/3d-models/jumpsuit-low-poly-game-ready-striker14-da288adbddf949aa85962968732f0999) · DanlyVostok | 무료 · CC BY 4.0 | 5/5 | 4/5 | **채택 · Unity 검증 전** |
| ENM-01 | 추적 로봇 | [Combat Robot Unit influenced — By GBaroni art](https://sketchfab.com/3d-models/combat-robot-unit-influenced-by-gbaroni-art-86fbb9e89e2a47628a2b34ee5ee4c763) · RoboticModels | 무료 · CC BY | 4/5 | 3/5 | 리그·LOD 시험 |
| ENM-02 | 포탑 | [Sentry Turret — Darwin Auto Cannon](https://sketchfab.com/3d-models/sentry-turret-darwin-auto-cannon-17840b0ce9754d78b0610e525a7a7ef5) · seangorman | 무료 · CC BY | 4/5 | 4/5 | 축 분리 시험 |
| BOSS-01 | 보스 | [Mech Cyberpunk — Police Mech Lowpoly Animated](https://sketchfab.com/3d-models/mech-cyberpunk-police-mech-lowpoly-animated-eabe4d28cca7439485244d6858590123) · matthall | 무료 · CC BY | 4/5 | 4/5 | **유력 후보** |
| WPN-01 | 펄스총 | [Dvina Sci-Fi Gun](https://sketchfab.com/3d-models/free-low-poly-model-sci-fi-gun-c4c9b90c2ce64cc4b74d2c81cf53c0b5) · Dmitry Blagodaryov | 무료 · CC BY | 4/5 | 4/5 | 손 위치 시험 |

### 왜 이 조합인가

- `Sci-Fi Construction Kit (Modular)`은 벽·바닥·천장·통로·계단뿐 아니라 선반·상자·팔레트·배럴·환기 덕트·조명·배관·표지판을 함께 제공한다고 명시되어 있다. 기존 팩에 부족했던 닫힌 실내와 창고 밀도를 한 제품군으로 만들 수 있어 주환경으로 채택했다.
- 공식 페이지에서 FREE, Standard Unity Asset Store EULA, 464.0MB, 버전 1.1.0을 확인했다. 원본 Unity 버전이 2018.4.21이고 2020년 이후 업데이트가 없으므로 Unity 6 URP 호환은 설명만으로 보장하지 않는다.
- 환경, 총, 로봇 모두 현실적인 PBR 금속과 산업용 형태를 사용해 같은 조명에서 묶기 쉽다.
- 주인공은 갑옷이 아닌 몸에 맞는 점프슈트를 입어 군인보다 현장 기술자로 읽힌다. 검정·차콜 의상은 산업 환경과 이미 가깝고, 주황 발광선을 청록 전력 표시로 바꾸면 게임의 색상 규칙과 연결된다.
- 주인공 모델은 약 12.5k triangles이며 배포 설명에 리깅 모델과 비리깅 모델이 모두 포함된다고 명시되어 있다. 등 실루엣이 단순해 기존 산업 소품을 배터리 장치로 부착하기도 쉽다.
- 포함 애니메이션은 충분한 TPS 동작 세트가 아니므로 Mixamo의 Idle, Walk, Run, Jump, Aim, Fire, Hit, Death를 Humanoid로 리타게팅한다. 손과 어깨가 총기 자세에서 뒤틀리면 대량 수정하지 않고 후보를 다시 검토한다.
- 포탑은 약 7.1k triangles, 보스는 약 29.9k triangles로 직접 모델링 없이 구현하기 현실적이다.
- 보스에 걷기 애니메이션과 리그 파일이 제공되어 2단계 행동을 코드와 Animator로 확장할 수 있다.
- 추적 로봇은 완전한 리그가 있으나 약 152.7k triangles이므로 LOD와 실제 프레임 테스트를 통과해야 한다.

점수는 웹 설명과 미리보기에 기반한 **사전 평가**이며 Unity에서의 승인 점수가 아니다.

## 2. 보조 에셋

| ID | 용도 | 링크 | 조건 |
|---|---|---|---|
| ENV-SUP-01 | 철골 발판·배관·케이블·조명 보조 | [Modern Industrial Pack](https://www.productioncrate.com/objects/RenderCrate-Modern_Industrial_Pack) | 무료 · Extended Use License. 설치됨. 완성형 실내 주환경으로는 부적합하며 새 주환경과 맞는 부품만 선별 |
| ENV-02 | 팔레트·작업등·산업 소품 보강 | [Industrial Props Kit](https://assetstore.unity.com/packages/3d/props/industrial/industrial-props-kit-84745) | FREE · Unity EULA. ENV-01과 재질을 맞춘 소품만 선택 |
| VFX-01 | 불꽃·폭발을 수정할 출발점 | [Particle Pack — Starter Assets](https://assetstore.unity.com/packages/vfx/particles/particle-pack-starter-assets-127325) | FREE · Unity EULA · Unity 6000.3 URP 호환 표기 |
| ANIM-01 | 임시 캐릭터와 카메라 참고 | [First Person + Third Person Character Controllers](https://assetstore.unity.com/packages/3d/characters/first-person-third-person-character-controllers-196526) | FREE · Non-standard EULA. **완성 이동 코드는 복사하지 않고** 모델·애니메이션·카메라 비교용 |
| ANIM-02 | 주인공 Humanoid 애니메이션 | [Adobe Mixamo](https://www.mixamo.com/) · [공식 FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) | Adobe ID로 무료. 게임 사용 가능. 각 클립 FBX 다운로드 필요 |
| LIGHT-01 | 산업 실내 반사·조명 기준 | [Industrial Workshop Foundry](https://polyhaven.com/a/industrial_workshop_foundry) | CC0. 1K~2K HDRI만 사용 |
| LIGHT-02 | 화물역 외부 하늘 기준 | [Freight Station](https://polyhaven.com/a/freight_station) | CC0. 배경보다 Reflection Probe 기준으로 사용 |
| SFX-01 | 기계·전기·금속·무기 소리 후보 | [Sonniss GDC 2026 Game Audio Bundle](https://gdc.sonniss.com/) · [라이선스](https://sonniss.com/gdc-bundle-license/) | 무료·상업 사용 가능·표기 불필요. 전체 7.47GB를 프로젝트에 넣지 말고 선택 파일만 사용 |

`Flooded Grounds`, `Sci-Fi Styled Modular Pack`은 무료지만 주 조합에 넣지 않는다. 전자는 오래된 대형 도시 장면이라 화물역 실내와 재질 정리 비용이 크고, 후자는 깨끗한 SF 모듈 비중이 높아 “오래 사용한 공공 산업 시설”이라는 방향과 충돌한다. 여기서 제외한 `Sci-Fi Styled Modular Pack`은 이번에 채택한 Sickhead Games의 `Sci-Fi Construction Kit (Modular)`와 다른 제품이다.

### 조사 후 제외한 주인공 후보

| 에셋 | 제외 이유 |
|---|---|
| [Engineer character sci-fi](https://sketchfab.com/3d-models/engineer-character-sci-fi-fe39548e9eba43858280ef917cc816e3) | 페이지 댓글에 상용 게임 캐릭터 추출물이라는 구체적인 지적이 있고, 다운로드 구성도 불완전하다는 보고가 있다. 표시된 CC BY만 믿기에는 원저작권 위험이 커서 **사용 금지**한다. |
| [Hazmat Suit 3](https://sketchfab.com/3d-models/hazmat-suit-3-b4a96fe273c0431fabd54c9d65926d59) | 외형은 맞지만 다운로드 후 메시·재질이 분리되고 텍스처 설정이 깨진다는 사용자 보고가 여러 건이다. 직접 복구 작업이 늘어나므로 **사용 금지**한다. |
| [Spacesuit (Confederation of Planets)](https://sketchfab.com/3d-models/spacesuit-confederation-of-planets-da7b5c14bc634f8283e2d21343afa7cc) | 밀폐 우주복 인상이 화물역 현장 작업자보다 강하고 리그 상태가 불명확하다. Striker14 채택으로 **후보에서 제외**한다. |

`CHR-01`은 시각·기획 기준으로 채택했지만 Unity 제작용 승인은 아직 아니다. 격리 장면에서 Humanoid 변환과 조준 자세를 통과하기 전에는 Starter Assets 캐릭터를 회색박스 대역으로 유지한다.

## 3. 설치 순서

한꺼번에 설치하면 어떤 에셋이 오류를 만들었는지 알기 어렵다. 아래 순서를 지킨다.

### 1단계 — 환경 한 개

1. 프로젝트를 Git 커밋으로 깨끗하게 만든다.
2. Unity Asset Store에서 `ENV-01`을 내려받고 필요한 패키지만 새 테스트 장면에 import한다.
3. 콘솔 오류, 분홍 재질, 1m 스케일, 콜라이더, 라이트맵 UV를 확인한다.
4. 원본은 `Assets/ThirdParty/SickheadGames/SciFiConstructionKit`에 두고, 프로젝트용 재질과 Prefab Variant는 `Assets/BLACKOUT`에 둔다.
5. 60fps와 빌드를 통과하면 환경을 임시 승인한다.

### 2단계 — 주인공과 애니메이션

1. `CHR-01`을 FBX 또는 glTF로 내려받고 라이선스 캡처를 저장한다.
2. Unity Import Settings에서 Scale 1, Rig `Humanoid`, Avatar `Create From This Model`을 시험한다.
3. 배포본의 리깅 모델을 먼저 사용한다. Humanoid 변환이 실패할 때만 Mixamo 자동 리깅을 한 번 시도한다.
4. Idle, Walk, Run, Jump, Aim, Fire, Hit, Death 클립을 우선 확보한다.
5. 손이 총에서 떨어지거나 어깨가 뒤틀리면 주인공 후보를 교체한다. 뼈를 대량 수정하지 않는다.

### 3단계 — 적 세 종류

`ENM-01 → ENM-02 → BOSS-01` 순서로 각각 별도 커밋에서 import한다. 한 화면에 세 모델을 놓고 크기, 금속 거칠기, 센서 색을 맞춘 뒤 승인한다. 일반 적과 보스의 높이 비는 약 `1 : 1.8~2.2`를 목표로 한다.

### 4단계 — 총, VFX, 오디오

총의 손잡이 피벗과 총구 Transform을 먼저 확인한다. 이후 VFX와 소리는 원본 전체를 넣지 말고 실제 사용하는 파일만 프로젝트 폴더로 복사한다.

## 4. 프로젝트 폴더 규칙

```text
BlackOut/Assets/
├─ BLACKOUT/
│  ├─ Art/Materials
│  ├─ Art/Prefabs
│  ├─ Audio
│  ├─ Scenes
│  ├─ Scripts
│  ├─ UI
│  └─ VFX
└─ ThirdParty/
   ├─ SickheadGames/SciFiConstructionKit
   ├─ ProductionCrate/ModernIndustrialPack
   ├─ Sketchfab/CreatorName/AssetName
   ├─ UnityTechnologies/ParticlePack
   ├─ Mixamo
   ├─ PolyHaven
   └─ Sonniss
```

외부 원본은 `ThirdParty`, 우리가 만든 Material·Prefab Variant·Controller는 `BLACKOUT`에 둔다. 외부 프리팹을 직접 수정하지 않고 Prefab Variant로 감싼다.

## 5. 에셋별 합격 기준

### 환경

- URP Lit에서 분홍 재질이 없다.
- 문 높이 약 2.1m, 난간 약 1.0m 등 사람 기준 스케일이 맞는다.
- 모듈 경계에서 빛샘과 발 걸림이 없다.
- 보스 경기장 25~35m 폭을 구성할 부품이 있다.
- 장면 전체를 가져오지 않아도 필요한 모듈을 분리해 쓸 수 있다.

### 주인공

- Humanoid Avatar가 오류 없이 생성된다.
- 8방향 이동·조준 중 팔과 허리가 심하게 꼬이지 않는다.
- 2K 이하 텍스처와 4개 이하 재질 슬롯으로 정리 가능하다.
- 등 배터리 가방을 붙일 공간과 명확한 상체 실루엣이 있다.

### 일반 적과 보스

- 루트 이동과 애니메이션 이동을 분리할 수 있다.
- 머리/센서, 몸통, 약점 Transform을 지정할 수 있다.
- 포탑은 받침·Yaw·Pitch·총열 축을 분리할 수 있다.
- 보스는 Walk 외 공격을 Unity에서 추가해도 관절 구조가 버틴다.
- 최종 빌드에서 LOD 전환과 사망 비활성화가 정상 동작한다.

## 6. 즉시 탈락 조건

- 유료로 바뀌었거나 구독이 필요한 에셋
- CC BY-NC, CC BY-ND, Editorial, 출처가 불명확한 재업로드
- 다른 게임·영화에서 추출했다고 적힌 모델
- KayKit, Kenney, Quaternius, Synty 계열처럼 의도적으로 장난감/카툰 비율인 팩
- Unreal Engine 형식만 제공하거나 다른 엔진 사용이 제한된 Fab 콘텐츠
- AI 생성 메시, 500k triangles 이상인데 LOD가 없는 일반 적
- 커스텀 셰이더를 고쳐야만 URP에서 보이는 오래된 에셋

## 7. 라이선스 기록 방법

Sketchfab CC BY는 제작자와 원본 링크를 표시해야 한다. Unity Asset Store 에셋은 게임에 포함해 사용할 수 있지만 원본 파일을 별도 에셋처럼 재배포하면 안 된다. Mixamo는 Adobe ID로 무료이며 게임 사용이 가능하다. 자세한 근거는 다음 원문을 기준으로 한다.

- [Unity Asset Store EULA](https://unity.com/legal/as-terms)
- [Unity Asset Store — Sci-Fi Construction Kit (Modular)](https://assetstore.unity.com/packages/3d/environments/sci-fi/sci-fi-construction-kit-modular-159280)
- [ProductionCrate Modern Industrial Pack](https://www.productioncrate.com/objects/RenderCrate-Modern_Industrial_Pack)
- [Sketchfab 다운로드 모델 표기 지침](https://sketchfab.com/developers/download-api/guidelines)
- [Adobe Mixamo FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)
- [Sonniss GDC Bundle License](https://sonniss.com/gdc-bundle-license/)

다운로드할 때 `에셋명 / 제작자 / 원본 URL / 라이선스 / 다운로드 날짜 / 수정 내용`을 [에셋 원장](BLACKOUT_AssetManifest.json)에 채운다. 법률 자문이 아니라 프로젝트의 출처 추적 기록이다.

## 8. 이번 주 실제 행동

이번 주에는 `ENV-01`, `CHR-01`, `WPN-01`을 각각 격리 장면에서 확인한 뒤 TPS 이동·조준·기본 공격에 필요한 최소 구성만 플레이 장면에 넣는다. 환경은 완성 맵부터 만들지 말고 벽·바닥·천장·문·계단·통로·소품을 각각 1개 이상 써서 작은 창고 전투방을 만든다. `Modern Industrial Pack`은 새 환경과 같은 화면에서 어울리는 철골·배관 부품만 남긴다.
