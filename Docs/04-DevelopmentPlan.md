# 개발 계획

<div align="center">

![header](https://capsule-render.vercel.app/api?color=D6C66B&type=rect&section=header&text=Development%20Plan&fontSize=36&fontColor=242218&desc=FILE%2004%20%C2%B7%20THE%20BACKROOMS&descSize=14&descAlignY=75&animation=fadeIn)

**구역은 모듈로, 이벤트는 문을 기준으로.**

<sub>
<a href="01-ProjectOverview.md">01 개요</a> &nbsp;·&nbsp;
<a href="02-ProjectConcept.md">02 컨셉</a> &nbsp;·&nbsp;
<a href="03-UserExperience.md">03 사용자 경험</a> &nbsp;·&nbsp;
<b>04 개발 계획</b> &nbsp;·&nbsp;
<a href="05-ProductionSchedule.md">05 제작 일정</a>
</sub>

</div>

<br>

## 개발 환경

<div align="center">

<img src="https://img.shields.io/badge/Unity-2022.3.21f1-222222?style=for-the-badge&logo=unity&logoColor=white" alt="Unity 2022.3.21f1">
<img src="https://img.shields.io/badge/URP-D6C66B?style=for-the-badge&labelColor=242218" alt="URP">
<img src="https://img.shields.io/badge/XR_Interaction_Toolkit-D6C66B?style=for-the-badge&labelColor=242218" alt="XR Interaction Toolkit">
<img src="https://img.shields.io/badge/Meta_Quest-0081FB?style=for-the-badge&logo=meta&logoColor=white" alt="Meta Quest">

</div>

<br>

## 현재 진행 상황

- [x] Meta Quest XR 환경 구성
- [x] Level 0 기본 공간 (벽 · 바닥 · 천장 · 형광등 프리팹, 형광등 소음)
- [x] 프로젝트 시작 시 `Backrooms` 씬 자동 열기

<br>

## 구현 기능

| 기능 | 담당 | 내용 |
|:---|:---:|:---|
| **구역 구조 · 동선** | 오예은 | 벽 · 바닥 · 천장 · 조명을 프리팹 모듈로 만들어 구역별로 조립 |
| **문 · 서랍 · 아이템** | 강진구 | XR Grab 기반으로 손잡이를 잡아 열고, 당기고, 줍는 상호작용 |
| **구역 전환** | 박승진 | 특정 문을 열면 다음 구역을 활성화하고, 지나온 구역은 정리 |
| **플레이어** | 박승진 | XR Origin 기반 이동 · 회전, 양손 컨트롤러 상호작용 |
| **분위기 연출** | 공통 | 형광등 소음과 깜빡임, 구역마다 다른 조명 · 사운드 |
| **몬스터** <sub>선택</sub> | 박승진 | NavMesh 기반 추적 AI. 진행 상황을 보고 추가 여부 결정 |

<br>

## 진행 단계

### 01 &nbsp; 공간 구성
<sub>ASSETS & ENVIRONMENT</sub>

- [ ] 담당 영역별 에셋 수집 및 프리팹 · 머티리얼 정리
- [ ] 이벤트 없는 구조물 · 오브젝트 배치
- [ ] 구역 간 동선 연결

### 02 &nbsp; 이벤트 개발
<sub>INTERACTION & CONTROL</sub>

- [ ] 배치 오브젝트 리스트 기준으로 스크립트 반영
- [ ] 문 · 서랍 · 아이템 상호작용
- [ ] 문 → 구역 전환 이벤트

### 03 &nbsp; VR 체험 완성
<sub>INTEGRATION & EXPERIENCE</sub>

- [ ] 공통 씬 통합
- [ ] Meta Quest 기기 실행 및 체험 검증
- [ ] 몬스터 추가 여부 결정

<details>
<summary><b>에셋 · 네이밍 규칙</b></summary>

<br>

- 에셋은 `Assets/` 아래 `Prefabs` · `Materials` · `Textures` · `Audio` · `Scripts` 규격에 맞춰 추가합니다.
- 이름은 **PascalCase**로 통일합니다. 자세한 규칙은 [메인 README](../README.md#네이밍-규칙)를 따릅니다.

</details>

<br>
<hr>

<div align="center">
    <sub>THE BACKROOMS · FILE 04 / 05</sub>
    <br><br>
    <a href="03-UserExperience.md">← 이전: 핵심 사용자 경험</a> &nbsp;·&nbsp; <a href="05-ProductionSchedule.md">다음: 제작 일정 →</a>
</div>
