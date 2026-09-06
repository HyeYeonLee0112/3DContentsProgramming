# 3D콘텐츠프로그래밍 · 3D Contents Programming

> 강의 주차 주제로 기말 프로젝트 **Grid Defenders**(3D 타워 디펜스, Unity 6 URP)의 시스템을 하나씩 구현한 기록.

## 기말 프로젝트: Grid Defenders

그리드에 타워를 지어 경로를 따라 몰려오는 적을 막는 3D 타워 디펜스. 평가 대상은 게임 완성도가 아니라 **각 3D 기초 시스템의 구현 품질과 이해도**.

- 기획: [docs/GridDefenders_PRD.md](docs/GridDefenders_PRD.md)
- 아트: [docs/GridDefenders_ArtBible.md](docs/GridDefenders_ArtBible.md)

## 커리큘럼 & 구현 현황

**상태** ⬜ 예정 · 🟨 진행 · ✅ 완료
**깊이** `📖 개념` · `🎮 예제 재현` · `🔧 프로젝트 시스템으로 구현` · `🧩 통합` · `⚡ 측정·최적화`

| 주 | 강의 주제 | Grid Defenders 시스템 | 상태 | 깊이 | 증거 |
|--:|---|---|:--:|:--:|---|
| 1 | 설치·에디터·빌드 | 프로젝트 셋업 · GridManager · 좌표 변환 | ⬜ | | `Core/GridManager` |
| 2 | 입력·이동·회전 | RTS 카메라 · 지면 레이캐스트 (Physics vs Plane) | ⬜ | | `Camera/` |
| 3 | 프리팹·발사체 | Arrow 타워 · 호밍 투사체 · `ObjectPool<T>` | ⬜ | | `Towers/` `Core/ObjectPool` |
| 4 | 특수효과·NavMesh | NavMesh 적 이동 · NavMeshObstacle 타워 · 발사 이펙트 | ⬜ | | `Pathfinding/NavMeshPathProvider` |
| 5 | AI·코루틴 | WaveSpawner · WaveDataSO · 타깃팅 정책 | ⬜ | | `Enemies/WaveSpawner` |
| 6 | 이동·사격·효과 | 타워 3종 (스플래시·히트스캔) · 데미지 계산 | ⬜ | | `Towers/` |
| 7 | 적 캐릭터·Mecanim | 적 3종 애니메이션 · 디졸브 셰이더 사망 | ⬜ | | `Enemies/Enemy` |
| 9 | 리스폰·임의 표적 | 풀링 완성 · 절차적 맵 생성 (시드) | ⬜ | | `Procedural/MapGenerator` |
| 10 | 사망·체력·UI | HUD · 체력바 빌보드 · 배치 고스트 프리뷰 | ⬜ | | `UI/` |
| 11 | 게임 매니저 | GameManager 상태머신 · 이코노미 · 배속 · 승패 | ⬜ | | `Core/GameManager` |
| 13 | 제작 1 | **커스텀 A\*** — 바이너리 힙 · 옥타일 휴리스틱 · 퍼널 스무딩 · IPathProvider 토글 | ⬜ | | `Pathfinding/AStarPathfinder` |
| 14 | 제작 2 | 최적화 — 인스턴싱 · MPB · LOD/컬링 · Job System | ⬜ | | Profiler 리포트 |
| 15 | 발표 | 빌드 · 영상 · 기술 문서 · 슬라이드 | ⬜ | | `[프로젝트 이슈](../../issues)` |

병렬: `[A*]` 커스텀 경로탐색 · `[최적화]` Profiler 리포트 · `[기술 문서]` · `[아트]` 에셋 소싱 · `[필기]` 주간 회상 노트 — Issues 참고

## 다룬 범위 / 다루지 않은 범위

- **다룸** (PRD §2 — 17항목): 변환 수학, 레이캐스팅, RTS 카메라, NavMesh + 커스텀 A\*, 스티어링, 오브젝트 풀링, GPU 인스턴싱, VFX, 절차적 생성, LOD/컬링, 조명, 셰이더, 충돌, 3D 오디오, UI, 씬/상태 관리, Job System/Burst
- **안 다룸**: 멀티플레이·네트워킹, 스토리·컷신, 세이브·메타 프로그레션, 모바일·컨트롤러, 에셋 직접 제작(CC0 에셋 사용)

## 실행

Unity 6 (URP). `docs/` 기획·아트 문서. 빌드·조작법은 프로젝트 완성 후 이 README에. 에셋은 Git LFS, `Library/` `Temp/` 제외.

## 진행 상황

주차별 할 일: [Issues](../../issues) · `[프로젝트] Grid Defenders` 이슈에 전체 시스템 트래킹

## 수업 정보

[docs/강의계획서.md](docs/강의계획서.md)
