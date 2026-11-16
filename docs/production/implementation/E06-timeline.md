# E06 — 보스 등장 연출을 건너뛰어도 같은 게임 상태 만들기

연출을 끝까지 봐야만 문이 열리면 건너뛰기·중단 후 진행이 깨진다. 게임의 최종 상태와 시간축 표현을 나눈다.

선행: [T23](23-boss.md) · 로드맵 X04 · [전체 목록](README.md)

## 개념·책임·구현 계약

Timeline은 카메라·애니메이션·소리를 시간축에 배치하는 도구다. Signal은 특정 시점 알림이다. 필수 진행 상태를 Signal 한 번에만 의존시키지 않는다.

| 파일/대상 | 계약 |
|---|---|
| `Experiments/BossIntroLab.cs` | `[SerializeField] private UnityEngine.Playables.PlayableDirector _director`; `private bool _finished`; `public void Play()`; `public void Skip()`; `private void Finish()`; `private void OnDisable()` |
| Play | 입력 허용을 흐름 담당에 요청하고 Director 재생; 중복 재생 거절 |
| Finish | 한 번만 연출 최종 배치·입력 허용 요청; 정상 종료와 Skip이 함께 사용 |
| OnDisable | 진행 연출 정리·알림 해제; 사망 상태를 임의로 Playing으로 바꾸지 않음 |

## 질문·의사코드

0.5초에 Skip하거나 마지막 프레임에 사망해도 보스 위치·문·카메라 상태가 일치해야 하는가?

```text
연출 시작 → 표현 재생
정상 완료/Skip → 한 번 Finish → 확정한 최종 상태 → 흐름 재개
외부 사망/복원 → 중단·초기화, 정상 완료 보상 호출 금지
```

## 따라하기

1. 대역 보스·문·카메라로 3초 등장 Timeline을 만든다. 이미 설치된 Timeline 버전과 Director 설정을 확인한다.
2. PlayableDirector의 장면 바인딩을 연결하고 카메라를 동시에 제어하는 컴포넌트가 없는지 본다.
3. Play/Skip 버튼과 Finish를 작성한다. Stop의 콜백이 Finish를 다시 호출해도 중복되지 않게 가드한다.
4. Signal은 발소리·조명 같은 표현에 사용하고 필수 보스 준비는 명시적 최종 처리로 보장한다.
5. 시작 직후·중간·마지막 Skip, 사망, 복원, 재실행을 각각 확인한다.

## TODO·검증 기준

먼저 Finish가 바꾸는 값을 종이에 나열하고 “이미 끝났으면 아무 것도 하지 않음”을 작성한다. 그 뒤 정상 종료와 Skip을 연결한다.

- [ ] 어떤 Skip 시점에서도 확정한 최종 배치가 같다.
- [ ] 사망·복원 중 입력을 잘못 다시 허용하지 않는다.
- [ ] 완료·중단 알림이 중복되지 않는다.
- [ ] Timeline 도입 전후 연출 편집 비용을 기록했다.

코드 타이머보다 편집·미리보기는 쉬워지지만 바인딩·중단·최종 상태 관리가 추가된다.
