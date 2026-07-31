# Spine → Unity 2D Animation 변환 가이드 (Soldier_Axe 검증 완료)

> 작성: 2026-07-30 / 검증 대상: Unity 6000.3.7f1, 2D Animation 13.0.4, Spine 4.2 익스포트
> 배경: Spine 애니메이션 작업 인원 부재로, 신규 몬스터를 Unity 자체 시스템(2D Animation 패키지)으로
> 제작/변환할 수 있는지 Soldier_Axe(S1_M01)로 전 과정을 검증함. **결과: 성공 — 원본과 거의 동일한 수준 재현.**

---

## 1. 최종 결과 요약

| 항목 | 결과 |
|---|---|
| 셋업 포즈 | 원본과 일치 (크기 오차: 높이 5.26 vs 5.27 유닛) |
| Idle (허리 바운스 + 무릎 굽힘 + 발바닥 고정) | 재현 완료 — 패스 컨스트레인트 + 발목 이펙터 IK 베이크 |
| Attack0 / Buff0 / Defence0 | 재현 완료 (종료 후 Idle 자동 복귀 포함) |
| FFD(버텍스 변형) | 본 1개짜리 메시는 유사변환 피팅으로 근사, 다중 본 메시(Body/Head)는 미반영 |
| 남은 시각 차이 | 그림자 크기 축소(2.28배), EyeLight의 screen/additive 블렌드 미적용, FFD 정점 단위 출렁임 손실 |

**산출물 위치: `Assets/TEST_UnityRig/`**

- `Editor/SpineToUnityRigConverter.cs` — 변환기 본체 (메뉴: `Tools > TEST UnityRig > Run All`)
- `TEST_SoldierAxe_UnityRig.prefab` — 본 35개 + 파츠별 SpriteSkin + Animator 프리팹
- `SoldierAxe_{Idle,Attack0,Buff0,Defence0}.anim` + `TEST_SoldierAxe_UnityRig.controller`
- `UnityRigCompare.unity` — 비교 씬 (왼쪽 Spine / 오른쪽 Unity, 플레이 후 숫자키 1~4로 동시 재생)
- `UnityRigCompareDriver.cs` — 비교 재생 드라이버
- `compare_play_idle.png`, `compare_play_attack.png` — 검증 스크린샷

---

## 2. 이 프로젝트 Spine 데이터의 구조 (몬스터 공통)

새 몬스터 리소스가 와도 대부분 아래 패턴을 따른다 (43종 JSON 전수 확인):

- **애니메이션 구성 표준**: 일반 몬스터 = `Idle`(루프) / `Attack0` / `Buff0` / `Defense0`(철자 `Defence0` 혼용 주의). 엘리트 = `Attack1` 추가.
- **스테이지별 리깅 복잡도**: Stage1 = FFD·IK·패스 컨스트레인트 사용(복잡). **Stage2·3 = FFD 0, IK 0의 순수 본 애니메이션(단순)** → 변환 난이도 낮음.
- **동일 스켈레톤 재활용 선례**: Watcher_A/B, SpearMan_A/B는 본 구조가 완전히 동일(이미지만 교체). 원작자도 템플릿 방식을 썼다.
- Soldier_Axe에서 확인된 리그 기법 (Stage1 계열에서 재등장 가능):
  - **다리**: 2본 IK × 2 (타깃 `LeftLeg`/`RightLeg`는 root 자식 = 지면 고정)
  - **허리 바운스**: 패스 컨스트레인트 — 골반(Body1)이 닫힌 베지어 타원 경로를 따라 이동, 애니메이션은 path `position` 값 하나만 키. Idle에서 0.20→1.20(한 바퀴), Buff0도 사용
  - **발**: `inherit: noRotationOrReflection` (다리가 회전해도 발은 수평 유지)
  - **눈 발광**: 슬롯 2개(screen/additive 블렌드)가 같은 어태치먼트 공유, alpha 타임라인으로 점멸
  - **그림자**: 본 스케일 2.28배 + multiply 블렌드 + 트랜스폼 컨스트레인트로 발/몸 추종(미변환, 시각 영향 경미)

---

## 3. 변환 파이프라인 (5 Phase)

`SpineToUnityRigConverter.cs`의 상단 상수(`SrcDir`, `JsonPath`, `AtlasPath`, `PngPath`, `OutDir` 등)를
새 몬스터 경로로 바꾸면 재사용 가능. 실행은 `Tools > TEST UnityRig > Run All`.

| Phase | 작업 | 핵심 포인트 |
|---|---|---|
| 1. 아틀라스 언팩 | `.atlas.txt` 파싱 → 패킹된 리전을 원본 크기 셀로 복원한 파츠 시트 PNG 생성 | 신형 포맷(`bounds`/`offsets`/`rotate:90`) 지원. **PMA(premultiplied alpha) 해제 필수** (`pma:true` 텍스처를 그대로 쓰면 테두리가 어두워짐). rotate:90 리전은 페이지에서 가로세로 스왑 상태 |
| 2. 스킨 데이터 주입 | `ISpriteEditorDataProvider`로 스프라이트별 SpriteRect + SpriteBone + 메시 정점/삼각형 + BoneWeight 주입 | **피벗은 반드시 BottomLeft(0,0)** — Center 피벗이면 정점/본 좌표계가 어긋나 파츠가 뭉개짐. 정점 위치는 "셀 내 UV 평면 좌표", 본 바인드포즈는 최소제곱 유사변환(F)의 역으로 셀 공간에 배치. Spine 웨이트(본당 4개 초과는 상위 4개 정규화) |
| 3. 리그 조립 | Spine 본 계층 → GameObject 계층(`Bones/`), 슬롯 → `Parts/` 하위 SpriteRenderer+SpriteSkin | 좌표 변환: PPU=100, Spine px÷100 = Unity 유닛(스파인 기본 스케일 0.01과 일치). `inherit` 모드는 베이크 시 월드 포즈를 Unity 로컬로 역산해 흡수. SpriteSkin의 `m_RootBone`/`m_BoneTransforms`는 SerializedObject로 주입 |
| 4. 애니메이션 베이크 | 30fps로 전 본 샘플 → localPosition/EulerAnglesRaw.z/localScale 커브. 슬롯 alpha → SpriteRenderer m_Color.a 커브. AnimatorController 생성(비루프 스테이트는 exitTime으로 Idle 복귀) | 컨스트레인트는 전부 베이크 시점에 풀어서 순수 본 커브로 만든다(런타임 IK 불필요). 적용 순서: 패스 → IK. 회전 커브는 unwrap(±180 점프 방지) |
| 5. 비교 씬 | Spine 원본과 나란히 배치 + 동시 재생 드라이버 | 검증용 |

### ⭐ 최종 아키텍처: 원본 애니메이션은 스파인 런타임으로 포즈 계산 (v2)

수제 IK/패스 솔버는 근사라서 스파인과 다리 위치가 40px까지 어긋나고, 사거리 한계에서 발-종아리가
분리되는 문제가 있었다(사용자 지적). **최종 해법: 원본에 존재하는 애니메이션(Idle/Attack0/Buff0/Defence0)은
프로젝트에 내장된 spine-csharp 런타임으로 매 프레임 스켈레톤을 직접 구동해 본 월드를 읽는다**
(`SpineWorldAt()`: `new Spine.Skeleton(data)` → `SetToSetupPose` → `Animation.Apply` → `UpdateWorldTransform`).
IK·패스·트랜스폼 컨스트레인트·상속·적용 순서가 원본과 수학적으로 100% 동일 → 오차 원천 제거.
셋업 포즈(Phase2 바인드포즈/Phase3 GO 트랜스폼)와 커스텀 애니메이션의 기준점(truth)도 스파인 셋업으로 통일.
단위 변환: 스파인 월드 × (1/SkeletonDataAsset.scale) = px.

아래의 수제 솔버(컨스트레인트 베이크)는 **원본에 없는 커스텀 애니메이션(Death 등) 전용**으로만 쓰인다.

### 컨스트레인트 베이크 방식 (커스텀 애니메이션 전용 수제 솔버)

모두 **"셋업 포즈 = 정답" 델타 보정** 원칙을 따른다: 셋업 포즈에서 원본 데이터와 솔버 결과의 차이를
오프셋으로 저장해두고, 매 프레임 솔버 결과에 오프셋을 더한다. → 셋업/Idle 시작 시점이 원본과 정확히 일치.

1. **패스 컨스트레인트**: 닫힌 베지어 경로(정점 트리플 = cpIn, anchor, cpOut)를 조밀 샘플(세그먼트당 24점)해
   호길이 테이블 생성 → `position`(0~1, 닫힌 경로는 wrap) 위치의 점을 평가 → 대상 본 월드 위치로 적용 → 하위 본 재계산.
   IK보다 **먼저** 적용해야 골반 하강이 무릎 굽힘으로 이어진다.
2. **2본 IK — 반드시 "발목 이펙터" 방식으로**: 이펙터를 정강이 본 끝(length 팁)이 아니라
   **정강이의 첫 자식(발 본)의 원점 = 발목**으로 정의하고, 발목 목표 = 타깃 위치 + 셋업 오프셋.
   굽힘 방향(bend)은 JSON 값 대신 **셋업 기하의 외적 부호로 자동 판정**.
   → 셋업 재현 오차 ~0, 타깃이 고정인 Idle에서 발바닥이 지면에 완전 고정(검증: RightFoot (-20,40)px 전 구간 불변).
3. **FFD(deform) 근사**: 본 1개짜리 메시 한정, 프레임별 변형 정점군을 원본 정점군에 최소제곱 유사변환
   (복소수 회귀: 회전+균등스케일+이동)으로 피팅해 그 본의 월드에 합성. 자식 본 월드는 건드리지 않으므로
   Unity 로컬 역산에서 자동 보상됨(발이 다리 deform에 끌려가지 않음).
4. **inherit 모드**(noRotationOrReflection 등): 별도 대응 불필요 — 베이크가 월드 포즈를 계산한 뒤
   Unity 계층(항상 normal 상속) 기준 로컬값으로 역산하므로 자동 흡수.

---

## 4. 시행착오 기록 (같은 함정에 빠지지 않기)

| 증상 | 원인 | 해결 |
|---|---|---|
| 파츠들이 원점 부근에 뭉개져 렌더 | 스프라이트 피벗 Center와 정점/본 좌표계(rect 좌하단 기준) 불일치 | 피벗 BottomLeft(0,0)로 슬라이스 |
| 에디트 모드에서 리그가 뭉개져 보임 (플레이는 정상) | SpriteSkin이 에디트 모드에서 즉시 변형 갱신을 안 함 (스크린샷 도구가 변형 전 캡처) | 판정은 **플레이 모드**에서. 에디트 모드에선 SpriteSkin enable 토글 후 다음 프레임에 확인 |
| 뒷다리가 꼬임 (셋업부터) | IK 굽힘 방향 부호 — spine `bendPositive=true`가 내 솔버 규약과 반대 | 셋업 기하(외적)로 방향 자동 판정하도록 변경 |
| Idle에서 다리만 정지 | Idle/Buff0의 다리 움직임은 본 트랙이 아니라 ① 패스 컨스트레인트(골반) ② FFD(다리 메시)로만 구동 | 패스 컨스트레인트 베이크 + FFD 유사변환 근사 추가 |
| 무릎 굽힘 시 발이 같이 떠오름 | "본 길이 팁" 이펙터 + 상수 각도 보정 방식 — 각도 보정이 정강이 방향 변화 시 발목을 호로 이동시킴 | 발목(발 본 원점) 이펙터 방식으로 재정식화 |
| 애니메이션 동결 비교 시 Spine 쪽 포즈가 다르게 나옴 | `SetAnimation`의 기본 믹스(이전 포즈와 블렌딩) 때문 | 비교 시 `ClearTracks + SetToSetupPose + MixDuration=0` |
| Phase4만 단독 재실행 후 리그 깨짐 | 클립/컨트롤러/프리팹 재생성 과정에서 씬·에셋 상태 불일치 (컨트롤러 참조 끊김 포함) | **항상 Phase2→3→4를 함께 실행 (Run All)** |
| 재빌드가 조용히 안 됨 | 에디터가 플레이 모드 상태면 변환기가 abort | 플레이 모드 종료 후 실행 |
| 뒷발이 골반 이동 극단에서 최대 9px 떠오름 (Idle 포함 모든 애니메이션) | IK 사거리 클램프 — 골반이 궤도 극단에 가면 hip→발목 거리가 다리 길이를 넘어 정강이 끝이 발목 목표에 못 미치고, 발이 무릎 쪽으로 끌려 올라감 | **발바닥 핀 고정**: 솔브 후 (목표 − 정강이 끝) 오차만큼 발과 하위 본을 통째로 평행이동해 발목을 목표에 정확히 고정 (변환기 IK 블록에 내장, 발 로컬 애니메이션 보존됨). 검증은 `DiagnoseFeet(애니이름)`으로 발 월드 y 전 구간 일정 확인 |
| 아틀라스 rotate:90 리전 | 페이지에 가로세로 스왑 상태로 저장됨 | bounds의 (w,h)는 원본 기준, 페이지 상 footprint는 (h,w). 블리팅 시 역회전 |
| **팔다리 파츠의 그림 위아래가 반대** (1_2RightArmT/B, LegT/B, AllLeftArmT 등 — 실루엣/포즈는 정상이라 전신 스크린샷으론 안 보임) | rotate:90 리전의 역회전 방향(CW/CCW)을 반대로 골랐음. 두 방향은 결과물이 180° 차이인데, **본 배치 최소제곱 피팅이 각도를 자동 보상해버려서 자세는 맞아 보이고 텍스처 내용물만 뒤집힘** — 관절 연결부 아트가 반대쪽 끝에 감 | `RotateVariantA = false`가 이 프로젝트 아틀라스의 정답. 새 몬스터 변환 시 **언팩 시트에서 팔다리 파츠의 관절 볼(둥근 끝)이 위(어깨/골반 쪽)에 오는지** 먼저 눈으로 확인하고, 최종적으로 원본과 클로즈업 대조할 것. 실루엣만 보고 검증 통과시키면 안 됨 |

## 4.5 커스텀 애니메이션 제작 (스파인 레퍼런스 없이 글 설명만으로) — Death로 검증 완료

"고개를 들고 하늘을 보면서 무릎을 꿇고 앞으로 꼬꾸라지는" Death 애니메이션을 글 설명만으로 제작해
성공한 워크플로우. 신규 애니메이션 요청은 이 절차를 따른다.

**구조**: 변환기의 `BuildDeathAnim()`처럼 스파인 타임라인 형식의 딕셔너리를 코드로 정의하고
`BakeClip()`으로 굽는다 (메뉴: `Tools > TEST UnityRig > Bake Death Animation`).
기존 베이크 파이프라인을 그대로 타므로 **IK(발바닥 고정)·패스 컨스트레인트·inherit 처리가 공짜로 적용**된다.
골반 하강만 키를 주면 무릎 꿇기는 IK가 알아서 만든다.

**절차**:
1. 글 설명을 구간별 키포즈로 분해 (Death: 응시 0~0.35s / 무릎꿇기 0.35~1.0s / 꼬꾸라짐 1.0~1.8s)
2. 본별 rotate/translate 키를 스파인 좌표(px, 도) 오프셋으로 정의
3. 베이크 → 플레이 모드에서 구간별 시점 동결 → 스크린샷 검증 → 값 수정 → 재베이크 (Death는 5회 반복으로 완성)

**핵심 교훈 — 회전 부호는 절대 추측하지 말 것** (Death에서 머리·상체·팔 전부 첫 추측이 틀렸음):
- **원본 데이터로 캘리브레이션하라**: 기존 애니메이션 키값과 그 시점의 실제 화면을 대조하면 부호가 확정된다.
  - 이 리그 확정값: **Head 음수 = 고개 들기** (Idle 키 +53=숙임/-31=들기로 확인),
    **Body1/2/3·Center rot 음수 = 뒤로 젖힘, 양수 = 앞으로 숙임** (Attack0 백스윙 Body2=-30으로 확인)
- 데이터가 없는 본(팔 등)은 A/B 스크린샷 반복으로 확정
- **IK 타깃(발) 키는 y=0 유지가 원칙** — 타깃 y를 올리면 발이 지면에서 떠버린다(사용자 지적으로 발견).
  쓰러짐처럼 골반이 크게 이동하는 포즈에서 다리를 정리하고 싶으면 x(지면 방향 슬라이드)만 소폭 키를 줄 것.
  발 고정 검증은 `DiagnoseFeet("Death")`처럼 수치로 확인(발 월드 y가 전 구간 일정해야 함)
- 비교 씬의 CompareDriver 인스펙터에 재생 버튼 있음(플레이 모드에서 Idle/Attack0/Buff0/Defence0/Death)

**이징 (버벅임 방지 — 필수)**: 커스텀 키는 선형 보간이 기본이었는데 구간 경계마다 속도가 급변해
버벅여 보였다(사용자 지적). 키 빌더 RK/TK는 이제 기본 `ease`(스무스), `Curve(keys, 키인덱스, 타입)`으로
구간별 지정: 낙하 = `easeIn`(중력 가속), 착지 반동 = `easeOut`(감속). 검증: 베이크된 커브의
프레임당 변화량이 0→점진 가속→(의도된 충격 반전)→감속 소멸로 이어지면 정상.

**스크린샷 검증 시 함정**: SpriteSkin 변형은 포즈 적용 후 다음 에디터 프레임에 반영된다.
포즈 설정과 캡처를 **반드시 별도 호출**로 하고, 캡처 결과가 직전과 동일하면 스테일이므로 한 번 더 캡처.

검증 스크린샷: `death_t30.png`(응시) / `death_t94.png`(무릎꿇기) / `death_t160.png`(꼬꾸라짐).
비교 씬 플레이 중 숫자키 5로 재생(유니티 리그 전용).

## 5. 알려진 한계 (미해결)

- **다중 본 메시(Body/Head)의 FFD 미반영** — 본 움직임이 주도해 시각 영향 적음.
- **그림자 크기 2.28배 축소** — SpriteBone에 스케일 필드가 없어 본 스케일 손실. 필요 시 그림자만 SpriteSkin 없이 본에 직접 부모화하는 방식으로 해결 가능.
- **슬롯 블렌드 모드(screen/additive/multiply) 미적용** — 기본 알파 블렌드로 렌더. EyeLight류는 additive 계열 머티리얼을 수동 지정하면 개선.
- **이벤트/드로우오더 타임라인 미변환** (이 몬스터는 이벤트만 존재, 게임 로직 연동 시 Animation Event로 수동 등록 필요).
- 클립 용량이 큼(본 전체 30fps 베이크, 클립당 2~4MB) — 필요 시 압축/키 리덕션 여지 있음.

---

## 6. 새 몬스터 리소스가 왔을 때 적용 절차

### A안. 기존 Spine 익스포트를 Unity로 변환 (이번에 검증한 경로)
1. `SpineToUnityRigConverter.cs` 상단 경로 상수를 새 몬스터로 수정 (`SrcDir`, `JsonPath`, `AtlasPath`, `PngPath`, `OutDir`, `PrefabPath` 등).
2. **플레이 모드 종료 확인** 후 `Tools > TEST UnityRig > Run All`.
3. 콘솔 경고 확인: 스케일 편차 경고(그림자류), IK 잔차 경고(>3°면 리그 구조가 다른 것 — 발 본 유무 확인).
   그리고 **언팩 시트(`*_Parts.png`)를 열어 rotate:90 리전 파츠들의 그림 방향이 올바른지 육안 확인**
   (관절 볼이 위쪽·아트 위아래가 원본과 일치하는지 — 실루엣만으론 오류가 안 보이므로 필수).
4. 비교 씬을 플레이 모드로 열어 Idle/공격/피격 육안 대조.
5. 새 몬스터가 이 몬스터에 없는 Spine 기능을 쓰면 추가 대응 필요: 다중 본 패스 컨스트레인트, 웨이트드 패스, 트랜스폼 컨스트레인트(현재 무시), 드로우오더 키 등 — 변환기가 경고 로그를 남기도록 되어 있음.

### B안. 신규 몬스터를 Unity에서 처음부터 제작 (권장 리깅 규칙)
Stage2·3 몬스터 수준으로 단순하게 만들면 이번에 겪은 격차가 아예 발생하지 않는다:
- 파츠 분리 PSD → PSD Importer → Skinning Editor에서 본+자동 웨이트
- FFD·패스 컨스트레인트 대신 **본만으로** 리깅 (필요 시 Unity 2D IK 패키지)
- 애니메이션 이름 표준: `Idle`(루프)/`Attack0`/`Buff0`/`Defense0` (+엘리트 `Attack1`), 철자는 `Defense0`으로 통일
- 허리 바운스류는 골반 본에 position 키를 직접 굽는 것으로 충분

### 공통: 실전(게임씬) 투입 전 필요한 코드 작업 (미착수)
현재 게임 코드는 `SkeletonAnimation`을 직접 참조한다. Unity 리그 몬스터를 게임에 넣으려면:
- `SpawnedMonster` / `TargetObject` / `M_TurnManager.Presentation`의 애니메이션 호출(`state.SetAnimation`,
  `Complete` 콜백, `timeScale`)을 Spine용/Animator용으로 추상화하는 어댑터 계층
- 사망 디졸브 연출: MeshRenderer 1개 전제(`CustomMaterialOverride` + MaterialPropertyBlock)를
  다중 SpriteRenderer 일괄 적용으로 변경
- 정렬(`sortingOrder`)·이펙트 스폰(`SkeletonDataAsset` 기반)도 동일하게 분기 필요
