# E09 — 시설 검사 결과를 UI Toolkit 편집 창으로 보기

Console에 오류가 많으면 어느 시설을 고칠지 찾기 어렵다. 이미 만든 검사 규칙을 화면으로 보여주고 클릭해서 문제 객체를 선택한다.

선행: [T26](26-profiler-validation.md) · 로드맵 X07 · [전체 목록](README.md)

## 책임·구현 계약

| 파일/타입 | 필요한 멤버 | 책임 |
|---|---|---|
| `Scripts/Editor/BlackoutValidationWindow.cs` | `public class BlackoutValidationWindow : UnityEditor.EditorWindow`; `[MenuItem("BLACKOUT/Validation Window")] public static void Open()`; `public void CreateGUI()`; `private void RefreshResults()` | 창·레이아웃·새로고침 |
| 검사 결과 모델 | 메시지·오류 수준·UnityEngine.Object 문맥 | 검사 규칙과 표시 분리 |
| T26 Validator 변경 | 읽기 전용 결과 목록 반환 추가 | Console·창에서 같은 규칙 사용 |

UI Toolkit은 VisualElement 트리로 UI를 구성하는 체계다. EditorWindow를 선택해 실험하므로 본편 uGUI HUD를 모두 재작성하지 않는다.

## 질문·의사코드

창을 다시 열 때 버튼 콜백이 두 번 붙는가? 단순 검사 버튼을 눌렀는데 장면이 수정되어도 되는가?

```text
CreateGUI → 버튼·목록 구성
새로고침 → 기존 결과 교체 → 객체 문맥과 메시지 표시
행 클릭 → Selection.activeObject → Inspector에서 수정 위치 확인
```

## 따라하기

1. T26의 검사 로직을 창 코드에 복사하지 않는다. 검사가 결과 목록을 반환하도록 작은 계약을 먼저 추가한다.
2. C# VisualElement만으로 새로고침 버튼과 결과 목록을 만든다. 레이아웃 요구가 늘면 UXML/USS로 옮겨 비교한다.
3. 오류 행에 객체 이름·ID·원인·수정 위치를 표시하고 클릭하면 Unity Selection을 바꾼다.
4. 창 열기/닫기 10회, 도메인 리로드, 대상 삭제 후 새로고침을 시험한다.
5. 선택 기능이 안정된 뒤 별도 실험으로 중복 ID 수정 버튼을 추가할 수 있다. 수정 전 Undo.RecordObject, 직렬화·Prefab 변경 기록·장면 dirty 처리를 적용한다. 검사 버튼과 수정 버튼을 분리한다.
6. 수정했다면 Undo/Redo·저장 후 재열기에서 상태가 같은지 확인한다.

## TODO·완료 기준

RefreshResults를 먼저 구현하고 실행할 때마다 새 목록으로 교체한다. 이전 VisualElement나 콜백을 계속 추가하지 않는다.

- [ ] 같은 검사 규칙을 Console과 창이 공유한다.
- [ ] 문제 행에서 실제 객체를 선택할 수 있다.
- [ ] 반복 열기·삭제 대상·리로드에 중복이나 예외가 없다.
- [ ] 수정 실험을 했다면 Undo/Redo·Prefab·재열기를 검증했다.

전용 창은 반복 수정 시간을 줄이지만 UI 수명과 편집 상태 관리 비용이 있다. Console 로그의 문맥 객체만 제공해도 충분한 단계인지 비교한다.
