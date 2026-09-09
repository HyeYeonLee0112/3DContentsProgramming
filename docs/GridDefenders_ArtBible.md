# Grid Defenders — Art Bible & 에셋 소싱 가이드

> 3D 콘텐츠 프로그래밍 기말 프로젝트
> 작성: 혜연 · 최종수정: 2026-09-03
> 관련 문서: [[GridDefenders_PRD]]

---

## 0. 이 문서의 목적

이 프로젝트는 아트를 직접 제작하지 않는다(프로그래밍에 집중). 따라서 아트 작업은
**무료 CC0 에셋을 골라 오는 일**이 전부이고, 그 과정에서 유일하게 중요한 판단 기준은
**"전부 한 세트처럼 보이는가"** 하나다.

이 문서는:
1. 비주얼 방향성과 지켜야 할 통일 규칙을 정의하고 (§1~§3)
2. 카테고리별로 사용할 에셋을 확정하고 (§4)
3. 임포트·정규화 절차를 체크리스트로 남긴다 (§5~§6)

---

## 1. 비주얼 목표 (Art Direction)

| 원칙 | 의미 | 반대말(피할 것) |
|---|---|---|
| **읽힌다 (Readable)** | RTS 카메라 거리에서 타워·적을 실루엣만으로 구분 | 디테일 과다, 비슷한 실루엣 |
| **차분하다 (Calm)** | 무광 표면, 낮은 채도, 약한 이펙트 | 번쩍이는 반사, 강한 블룸, 파티클 도배 |
| **정돈됐다 (Clean)** | 일관된 폴리 밀도, 아웃라인 유무 통일, 팔레트 고정 | 여러 스타일 혼용, 텍스처 해상도 제각각 |
| **기능이 색을 지배한다** | 색은 게임 정보(적 타입, 배치 가능 여부)를 전달하는 신호 | 장식용 컬러, 의미 없는 그라디언트 |

**한 줄 정의**: "플랫 셰이딩 로우폴리, 고대비 실루엣, 저채도 팔레트, 최소 이펙트."

---

## 2. 스타일 기준: 단일 생태계 원칙

> **한 제작자의 에셋군으로 통일하면 통일감의 90%가 저절로 해결된다.**

- **주력: Kenney** (kenney.nl) — 플랫 셰이딩, 고대비, 실루엣 중심. 3D 모델 + 2D UI + 오디오 +
  폰트까지 전 카테고리를 한 스타일로 커버. "화려하지 않고 식별 잘 됨" 조건에 정확히 부합.
- **보조: Quaternius** (quaternius.com) — 애니메이션된 적/캐릭터가 필요할 때만.
  Kenney보다 비율이 사실적이고 텍스처 디테일이 많아 **3D끼리 섞으면 미묘하게 튄다.**

### 혼용 규칙 (엄수)

| 조합 | 허용 여부 |
|---|---|
| Kenney 3D + Kenney UI + Kenney Audio | ✅ 기본 |
| Kenney 3D 환경 + Quaternius 3D 적 (적 **전체**를 Quaternius로) | ⚠️ 조건부 허용 — 반반 금지 |
| Kenney 3D + Quaternius 3D를 같은 카테고리에서 반반 | ❌ 금지 (지저분함의 주원인) |
| Kenney/Quaternius + 에셋스토어 사실적 PBR 팩 | ❌ 금지 |
| Synty POLYGON 무료 샘플 추가 | ❌ 금지 (유료 팩과 섞이면 붕괴, 샘플만으론 부족) |

---

## 3. 통일감 체크리스트 (프로젝트 내내 지킬 것)

1. **스케일 정규화** — 전 에셋 "1 Unity 유닛 = 1 그리드 셀". Kenney 킷은 이미 이 기준으로 제작됨.
2. **머티리얼 단일화** — Kenney 각 킷은 색상 아틀라스(colormap) 1장 사용. 타워·적·환경을
   **같은 팔레트 안에서** 리컬러한다. 적 타입 구분은 이 팔레트의 accent 색만 교체.
3. **셰이딩 통일** — 전부 URP `Lit`, Smoothness 0~0.1(무광), Metallic 0. 광택 제거.
4. **라이팅 1세트** — Directional Light 1개 + Ambient(Gradient), 그림자 강도 통일.
   Baked GI로 톤 고정. Reflection Probe는 최대 1개.
5. **포스트프로세싱 최소** — 약한 Vignette + 미세 색보정(약간의 온도/틴트)만.
   Bloom Intensity ≤ 0.05. Chromatic Aberration / Film Grain / Depth of Field **OFF**.
6. **스카이박스·안개 1개** — 단색 또는 2색 그라디언트 스카이. 원경 Linear Fog로 뎁스 통일.
7. **바닥 팔레트 3색 고정** — 배치 가능 / 경로 / 장애물 셀을 **명도 대비**로 구분(색상 아님).
   → 색약 대응 + 식별성 확보.
8. **실루엣 우선** — 타워는 top-down 실루엣만으로 구분되게 디자인. 색은 보조 신호일 뿐.
9. **이펙트 억제** — 폭발은 0.4초 이내, 라이트 플래시 1프레임, 채도 억제.
   메인 비주얼 이펙트는 디졸브 셰이더가 담당.
10. **폰트 2종 고정** — 디스플레이용 1 + 본문용 1. 그 외 사용 금지.

---

## 4. 에셋 목록 (Asset List)

> 별도 표기 없으면 전부 **CC0 1.0** (상업 이용 가능, 출처 표기 불필요하나 크레딧에 기재 권장).
> 다운로드: 각 링크 또는 "Kenney Game Assets All-in-1" 번들.

### 4.1 3D — 환경 / 지형 / 구조물

| 카테고리 | 에셋 | 출처 | 용도 |
|---|---|---|---|
| 타일·경로·기본 타워·바위/나무 | **Tower Defense Kit** (160모델) | kenney.nl/assets/tower-defense-kit | 프로젝트 뼈대. 그리드 타일, 곡선 경로, 모듈형 타워 파츠 |
| 자연물 (숲/언덕/절벽/디테일) | **Nature Kit** (330모델) | kenney.nl/assets/nature-kit | 배치 불가 영역 장식 |
| 요새 / 코어 / 성벽 | **Castle Kit** (75모델) | kenney.nl/assets/castle-kit | 경로 끝 목표물, 배경 요새 |

### 4.2 3D — 적 유닛 / 캐릭터

| 카테고리 | 에셋 | 출처 | 용도 |
|---|---|---|---|
| 적 유닛 (리깅·애니메이션 포함) | **Mini Characters** (25) 또는 **Blocky Characters** (20) | kenney.nl/assets/mini-characters | Grunt / Runner / Tank — 색상만 교체해 구분. 스켈레탈 애니메이션 학습용 |
| 커맨더 / 일꾼 유닛 (선택) | Mini Characters 재활용 | — | 별도 캐릭터 추가 시에도 같은 팩에서 |
| (대안) 애니메이션 몬스터 | **RPG Character Pack** (6종 리깅·애니메이션) | quaternius.com/packs/rpgcharacters.html | Kenney 캐릭터 애니메이션이 부족하면 **적 전체를** 이걸로 통일 |
| (대안) 리타게팅 실습 | **Universal Base Characters** + **Universal Animation Library** | quaternius.com | Mixamo 호환 리타게팅 학습과 연결 |

### 4.3 2D — UI

| 카테고리 | 에셋 | 출처 | 용도 |
|---|---|---|---|
| 버튼 / 패널 / 슬라이더 | **UI Pack** (430) | kenney.nl/assets/ui-pack | HUD, 메뉴 기본 |
| 판타지/RPG UI 확장 | **UI Pack — RPG Expansion** (85) | kenney.nl/assets/ui-pack-rpg-expansion | 타워 정보 팝업, 프레임 |
| 아이콘 | **Game Icons** (105) | kenney.nl/assets/game-icons | 골드·라이프·업그레이드·타워 아이콘 |
| 폰트 | **Kenney Fonts** | kenney.nl/assets/kenney-fonts | UI 팩과 세트로 디자인됨 |

### 4.4 오디오

| 카테고리 | 에셋 | 출처 | 용도 |
|---|---|---|---|
| UI 사운드 | **Interface Sounds** | kenney.nl/assets/interface-sounds | 클릭, 배치, 경고 |
| 전투 SFX | **Impact Sounds** + **RPG Audio** | kenney.nl/assets | 발사, 타격, 사망 |
| BGM / 스팅어 | **Music Jingles** / **Music Loops** | kenney.nl/assets | 웨이브 시작·승리·패배, 배경음 |

### 4.5 VFX (Kenney는 3D 파티클이 약함 — 예외적으로 외부 사용)

| 에셋 | 출처 | 라이선스 | 비고 |
|---|---|---|---|
| **Particle Pack** (스프라이트) | kenney.nl/assets/particle-pack | CC0 | 스타일드 폭발/스파크. 1순위 |
| **Unity Particle Pack** | assetstore.unity.com (Unity 공식) | Unity 무료 | 사실적 톤 → **채도 낮추고 크기 축소** 후 사용. URP 대응 |

> 이펙트는 §3의 9번 규칙을 최우선으로 적용. "안 보이는 게 기본, 필요할 때만 짧게."

---

## 5. 임포트 & 정규화 절차

1. **폴더 구조**
   ```
   Assets/
     Art/
       _Kenney/TowerDefenseKit/
       _Kenney/NatureKit/
       _Kenney/CastleKit/
       _Kenney/Characters/
       _Kenney/UI/
       _Quaternius/...        (사용 시)
     Audio/_Kenney/
     VFX/
   ```
   원본 팩은 하위 폴더에 그대로 두고, 프로젝트용 프리팹·머티리얼은 별도 `Prefabs/`, `Materials/`에 생성.

2. **모델 임포트 설정 통일**
   - Scale Factor: 팩 기준 그대로 → 씬에서 1셀=1유닛 확인
   - Mesh Compression: Medium
   - Read/Write: 필요한 것만(절차적 메시 대상 제외)
   - Normals: Import / Import한 것 없으면 Calculate, Smoothing Angle 통일(예: 60)
   - Material: "Use Embedded" 대신 프로젝트 공용 머티리얼로 리맵

3. **머티리얼 통일**
   - Kenney colormap 텍스처 1장을 공유 머티리얼(URP Lit, Smoothness 0.05, Metallic 0)로
   - 적 타입별 accent는 `MaterialPropertyBlock` 틴트로 처리(머티리얼 인스턴스 증가 방지 — PRD §3.8)

4. **팔레트 확정**
   - Kenney colormap에서 프로젝트 팔레트 표를 뽑아 이 문서 §7에 색상값 기록
   - 이후 모든 UI·이펙트·데칼 색을 이 표에서만 선택

5. **라이팅·포스트 프로파일 저장**
   - `Settings/PostProcess_Global.asset`, 라이팅 세팅을 프리셋으로 고정
   - 씬마다 재설정 금지 — 프리셋 참조

---

## 6. 완료 기준 (Definition of Done — 아트 파트)

- [ ] 모든 3D 오브젝트가 동일 셰이더(URP Lit) + 무광 세팅
- [ ] 적 3종이 RTS 카메라 거리에서 실루엣/명도로 구분됨 (스크린샷으로 검증)
- [ ] 바닥 3상태(배치 가능/경로/장애물)가 흑백 변환 후에도 구분됨
- [ ] 포스트프로세싱: Bloom ≤ 0.05, Grain/Chromatic/DOF OFF
- [ ] UI가 단일 폰트 2종, 단일 팩(Kenney UI)으로 구성
- [ ] 크레딧 문서에 사용 에셋 전부 출처·라이선스 기재
- [ ] 서로 다른 제작자 에셋을 같은 카테고리에서 혼용하지 않음

---

## 7. 프로젝트 팔레트 (임포트 후 채울 것)

| 역할 | 색상값 (Hex) | 출처 |
|---|---|---|
| 지면 — 배치 가능 | | Kenney colormap |
| 지면 — 경로 | | |
| 지면 — 장애물 | | |
| 타워 공통 베이스 | | |
| 적 — Grunt accent | | |
| 적 — Runner accent | | |
| 적 — Tank accent | | |
| UI — 배경 패널 | | |
| UI — 강조/버튼 | | |
| 경고 / 위험 (라이프 감소 등) | | |

---

## 8. 크레딧 (빌드에 포함)

```
3D Models, UI, Audio: Kenney (kenney.nl) — CC0 1.0
[사용 시] Characters/Animations: Quaternius (quaternius.com) — CC0 1.0
[사용 시] VFX: Unity Technologies — Unity Particle Pack
```
