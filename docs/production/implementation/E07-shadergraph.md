# E07 — 전력 상태를 전달하는 Shader Graph 만들기

전력 표시를 재질 색만 바꿔 전달하면 통전 과정과 약점 상태가 잘 읽히지 않을 수 있다. 단순한 흐름·발광을 만들어 정보 전달과 GPU 비용을 함께 비교한다.

선행: [T15](15-facilities.md) · 로드맵 X05 · [전체 목록](README.md)

## 책임·설정 계약

Shader Graph는 노드를 연결해 GPU에서 표면을 그리는 규칙을 만드는 도구다. 게임의 전력량을 직접 수정하지 않는다.

| 대상 | 계약 |
|---|---|
| `Art/Materials/LabPower.shadergraph` | 현재 URP 설정의 Lit/Opaque 그래프; 노출 float `_Power01` 0~1 |
| Material | 베이스 색·청록 발광·검증한 흐름 텍스처 |
| `Presentation/VFX/PowerMaterialPresenter.cs` | `[SerializeField] private Renderer _renderer`; `[SerializeField] private FacilityPower _facility`; `private MaterialPropertyBlock _block`; `private void OnEnable()/OnDisable()`; `private void Refresh()` |
| Presenter | 통전 결과를 읽어 property 갱신; 원본 공유 재질을 실행 중 일괄 수정하지 않음 |

## 질문·의사코드

같은 Material을 쓰는 시설 두 개에서 하나만 켜져야 한다면 값을 어디에 둬야 하는가?

```text
현재 상태 읽기 → 0~1 표현값 → 그래프 속성
전력 규칙은 그대로 → 표시만 A/B 비교 → GPU와 가독성 관찰
```

## 따라하기

1. 원본 재질을 보존하고 실험용 불투명 URP Graph를 만든다. 단색에서 시작한다.
2. `_Power01`을 BaseColor/Emission 변화에 연결하고 0/1 상태를 확인한다.
3. 꼭 필요할 때만 UV 흐름과 마스크를 추가한다. 그래프가 실제 시설 상태를 유추하는 로직을 갖지 않게 한다.
4. 두 시설에 같은 Material을 적용한 뒤 서로 다른 상태를 준다. MaterialPropertyBlock을 썼다면 SRP Batcher·드로우 호출에 미치는 영향도 Frame Debugger로 확인한다.
5. Bloom 유무·정전·멀리서 보기·색각 구분을 비교한다. 투명 효과를 쉽게 추가해 overdraw를 늘리지 않는다.

## TODO·완료 기준

Refresh의 입력은 FacilityPower.IsPowered 하나다. 원하는 색에서 역으로 전력값을 계산하지 말고 상태→표시 방향으로 연결한다.

- [ ] 같은 Material의 두 시설을 독립 표시한다.
- [ ] 정전/통전/약점 정보가 읽힌다.
- [ ] 그래프와 Material 변경이 전력 규칙을 바꾸지 않는다.
- [ ] GPU·드로우 호출·가독성 비교를 기록했다.

단순 Lit 재질의 Emission만 조절해도 충분하면 그 대안을 채택할 수 있다. 그래프 노드 수가 많다는 이유로 완성도를 높게 보지 않는다.
