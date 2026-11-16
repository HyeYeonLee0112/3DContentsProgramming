# E04 — Addressables 에셋을 불러오고 정확히 해제하기

Inspector에서 항상 연결한 에셋이 많아지면 필요한 시점에만 로드하고 싶어진다. 로드 성공보다 핸들의 주인과 해제 시점을 먼저 정한다.

선행: [E03](E03-async.md) · 다음: [E05](E05-additive.md) · 로드맵 X01

## 개념·책임·계약

Addressables는 주소/참조로 에셋을 로드·관리하는 Unity 패키지다. 별도 장면과 무료 대역 프리팹 한 개로 시작한다. 설치 시 프로젝트 호환 버전의 공식 문서를 확인한다.

| 실험 파일 | 필요한 멤버 | 역할 |
|---|---|---|
| `Experiments/AddressableLab.cs` | `private AsyncOperationHandle<GameObject> _handle`; `private bool _ownsHandle`; `private GameObject _instance`; `private int _generation` | 로드 핸들·인스턴스·최신 요청 |
| Lab | `public void Load(string address)`; `public void Unload()`; `private void OnDisable()` | 같은 요청 중복 방지, 늦은 완료 정리 |

위 핸들 타입은 `UnityEngine.ResourceManagement.AsyncOperations`에 있고 패키지 도입 후에만 컴파일된다. 기본 실험은 LoadAssetAsync 후 일반 Instantiate를 사용하고 **인스턴스 Destroy 후 로드 핸들 Release**로 짝을 맞춘다. InstantiateAsync를 쓰는 비교에서는 ReleaseInstance와 짝을 맞추며 두 방식을 섞지 않는다.

## 질문·의사코드

사용자가 취소를 눌렀는데 로드는 끝났다면 누가 그 핸들을 해제하는가? 프리팹 인스턴스가 살아 있는데 원본 핸들을 먼저 놓아도 되는가?

```text
로드 요청별 로컬 핸들 소유 → 성공·최신성 확인 → 인스턴스 생성
실패/늦은 완료 → 그 요청의 핸들 해제
사용 종료 → 인스턴스 제거 → 대응 핸들 한 번 해제
```

## 따라하기

1. 실험 전 메모리 기준과 현재 직접 참조 방식을 기록한다. Package Manager에서 호환 버전을 선택한다.
2. 무료 대역 프리팹 한 개를 Addressable로 지정하고 고정 주소를 부여한다. 그룹·콘텐츠 빌드·Play Mode Script 차이를 확인한다.
3. Load/Unload 버튼을 연결한다. 로컬 요청 핸들을 콜백이 보관하게 하여 B가 시작된 뒤 A의 핸들로 필드를 덮지 않게 한다.
4. 없는 주소·로드 실패·완료 전 Unload·연타·비활성화를 시험한다. 오류 핸들도 소유했다면 유효성에 맞춰 해제한다.
5. 20회 로드·사용·해제하고 안정 시점 메모리와 핸들 수를 비교한다. 패키지 캐시·공유 의존성이 남는 것과 살아있는 인스턴스를 구분한다.
6. 콘텐츠 빌드를 포함한 Windows 실행 파일에서도 시험한다. Editor의 Asset Database 모드만 확인하고 완료하지 않는다.

## TODO 힌트

`Unload`를 먼저 한 문장으로 쓰고, 핸들마다 “누가 언제 한 번 해제하는지” 표를 만든다. API는 설치 버전의 LoadAssetAsync/Release를 읽되 취소와 언로드를 같은 의미로 취급하지 않는다.

## 완료 기준

- [ ] 정상·실패·늦은 완료의 핸들 주인이 분명하다.
- [ ] 20회 사용 후 인스턴스·참조가 계속 증가하지 않는다.
- [ ] 콘텐츠 빌드가 있는 Windows 실행 파일에서 검증했다.
- [ ] 직접 Inspector 참조보다 나은 점·추가 비용을 비교했다.
