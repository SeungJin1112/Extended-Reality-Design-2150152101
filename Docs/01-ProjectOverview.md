# 프로젝트 개요

<div align="center">

![header](https://capsule-render.vercel.app/api?color=D6C66B&type=rect&section=header&text=Project%20Overview&fontSize=36&fontColor=242218&desc=FILE%2001%20%C2%B7%20THE%20BACKROOMS&descSize=14&descAlignY=75&animation=fadeIn)

**세 명이 한 공간을 나눠 맡습니다.**

<sub>
<b>01 개요</b> &nbsp;·&nbsp;
<a href="02-ProjectConcept.md">02 컨셉</a> &nbsp;·&nbsp;
<a href="03-UserExperience.md">03 사용자 경험</a> &nbsp;·&nbsp;
<a href="04-DevelopmentPlan.md">04 개발 계획</a> &nbsp;·&nbsp;
<a href="05-ProductionSchedule.md">05 제작 일정</a>
</sub>

</div>

<br>

## 한눈에 보기

| 항목 | 내용 |
|:---|:---|
| **프로젝트명** | The Backrooms — 경계 너머에 혼자 남겨진 연구원의 기록 |
| **장르** | VR 탐험 · 리미널 호러 |
| **플랫폼** | Meta Quest (XR Origin 기반) |
| **엔진** | Unity 2022.3.21f1 · URP · XR Interaction Toolkit |
| **발표 자료** | [`TheBackroomsProposal.pptx`](TheBackroomsProposal.pptx) |

<br>

## 팀원 및 역할 분담

<div align="center">

![Objects](https://img.shields.io/badge/01-사물%20·%20아이템-D6C66B?style=for-the-badge&labelColor=242218)
![Space](https://img.shields.io/badge/02-구조%20·%20공간-D6C66B?style=for-the-badge&labelColor=242218)
![Player](https://img.shields.io/badge/03-플레이어%20·%20몬스터%20·%20액션-D6C66B?style=for-the-badge&labelColor=242218)

</div>

| | 이름 | 담당 영역 | 하는 일 |
|:---:|:---:|:---|:---|
| **01** | **강진구** | 사물 · 아이템 | 문, 서랍, 아이템 에셋 수집 / 열기 · 당기기 · 줍기 상호작용 / 배치된 사물에 스크립트 반영 |
| **02** | **오예은** | 구조 · 공간 | 구역별 구조물 · 공간 에셋 수집 / 벽 · 바닥 · 소품 오브젝트 배치 / 구역 간 동선 연결 |
| **03** | **박승진** | 플레이어 · 몬스터 · 액션 | XR Origin 기반 플레이어 구성 / 문 → 구역 전환 이벤트 제어 / 몬스터 AI (추가 시) |

> [!NOTE]
> 작업 범위는 나뉘어 있지만 흐름은 처음부터 끝까지 이어져 있습니다.
> **공간(예은) → 사물(진구) → 이벤트 · 플레이어(승진)** 순서로 결과물을 넘겨받는 구조라서, 앞 단계 일정이 밀리면 뒤 단계도 같이 밀립니다.

<details>
<summary><b>작업 넘겨주기 방식</b></summary>

<br>

1. 각자 모은 에셋은 `Assets/` 아래 규격(`Prefabs` · `Materials` · `Textures` …)에 맞춰 추가합니다.
2. 배치가 끝나면 사용한 오브젝트를 **전부 리스트업**해서 팀 톡방에 공유합니다.
3. 공유된 리스트를 기준으로 스크립트를 반영합니다.

</details>

<br>
<hr>

<div align="center">
    <sub>THE BACKROOMS · FILE 01 / 05</sub>
    <br><br>
    <a href="../README.md">메인 README</a> &nbsp;·&nbsp; <a href="02-ProjectConcept.md">다음: 프로젝트 컨셉 →</a>
</div>
