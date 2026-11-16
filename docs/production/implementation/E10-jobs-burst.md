# E10 — 많은 대상의 거리 계산을 Jobs·Burst와 비교하기

적이 많아질 때 계산을 분산하면 빨라질 수 있지만, 작업 준비·메모리 복사·기다림도 비용이다. Unity 객체를 건드리지 않는 거리 계산만 분리해 비교한다.

선행: [T26](26-profiler-validation.md) · 다음: [E11](E11-ecs.md) · 로드맵 X08

## 개념·구현 계약

Job은 정해진 데이터 작업을 Unity의 작업 시스템에 맡기는 단위다. Burst는 지원되는 데이터 코드의 실행을 최적화한다. NavMeshAgent·GameObject를 Job 안에서 직접 조작하는 실험이 아니다.

| 파일/타입 | 필요한 멤버 | 규칙 |
|---|---|---|
| `Experiments/DistanceJob.cs` | `struct DistanceJob : Unity.Jobs.IJobParallelFor`; `[ReadOnly] public NativeArray<float3> Positions`; `public float3 Origin`; `[WriteOnly] public NativeArray<float> SquaredDistances`; `public void Execute(int index)` | 각 index는 자기 결과만 기록 |
| `Experiments/DistanceBenchmark.cs` | Positions/Results NativeArray, JobHandle·예약 여부 필드; `public void Run(int count)`; `private void OnDisable()` | 생성·스케줄·Complete·Dispose 소유 |
| 순차 기준 | 같은 입력과 같은 제곱거리 식 | sqrt 생략 조건 동일 |

패키지·namespace는 도입 버전의 Collections/Mathematics/Burst 문서와 실제 설치를 확인한다. NativeArray는 관리 배열과 달리 해제 시점을 직접 관리한다.

## 질문·의사코드

100개에서는 작업 예약 비용이 계산보다 클 수 있는가? Schedule 직후 Complete하면 겹쳐 실행할 다른 일이 남는가?

```text
고정 입력 생성 → 순차 계산 결과 기준 저장
데이터 준비 → Job 예약 → 필요한 시점 Complete → 결과 비교 → 메모리 해제
준비·복사·대기 포함 총시간도 측정
```

## 따라하기

1. 100/1,000/10,000개 고정 좌표를 만들고 순차 C# 결과를 기준으로 둔다.
2. Transform 위치를 메인 스레드에서 값 배열로 옮긴다. Job 내부에는 값 데이터만 들어간다.
3. Execute에서 `(position-origin)`의 제곱 길이를 계산한다. 결과 배열 크기와 인덱스 범위를 검증한다.
4. Schedule/Complete 구간과 전체 준비+복사+실행 시간을 따로 측정한다. Burst 첫 컴파일·워밍업 시간을 구분한다.
5. 실행 도중 비활성화할 때 먼저 Complete 후 Dispose한다. 이중 Dispose·미완료 상태 해제를 일부러 검사한다.
6. 결과 허용 오차·성능·메모리·복잡성을 비교한다. 실제 본편의 작은 적 수에서 이득이 없으면 실험으로 남긴다.

## TODO·완료 기준

Execute에는 한 index의 식만 쓰고, Complete와 Dispose는 소유자에 둔다. 계산 결과가 맞는지 확인하기 전 성능 결과를 채택하지 않는다.

- [ ] 순차와 Job 결과가 허용 오차 안에서 같다.
- [ ] 준비·복사·동기화 포함 총비용을 비교했다.
- [ ] 중단·종료에서도 NativeArray 누수가 없다.
- [ ] 본편 규모에서 통합할 이유 또는 보류 이유가 있다.
