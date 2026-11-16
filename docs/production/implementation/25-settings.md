# T25 — 감도·음량·키 설정을 바꾸고 다시 실행하기

게임을 껐다 켤 때 감도가 초기화되거나 키를 바꿨는데 실제 플레이에는 반영되지 않으면 설정이 완성된 것이 아니다. 실제 사용 중인 입력 객체와 저장값을 연결한다.

선행: [T20](20-game-flow.md) · 다음: [T26](26-profiler-validation.md)

## 책임·구현 계약

| 파일 | 필요한 멤버 | 역할·실패 |
|---|---|---|
| `Infrastructure/Settings/PlayerSettingsData.cs` | `[System.Serializable] public class PlayerSettingsData`; `public int version = 1`; `public float sensitivity = 0.1f, masterVolume = 1f`; `public bool invertY`; `public string bindingOverrides` | 설정값만 저장, 진행 상태 제외 |
| `Infrastructure/Settings/SettingsService.cs` | `public PlayerSettingsData Current { get; private set; }`; `public void Load()`; `public void Save()`; `public void ResetToDefaults()` | 파일 누락·손상은 기본값과 이유 로그; 범위 검증 |
| Service | `private string SettingsPath { get; }` | Application.persistentDataPath 아래 고정 파일명 |
| Reader 추가 | `public UnityEngine.InputSystem.InputActionAsset Actions => _actions.asset` | 현재 게임이 쓰는 같은 입력 인스턴스 노출 |
| `Presentation/UI/RebindButton.cs` | `[SerializeField] private PlayerInputReader _reader`; `private UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation _operation`; `public void BeginRebind(string actionName, int bindingIndex)`; `public void CancelRebind()`; `private void OnDisable()` | 재지정 시작·완료/취소 Dispose; 실제 인스턴스에 override 적용 |
| CameraRig 추가 | `public void SetSensitivity(float value, bool invertY)` | 양의 유한 감도 검증; 입력 delta에 반영 |

설정 Service는 순수 C# 또는 장면 설정 담당 중 하나로 정해 중복 생성하지 않는다. 파일 저장 중 종료도 경험하려면 임시 파일에 쓴 뒤 교체하고 이전 정상 파일을 보존하는 절차를 추가한다.

## 질문·의사코드

재지정하려고 누른 Escape가 새 발사 키가 되면 되는가? `.inputactions` 원본을 실행 중 덮어써야 하는가?

```text
Load → 버전·범위 확인 → 같은 Reader.Actions에 override 적용 → 카메라·AudioMixer 갱신
재지정 시작 → Gameplay 중지 → 장치 입력 수집
완료/취소 → operation Dispose → 충돌 검사·값 저장 → 원래 흐름 상태 유지
```

## TODO 실습

```csharp
public void CancelRebind()
{
    // TODO: 진행 operation을 취소·Dispose하고 참조를 비운다.
    // 사망 상태를 임의로 Playing으로 바꾸지 않는다.
}
```

힌트: JSON은 데이터 저장 수단이며 유효값 보장은 별도 → SaveBindingOverridesAsJson/LoadBindingOverridesFromJson → 키 충돌·취소는 정책 필요.

## 따라하기·연결

1. Pause 메뉴에 감도·Y반전·Master/SFX 음량과 초기화 버튼을 추가한다. Canvas Scaler를 기준 해상도에 맞추고 키보드로 모든 항목에 접근되는지 본다.
2. 유효 범위와 기본값을 정한다. 0 음량은 mixer dB 변환에서 log(0)을 피하고 별도 음소거 값으로 처리한다.
3. 파일이 없으면 기본값, 잘못된 수치·깨진 JSON·지원하지 않는 버전이면 복구 정책을 적용한다. 외부 데이터를 그대로 Transform이나 Mixer에 넣지 않는다.
4. 먼저 Jump 같은 단일 Binding만 재지정한다. WASD Composite의 각 Part는 별도로 재지정하고 루트 Composite 인덱스를 키 하나로 처리하지 않는다.
5. 중복 키는 사용자에게 이유를 보여주고 취소·교체 중 정책을 정한다. 최소 버전은 같은 Gameplay 내 충돌을 거절한다.
6. 게임을 종료하고 Windows 빌드를 다시 켜 감도·음량·키가 유지되는지 확인한다. 에셋 복사본에만 override를 적용해 실제 Reader와 달라지는 문제를 검사한다.

## 완료 기준

- [ ] 감도·음량·반전이 실제 조작에 즉시 반영된다.
- [ ] 재실행·초기화·파일 누락이 일관되게 동작한다.
- [ ] 재지정 완료·취소·메뉴 닫힘에서 operation이 정리된다.
- [ ] 충돌 키·깨진 파일·잘못된 버전을 처리한다.
- [ ] 16:9 외 화면과 키보드 조작·실행 파일 증거를 남겼다.

## 선택과 한계

설정과 진행 저장을 나누면 실험 범위가 분명하다. 버전·복구·재지정 수명 관리가 추가된다. 소수 음량 값은 PlayerPrefs도 대안이지만 데이터 형태와 복구 방식을 설명할 수 있어야 한다.
