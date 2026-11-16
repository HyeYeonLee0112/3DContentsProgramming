# T26 — 느린 원인을 측정하고 제작 실수를 실행 전에 찾기

프레임이 느리다고 아무 코드나 바꾸면 개선 이유를 설명하기 어렵다. 같은 장면·같은 행동으로 비용을 비교하고, 누락 참조와 ID 오류는 검사 도구로 찾는다.

선행: [T24](24-level-integration.md), [T25](25-settings.md) · 다음: [T27](27-release.md)

## 개념·책임

Profiler는 실행 시간·메모리를 관찰하는 도구다. ProfilerMarker는 코드의 특정 구간에 붙이는 측정 이름이다. 60fps의 프레임 예산은 약 16.67ms이며 CPU·GPU·대기 시간을 구분해야 한다. Editor 숫자를 Windows 빌드 성능과 같다고 보지 않는다.

## 변경 계약

| 대상 | 작성할 멤버 | 역할·실패 |
|---|---|---|
| Motor의 측정 구간 | `private static readonly Unity.Profiling.ProfilerMarker s_TickMarker = new("Blackout.PlayerMotor.Tick")` | 한 번 생성; Auto scope로 Tick 본문 감쌈 |
| `Scripts/Editor/BlackoutSceneValidator.cs` | `public static class BlackoutSceneValidator`; `[UnityEditor.MenuItem("BLACKOUT/Validate Active Scene")] public static void ValidateActiveScene()` | 읽기 전용 검사 실행 |
| Validator | `private static int ValidateIds()`; `private static int ValidateReferences()`; `private static int ValidateRanges()` | 오류 개수 반환; 객체 문맥을 포함한 로그 |
| 결과 기록 | 측정 조건·전후 캡처·오류 목록 | 통과 사실과 미확인 항목 구분 |

Gun·Coordinator를 추가 측정할 때도 정확한 필드·marker 이름을 계약에 기록한다. 검사 함수는 값을 자동 수정하거나 장면을 저장하지 않는다.

## 질문·의사코드

프레임의 긴 부분이 VSync 대기라면 AI를 최적화해도 FPS가 오르는가? 메모리가 한 번 증가한 것과 계속 증가하는 것은 같은 문제인가?

```text
재현 경로 고정 → Development Build 연결 → 원인 구간 찾기
가설 하나 → 변경 하나 → 같은 경로 반복 → 느린 프레임·화면 품질 비교
제작 검사: ID → 필수 참조 → 수치 범위 → 오류 위치 보고
```

## TODO 실습

```csharp
// 기존 메서드 내부를 측정할 때의 틀이다.
using (s_TickMarker.Auto())
{
    // TODO: 새 코드를 복제하지 않고 기존 측정 구간을 넣는다.
}
```

이 예제의 `s_TickMarker`는 위 계약에 따라 직접 선언해야 한다. 힌트: Deep Profile을 처음부터 켜지 않기 → CPU Timeline의 호출 관계 확인 → GC Alloc은 반복 주기와 함께 보기.

## 따라하기·실험 순서

1. CPU·GPU·RAM·해상도·품질·VSync/목표 프레임·빌드 종류를 기록한다. 워밍업 뒤 같은 60초 경로를 3회 측정한다.
2. CPU Timeline/Hierarchy에서 이동·AI·Physics·복원·GC·대기를 구분한다. GPU 측정이 지원 안 되면 미측정이라고 적는다.
3. 정전/통전 화면의 추가 조명·그림자·투명 효과를 비교한다. Frame Debugger로 렌더링 패스를 살펴본다. 빛을 꺼 프레임만 올리고 가독성을 잃지 않는다.
4. 탄환 100발·전투 60초·사망 복원 10회를 반복한다. 관리 메모리·Native/GPU 메모리의 증가 원인을 분리하고 필요 시 Memory Profiler를 도입해 두 안정 시점 Snapshot을 비교한다.
5. Profile Analyzer를 도입하면 동일 구간의 분포·느린 프레임을 비교한다. 패키지 버전은 설치 때 확인하고 도구 이름만 완료 표시하지 않는다.
6. 검사 메뉴로 중복 시설/적 ID·누락 소켓/총구/Health 참조·불가능한 전력 요구량을 보고한다. 오류는 Debug.LogError(message, target)처럼 선택 가능한 문맥을 제공한다.
7. 일부러 오류 3개를 만든 작업 복사본에서 전부 발견되는지 확인한다. 읽기 전용 검사 후 장면 dirty 상태와 Git diff가 바뀌지 않아야 한다.

## 완료 기준

- [ ] Windows 빌드의 CPU/GPU/대기 차이를 근거로 설명한다.
- [ ] 같은 조건 전후 측정과 느린 프레임 자료가 있다.
- [ ] 반복 전투·복원 뒤 메모리 증가를 추적했다.
- [ ] 누락·중복·범위 오류를 수정 위치와 함께 찾는다.
- [ ] 검사만으로 장면이 바뀌지 않으며 남은 성능 한계를 기록했다.

## 선택과 한계

측정은 최적화 우선순위를 정하지만 그 자체로 개선하지 않는다. 검사 도구도 실행 비용이 드므로 제작 오류가 반복되는 항목부터 추가한다. 실제 수정 도구는 Undo·Prefab·직렬화·저장을 별도 설계하며 읽기 전용 검사와 섞지 않는다.

[Unity Profiler 공식 안내](https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html)
