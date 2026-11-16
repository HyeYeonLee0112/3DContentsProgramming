# E03 — 취소한 작업의 늦은 완료를 버리기

A 요청 후 B 요청을 시작했는데 A가 늦게 끝나 B의 화면을 덮을 수 있다. 기다리는 방식보다 결과가 아직 유효한지 확인하는 규칙부터 배운다.

선행: [T22](22-checkpoint-restore.md) · 다음: [E04](E04-addressables.md) · 로드맵 X02

## 책임과 구현 계약

| 파일 | 필요한 멤버 | 의미 |
|---|---|---|
| `Scripts/Runtime/Experiments/RequestLab.cs` | `private int _generation`; `private System.Threading.CancellationTokenSource _cts`; `private bool _alive` | 현재 요청·취소·소유자 수명 |
| Lab | `public void Begin(float delaySeconds)`; `public void Cancel()`; `private void OnEnable()/OnDisable()` | 새 요청은 이전 취소·세대 증가; 비활성화 취소·해제 |
| Lab | `private async System.Threading.Tasks.Task RunAsync(int generation, float delaySeconds, System.Threading.CancellationToken token)` | 대기 후 취소·세대·수명 재검사, 예외 관찰 |

실험은 Task.Delay처럼 Unity 객체를 건드리지 않는 대기로 시작한다. 결과의 UI 적용은 Unity 메인 스레드에서 실행됨을 보장한다. fire-and-forget Task의 예외를 버리지 않도록 내부 try/catch로 예상 취소와 실제 오류를 구분한다. 실제 Unity 프레임 연동에서는 해당 버전의 Awaitable 또는 코루틴과 비교한다.

## 질문·의사코드

대기자가 취소됐다고 네트워크 요청이나 에셋 로드 자체가 즉시 중단됐다고 볼 수 있는가?

```text
Begin → 이전 취소 → 세대 증가 → 내 세대·token 보관
대기 → token 취소/소유자 종료/세대 다름이면 결과 폐기
모두 유효할 때만 결과 적용 → 자원 정리
```

## TODO 실습

```csharp
public void Cancel()
{
    // TODO: 현재 작업을 무효화하고 token source를 정리한다.
    // 취소와 성공 결과 적용을 동시에 수행하지 않는다.
}
```

힌트: 완료 순서와 요청 순서는 다름 → 취소 예외를 정상 경로로 구분 → 세대 비교는 실제 결과 적용 직전.

## 따라하기·연결

1. 실험 Canvas에 A(긴 대기), B(짧은 대기), 취소 버튼과 현재 결과 라벨을 둔다.
2. A→B 순서로 누르고 먼저 끝난 B가 유지되는지 본다. 취소 검사 없이 한 번 실패를 재현한다.
3. 세대·token 검사를 추가하고 같은 순서를 반복한다. 시작/완료/폐기 로그에 세대를 넣는다.
4. 대기 중 Lab 비활성·장면 종료·재시작을 시험한다. 파괴된 UI를 호출하거나 예외가 관찰 없이 남으면 실패다.
5. Update 타이머·코루틴·Awaitable과 시간 기준·취소 표현·디버깅 비용을 비교한다. 짧은 0.7초 흡수는 복잡한 async 도입 없이도 완료 가능하다.

## 완료 기준

- [ ] 오래된 요청의 결과가 최신 결과를 덮지 않는다.
- [ ] 취소·비활성·종료 후 결과 적용이 없다.
- [ ] 예상 취소와 실제 예외를 구분해 기록했다.
- [ ] 단순 타이머와 비교해 도입 이유를 설명했다.

비동기는 기다리는 흐름을 표현하며 CPU 병렬 처리와 같지 않다. 실제 로드 해제 문제는 [E04](E04-addressables.md)에서 별도로 다룬다.
