# The Backrooms

<div align="center">

![header](https://capsule-render.vercel.app/api?color=D6C66B&type=rect&section=header&text=The%20Backrooms&fontSize=40&fontColor=242218&animation=fadeIn)

**익숙하지만 낯선 공간, 끝없이 이어지는 백룸을 VR로 경험하다.**

![Unity](https://img.shields.io/badge/Unity-6000-222222?style=flat-square&logo=unity&logoColor=white)
![CSharp](https://img.shields.io/badge/Language-C%23-512BD4?style=flat-square)
![MetaQuest](https://img.shields.io/badge/Target-Meta%20Quest-0081FB?style=flat-square&logo=meta&logoColor=white)
![XR](https://img.shields.io/badge/XR-Interaction%20Toolkit-D6C66B?style=flat-square)

</div>

## 프로젝트 소개

Unity 기반으로 백룸 공간과 상호작용을 구현하는 XR 프로젝트입니다.  
공간 구성과 오브젝트 이벤트를 단계적으로 개발하여, **Meta Quest에서 직접 이동하고 사물과 상호작용하는 백룸 체험**을 완성하는 것을 목표로 합니다.

<br>

<div align="center">

[개발 환경](#개발-환경) &nbsp; · &nbsp;
[개발 로드맵](#개발-로드맵) &nbsp; · &nbsp;
[역할 분담](#역할-분담) &nbsp; · &nbsp;
[네이밍 규칙](#네이밍-규칙)

</div>

<br>

## 개발 환경

<div align="center">

<img src="https://img.shields.io/badge/Unity-6000-222222?style=for-the-badge&logo=unity&logoColor=white" alt="Unity 6000">
<img src="https://img.shields.io/badge/C%23-512BD4?style=for-the-badge" alt="C#">
<img src="https://img.shields.io/badge/Meta_Quest-0081FB?style=for-the-badge&logo=meta&logoColor=white" alt="Meta Quest">

</div>

<br>

| 구분 | 구성 |
|:---|:---|
| **Engine** | Unity Editor **6000** |
| **Language** | C# |
| **XR Interaction** | XR Interaction Toolkit |
| **Player Origin** | XR Origin |
| **Target Device** | Meta Quest |

<br>

## 개발 로드맵

> [!IMPORTANT]
> **12월 1일 이전 — 에셋 수집 및 기본 공간 구성**
>
> 사용할 에셋을 수집하고, 이벤트가 없는 구조물을 **Scene View에 배치**하는 단계까지 완료합니다.

### 01 &nbsp; 공간 구성
<sub>ASSETS & ENVIRONMENT</sub>

백룸의 분위기와 동선을 구성하는 기본 공간을 제작합니다.

- [ ] 프로젝트에 사용할 에셋 수집 및 정리
- [ ] 이벤트 없는 오브젝트와 구조물 배치
- [ ] 백룸의 기본 동선 및 공간 연결 구성
- [ ] 개발자별 작업 결과 통합 및 씬 확인

<br>

### 02 &nbsp; 이벤트 개발
<sub>INTERACTION & CONTROL</sub>

각 담당 영역에서 사물의 동작과 이벤트 흐름을 구현합니다.

- [ ] 사물 및 아이템 상호작용 구현
- [ ] 오브젝트 이벤트 제어 구현
- [ ] 오브젝트 생성 로직 구현
- [ ] 공간과 개별 이벤트 연결

<br>

### 03 &nbsp; VR 체험 완성
<sub>INTEGRATION & EXPERIENCE</sub>

공간과 기능을 통합하고 실제 기기에서 체험을 검증합니다.

- [ ] XR Origin 기반 플레이어 구성
- [ ] 공통 씬에 기능 통합
- [ ] Meta Quest 기기에서 실행
- [ ] 이동 및 상호작용 체험 검증

> [!TIP]
> **최종 목표**
>
> Meta Quest 내부에서 직접 이동하고 사물과 상호작용할 수 있는 백룸 체험을 완성합니다.

<br>

## 역할 분담

| 담당 | 개발 영역 | 작업 범위 |
|:---:|:---|:---|
| **01** | **사물 · 아이템 이벤트** | 사물과 아이템의 상호작용 및 개별 이벤트 구현 |
| **02** | **공간 · 구조물 구성** | 이벤트 없는 오브젝트 배치 및 에셋 기반 구조물 세부 구성 |
| **03** | **이벤트 제어 · 생성** | 오브젝트 이벤트 흐름 제어 및 생성 로직 구현 |

<br>

<details>
<summary><b>공통 개발 흐름</b></summary>

<br>

1. **에셋 수집** — 사용할 리소스를 수집하고 정리합니다.
2. **기본 공간 구성** — 이벤트 없는 구조물과 오브젝트를 배치합니다.
3. **담당 기능 구현** — 역할별 상호작용 및 제어 기능을 개발합니다.
4. **씬 통합** — 공통 씬에서 공간과 이벤트의 연결을 확인합니다.
5. **기기 검증** — Meta Quest에서 실행하고 체험 동작을 확인합니다.

</details>

<br>

## 네이밍 규칙

![PascalCase](https://img.shields.io/badge/Convention-PascalCase-D6C66B?style=flat-square&labelColor=242218)

프로젝트 내 이름은 **PascalCase**로 통일합니다.  
각 단어의 첫 글자를 대문자로 작성하며, 공백이나 밑줄로 단어를 구분하지 않습니다.

| 영역 | 적용 대상 | 예시 |
|:---|:---|:---|
| **Hierarchy View** | 게임 오브젝트명 | `PlayerOrigin` · `ExitDoor` |
| **Project View** | 에셋 파일명 | `WallMaterial` · `FloorTexture` |
| **Project View** | 스크립트 파일명 | `DoorController.cs` |
| **Script** | 클래스명 | `DoorController` |
| **Project View** | 폴더명 | `Scripts` · `Materials` · `Prefabs` |

> [!NOTE]
> 스크립트 파일명과 클래스명은 동일하게 작성합니다.
>
> **`DoorController.cs` → `public class DoorController`**

<details>
<summary><b>네이밍 작성 예시</b></summary>

<br>

| 권장 | 변경 필요 |
|:---|:---|
| `ExitDoor` | `exitDoor` · `exit_door` · `Exit Door` |
| `WallMaterial` | `wallMaterial` · `wall_material` |
| `DoorController.cs` | `doorController.cs` · `door_controller.cs` |
| `Prefabs` | `prefabs` |

</details>

<br>
<hr>

<div align="center">
    <sub>THE BACKROOMS · UNITY 6000 · META QUEST</sub>
    <br><br>
    <a href="#the-backrooms">맨 위로 ↑</a>
</div>
