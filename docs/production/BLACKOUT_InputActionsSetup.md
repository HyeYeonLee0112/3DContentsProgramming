# BLACKOUT — Input Actions 설정 따라 하기

이 문서는 키보드와 마우스의 조작을 BLACKOUT 게임의 행동으로 연결하는 Unity 설정 가이드다. 코드부터 작성하지 않는다. 먼저 `WASD = 이동`, 마우스 이동 = 시선, 클릭 = 발사처럼 입력표를 정확히 만든 뒤, 다음 단계에서 `PlayerInputReader`가 이 입력표를 읽는다.

> 대상: Unity 6000.3.22f1 · Input System 1.20.0  
> 연결 문서: [TPS 플레이어 이동·조준·펄스 발사 구현 가이드](BLACKOUT_TPSPlayerMovementAndPulseFire.md)  
> 현재 확인: `Assets/BLACKOUT/Settings/New Actions.inputactions`가 이미 생성되어 있다. 이 파일을 새로 만들지 말고 이름을 바꿔 이어서 설정한다.

## 1. 먼저 이해할 세 단어

| 이름 | 쉬운 뜻 | BLACKOUT 예시 |
|---|---|---|
| Action Map | 같은 상황에서 사용할 입력 묶음 | `Gameplay`: 플레이 중 조작 |
| Action | 플레이어의 의도 | `Move`, `Fire`, `Interact` |
| Binding | Action에 연결한 실제 키·버튼 | `Move`에 연결한 W, A, S, D |

`Move`는 키 이름이 아니다. “이동하려는 의도”다. 그래서 키보드 W와 게임패드 왼쪽 스틱을 모두 `Move`에 연결할 수 있다. 코드가 `W`를 직접 읽지 않고 `Move`를 읽으면 키 재지정과 게임패드 지원을 나중에 추가하기 쉬워진다.

```text
W 키
게임패드 왼쪽 스틱
        ↓
     Move Action
        ↓
 PlayerInputReader
        ↓
    PlayerMotor
```

## 2. 이번에 만들 최종 입력표

지금은 아래 여섯 Action만 만든다. 달리기, 메뉴, 충전 사격은 기본 조작이 확인된 뒤 추가한다.

| Action Map | Action | Action Type | Control Type | 키보드·마우스 | 게임패드(나중) |
|---|---|---|---|---|---|
| Gameplay | Move | Value | Vector2 | WASD, 방향키 | Left Stick |
| Gameplay | Look | Value | Vector2 | Mouse Delta | Right Stick |
| Gameplay | Jump | Button | Button | Space | South Button |
| Gameplay | Aim | Button | Button | Mouse Right Button | Left Trigger |
| Gameplay | Fire | Button | Button | Mouse Left Button | Right Trigger |
| Gameplay | Interact | Button | Button | E | West Button |

`Value`는 방향·크기처럼 계속 바뀌는 값을 받고, `Button`은 눌렀는지 여부를 받는다. `Vector2`는 X와 Y, 두 숫자를 묶은 방향값이다.

```text
W: ( 0,  1)     S: ( 0, -1)
A: (-1,  0)     D: ( 1,  0)
```

## 3. 시작 전 확인

### 3.1 기존 파일을 사용한다

Project 창에서 다음 파일을 찾는다.

```text
Assets/BLACKOUT/Settings/New Actions.inputactions
```

파일을 선택하고 `F2` 또는 마우스 오른쪽 버튼의 Rename으로 아래 이름으로 바꾼다.

```text
BlackoutInputActions.inputactions
```

Unity Project 창 안에서 이름을 바꾼다. Windows 파일 탐색기에서 `.meta` 파일과 분리해 이름을 바꾸지 않는다. Unity가 참조를 관리하기 때문이다.

### 3.2 창을 연다

이 파일을 더블클릭하면 **Input Actions Editor**가 열린다. 창 제목이 `BlackoutInputActions`로 표시되면 저장된 파일을 편집 중인 것이다.

화면은 세 영역으로 나뉜다.

| 위치 | 이름 | 하는 일 |
|---|---|---|
| 왼쪽 | Action Maps | `Gameplay`처럼 입력 묶음을 만든다 |
| 가운데 | Actions / Bindings | 행동과 실제 키 연결을 만든다 |
| 오른쪽 | Properties | 선택한 Action 또는 Binding의 세부 설정을 바꾼다 |

화면 위의 `No Control Schemes`, `All Devices`는 지금 바꾸지 않는다. 키보드·마우스 기본 조작이 정상 동작한 뒤 게임패드를 연결할 때 사용한다.

## 4. Gameplay Map 확인

왼쪽에 `Gameplay`가 이미 있으면 그대로 사용한다. 없다면 왼쪽 `Action Maps` 제목 옆의 `+`를 눌러 `Add Action Map`을 선택하고 이름을 `Gameplay`로 정한다.

`Gameplay`은 플레이어가 움직이고 싸울 때만 켜는 묶음이다. 나중에 메뉴를 열면 이 묶음을 끄고 `UI` 묶음을 켜서, 메뉴를 누른 클릭이 총 발사로 이어지지 않게 한다.

## 5. Move 설정: WASD로 이동 의도 만들기

### 5.1 Action 속성

가운데에서 `Move`를 클릭한다. 없다면 `Actions` 제목 오른쪽 `+`로 새 Action을 만들고 이름을 `Move`로 정한다.

오른쪽 `Action Properties`를 다음처럼 설정한다.

| 설정 | 값 | 이유 |
|---|---|---|
| Action Type | `Value` | 이동은 한 번의 클릭이 아니라 계속 바뀌는 방향값이다 |
| Control Type | `Vector2` | 왼쪽·오른쪽과 앞·뒤를 한 번에 전달한다 |
| Interactions | 비움 | 이동은 누르는 동안 계속 읽으면 된다 |
| Processors | 비움 | 입력 보정은 기본 조작을 확인한 뒤 추가한다 |

### 5.2 WASD 묶음 추가

`Move` 줄 오른쪽의 `+`를 누르거나 `Move`를 마우스 오른쪽 버튼으로 클릭한다. 메뉴에서 다음을 선택한다.

```text
Add 2D Vector Composite
```

다음 구조가 생긴다.

```text
Move
└─ 2D Vector
   ├─ Up
   ├─ Down
   ├─ Left
   └─ Right
```

`2D Vector`를 클릭한 뒤 오른쪽 Binding Properties의 `Mode`를 다음처럼 설정한다.

```text
Digital Normalized
```

W와 D를 동시에 눌렀을 때 이동값이 지나치게 커지지 않도록 돕는 설정이다. 실제 이동 코드에서도 값의 길이를 한 번 더 최대 1로 제한한다. 한쪽 설정만 믿지 않는 이유는 다른 입력 장치와 이후 기능을 추가할 때도 안전하게 처리하기 위해서다.

### 5.3 각 방향에 키 연결

`Up`을 클릭하면 오른쪽이 Action Properties에서 **Binding Properties**로 바뀐다. `Path` 옆 `Listen`을 누르고 원하는 키를 누른다.

| Composite 항목 | Listen 중 누를 키 | 예상 Path |
|---|---|---|
| Up | W | `<Keyboard>/w` |
| Down | S | `<Keyboard>/s` |
| Left | A | `<Keyboard>/a` |
| Right | D | `<Keyboard>/d` |

완료 뒤에는 가운데 목록이 아래처럼 보여야 한다.

```text
Move
└─ 2D Vector
   ├─ Up: W
   ├─ Down: S
   ├─ Left: A
   └─ Right: D
```

### 5.4 방향키와 게임패드 추가

방향키도 지원하려면 `Move`에 `Add 2D Vector Composite`를 한 번 더 추가한다. 새 묶음의 Up/Down/Left/Right에는 각각 방향키를 Listen으로 연결한다.

게임패드는 Composite가 필요 없다. `Move`의 `+`에서 `Add Binding`을 선택하고 Listen 중 게임패드 왼쪽 스틱을 움직인다.

```text
<Gamepad>/leftStick
```

왼쪽 스틱은 이미 X·Y 방향을 가진 Vector2 입력이기 때문이다.

## 6. Look 설정: 마우스로 카메라 돌리기

`Actions` 제목 옆 `+`로 새 Action을 만들고 이름을 `Look`으로 정한다.

| 설정 | 값 |
|---|---|
| Action Type | `Value` |
| Control Type | `Vector2` |
| Interactions | 비움 |
| Processors | 비움 |

`Look`의 `+`에서 `Add Binding`을 선택한다. 새 Binding을 클릭하고 `Path > Listen`을 누른 뒤 마우스를 움직인다.

```text
<Mouse>/delta
```

`delta`는 마우스가 화면에서 있는 위치가 아니라, 이번 순간에 얼마나 움직였는지를 뜻한다. TPS 카메라는 이 움직인 양을 받아 가로 회전과 세로 회전을 계산한다.

게임패드 지원 시에는 `Look`에 Binding을 하나 더 추가하고 오른쪽 스틱을 움직인다.

```text
<Gamepad>/rightStick
```

마우스 감도와 게임패드 감도는 서로 다르게 느껴진다. 처음부터 Processor에 수치를 넣기보다, 다음 코드 단계에서 각각의 감도 설정을 분리해 실제 플레이로 조정한다.

## 7. Button Action 네 개 만들기

아래 네 Action은 모두 같은 방식으로 만든다.

1. `Actions` 제목의 `+`를 눌러 Action을 추가한다.
2. 이름을 입력한다.
3. 오른쪽에서 `Action Type = Button`, `Control Type = Button`으로 설정한다.
4. 해당 Action의 `+`에서 `Add Binding`을 고른다.
5. Binding을 선택하고 `Path > Listen`을 누른 뒤 원하는 키를 누른다.

| Action | 키보드·마우스 Binding | 게임패드 Binding | BLACKOUT에서 할 일 |
|---|---|---|---|
| Jump | Space | South Button | 점프 시작 요청 |
| Aim | Mouse Right Button | Left Trigger | 누르는 동안 조준 |
| Fire | Mouse Left Button | Right Trigger | 기본 펄스 발사 요청 |
| Interact | E | West Button | 이후 전력 흡수·주입 |

### Interaction은 왜 비워 두는가

`Interaction`은 입력 시간 규칙이다. 예를 들어 Tap은 짧게 눌렀다 뗀 행동, Hold는 일정 시간 이상 누른 행동이다.

| Action | 지금 선택 | 이유 |
|---|---|---|
| Jump | Interaction 없음 | 눌렀을 때 한 번 요청하면 충분하다 |
| Aim | Interaction 없음 | 누르고 있는 동안의 상태를 읽는다 |
| Fire | Interaction 없음 | W02에서는 클릭 한 번에 기본 펄스 한 발이다 |
| Interact | Interaction 없음 | 전력 시스템 구현 때 실제 필요한 시간 규칙을 결정한다 |

충전 사격은 기본 발사가 검증된 뒤 `Fire`의 시작·유지·해제 시점을 코드에서 읽어 구현한다. 지금 `Hold`를 억지로 설정하면 기본 발사 규칙과 겹칠 수 있다.

## 8. 현재 단계에서 완성된 모습

```text
Gameplay
├─ Move                  Value / Vector2
│  ├─ 2D Vector
│  │  ├─ Up: W
│  │  ├─ Down: S
│  │  ├─ Left: A
│  │  └─ Right: D
│  ├─ 2D Vector          ← 선택: 방향키
│  └─ Gamepad Left Stick ← 선택: 게임패드
├─ Look                  Value / Vector2
│  ├─ Mouse Delta
│  └─ Gamepad Right Stick ← 선택: 게임패드
├─ Jump                  Button / Space
├─ Aim                   Button / Mouse Right Button
├─ Fire                  Button / Mouse Left Button
└─ Interact              Button / E
```

## 9. Control Scheme은 나중에 설정한다

Control Scheme은 Action Map이 아니다. 어떤 장치 조합으로 게임을 조작하는지 묶는 설정이다.

기본 조작이 확인된 뒤 다음 두 개를 추가한다.

| Scheme 이름 | Required Devices |
|---|---|
| `Keyboard&Mouse` | Keyboard, Mouse |
| `Gamepad` | Gamepad |

이를 추가하면 화면 안내에서 “Press E”와 “Press X”를 장치에 맞게 구분하거나, 키 재지정 화면을 정리하기 좋아진다. 지금 만들지 않아도 키보드 입력은 정상적으로 설정할 수 있다.

## 10. C# 클래스 생성 설정

Input Actions Editor를 닫거나 저장한 뒤, Project 창에서 `BlackoutInputActions.inputactions`를 선택한다. Inspector에서 다음 항목을 찾는다.

```text
Generate C# Class
```

체크하고 클래스 이름을 아래처럼 정한 뒤 `Apply`를 누른다.

```text
BlackoutInputActions
```

Unity가 입력표를 C#에서 읽을 수 있는 자동 생성 클래스로 바꿔 준다. 생성된 `.cs` 파일은 직접 수정하지 않는다. 다음 단계의 `PlayerInputReader`가 이 클래스를 만들고 `Gameplay.Move`, `Gameplay.Fire` 같은 행동을 읽는다.

```text
BlackoutInputActions.inputactions
             ↓ Unity 자동 생성
BlackoutInputActions.cs
             ↓ 다음 구현 단계에서 사용
PlayerInputReader.cs
```

## 11. Project Settings 확인

Input System 패키지가 설치되어 있어도 Player 설정이 구형 입력만 사용하면 새 입력이 동작하지 않을 수 있다.

1. `Edit > Project Settings > Player`를 연다.
2. `Other Settings`를 연다.
3. `Active Input Handling`을 확인한다.
4. 다음 중 하나로 설정한다.

```text
Input System Package (New)
또는
Both
```

설정 변경 뒤 Unity 재시작을 요구하면 저장 후 재시작한다. 기존 구형 입력 코드를 함께 쓰는 기간에는 `Both`, 새 Input System만 사용할 것이 확실하면 `Input System Package (New)`를 선택한다.

## 12. 아직 실행해도 캐릭터가 움직이지 않는 이유

이 파일은 입력을 정리한 표이지 이동 코드가 아니다. 지금 설정을 마쳐도 `Player.cs`가 아직 `Move`를 읽지 않으므로 캐릭터는 움직이지 않는다. 이는 정상이다.

다음 구현 흐름은 아래와 같다.

```text
Move Action의 Vector2 값
        ↓
PlayerInputReader가 값 저장
        ↓
PlayerController가 이번 프레임 입력 묶음 생성
        ↓
PlayerMotor가 카메라 기준 월드 이동으로 변환
        ↓
CharacterController가 실제 이동
```

## 13. 설정 확인 목록

- [ ] 파일 이름이 `BlackoutInputActions.inputactions`이고 `Assets/BLACKOUT/Settings`에 있다.
- [ ] `Gameplay` Map이 있다.
- [ ] Move는 `Value / Vector2`이며 WASD 2D Vector Composite가 있다.
- [ ] Look은 `Value / Vector2`이며 Mouse Delta가 연결됐다.
- [ ] Jump, Aim, Fire, Interact는 `Button / Button`이다.
- [ ] 각 Button Action에 Space, 우클릭, 좌클릭, E가 각각 연결됐다.
- [ ] `Generate C# Class`가 켜져 있고 클래스 이름이 `BlackoutInputActions`다.
- [ ] `Active Input Handling`이 New 또는 Both다.
- [ ] Save Asset을 눌렀고 Input Actions Editor에 저장되지 않은 변경 표시가 없다.

## 14. 자주 생기는 문제

| 문제 | 원인 | 먼저 할 일 |
|---|---|---|
| Move에 W를 넣었는데 Vector2를 읽을 수 없다 | Action Type 또는 Control Type이 잘못됨 | Move를 `Value / Vector2`로 바꾼다 |
| W와 D를 함께 누르면 유난히 빠르다 | Composite Mode와 코드의 길이 제한이 없음 | `Digital Normalized`와 코드의 `ClampMagnitude`를 모두 확인한다 |
| Path에 키가 안 들어간다 | Binding이 아니라 Action을 선택했거나 Listen을 누르지 않음 | 가운데에서 Up/Down 같은 Binding 행을 선택한다 |
| 마우스가 Look에 연결되지 않는다 | 마우스 버튼을 눌렀거나 움직임이 너무 작음 | Binding의 Listen을 다시 누르고 마우스를 크게 움직인다 |
| 설정했는데 실행 중 입력이 없다 | Active Input Handling 또는 다음 코드 연결이 없음 | 11장 설정과 PlayerInputReader 구현 상태를 확인한다 |
| 생성된 C# 파일이 안 보인다 | Generate C# Class 적용 전 | Input Actions Asset Inspector에서 체크 후 Apply를 누른다 |
| 같은 Action이 두 번 실행된다 | 이후 코드에서 입력 콜백을 중복 구독함 | `OnEnable` 구독과 `OnDisable` 해제를 대칭으로 확인한다 |

입력 설정을 끝냈다면 다음 문서의 [입력 Reader 구조](BLACKOUT_TPSPlayerMovementAndPulseFire.md#72-입력-reader의-최소-형태)로 넘어가 `PlayerInputReader`를 구현한다. 이때부터 설정 파일의 입력값이 실제 TPS 이동과 펄스 발사로 연결된다.
