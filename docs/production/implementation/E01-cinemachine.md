# E01 — 직접 만든 카메라와 Cinemachine 비교하기

카메라 충돌과 조준 전환을 직접 고치다 보면 반복 작업이 늘어난다. 같은 요구를 Cinemachine으로 구성하고 무엇이 줄거나 늘어나는지 비교한다.

선행: [T06](06-aim.md) · 연결: K08 · [전체 목록](README.md)

## 개념·책임·계약

Cinemachine은 카메라 동작을 구성하는 Unity 패키지다. [Third Person Follow](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html)는 추적 대상에 대한 어깨 위치·거리·충돌을 설정한다. 추적 대상의 회전 입력은 별도로 연결해야 한다.

| 대상 | 실험 계약 |
|---|---|
| 실험 장면 | PlayerLab 복사, 원본 수동 카메라 상태 보존 |
| 실제 Camera | Brain이 최종 위치를 제어; 수동 Rig의 위치 변경 중지 |
| CinemachineCamera | 추적 대상·거리·충돌 레이어 설정; 설치 버전의 UI 확인 |
| 입력 연결 | 기존 ApplyLook의 yaw/pitch로 독립 추적 Transform 회전; 입력 Reader 재소비 금지 |

새 패키지는 도입 시 Unity 6.3 호환·무료 여부·버전을 확인한다. 현재 manifest에 설치됐다고 가정하지 않는다. 구버전의 VirtualCamera API를 3.x 코드에 섞지 않는다.

## 먼저 생각하기·의사코드

추적 대상을 몸에 붙이면 몸이 도는 만큼 카메라도 돌아야 하는가?

```text
동일 검증 경로 확보 → 패키지 도입 → 추적 대상 회전 연결
수동 카메라 비활성 → Brain만 최종 위치 결정 → 조준 Ray 연결 → 같은 경로 비교
```

## 따라하기

1. 수동 카메라의 벽 모서리·상하 끝각·급회전·조준 동영상을 남긴다.
2. 실험 장면에서 Package Manager로 호환 버전을 선택하고 설치 후 Console 오류를 확인한다.
3. MainCam에 Brain을 설정하고 CinemachineCamera와 Third Person Follow를 만든다. 별도 회전 가능한 CameraTarget을 연결한다.
4. 기존 감도·pitch 제한을 추적 대상 회전으로 전달한다. Body 회전과 CameraTarget 회전을 분리한다.
5. 거리·Shoulder Offset·Damping·카메라 반경·충돌 마스크를 같은 환경에서 조절한다. Player를 자기 장애물로 검사하지 않는다.
6. 실제 Camera의 중심 Ray를 Gun에 전달한다. 두 카메라가 같은 Transform을 동시에 제어하거나 입력을 두 번 읽지 않는지 본다.
7. 구현 시간·조절 편의·좁은 통로 품질·프레임 비용을 비교하고 채택/보류 이유를 적는다.

## 검증·완료 기준

- [ ] 벽·모서리·상하·급회전을 같은 경로로 비교했다.
- [ ] 카메라 위치 변경자와 입력 소비자가 각각 하나다.
- [ ] 조준점과 총구 장애물 규칙이 유지된다.
- [ ] 품질·비용·유지보수 관점의 채택 판단이 있다.

## 이득과 비용

패키지는 카메라 구성·충돌의 반복 작업을 줄일 수 있다. 패키지 버전·업데이트 순서·설정 조합을 배워야 한다. 수동 카메라 유지도 검증 결과가 뒷받침하면 유효한 결론이다.
