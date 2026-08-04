# 다국어(현지화) 시스템 가이드

> 작성일: 2026-08-04 · 방식: **키 기반 스트링 테이블**
> 기준 언어는 **한국어(ko)**. 번역이 없는 항목은 자동으로 한국어로 표시된다.

---

## 1. 한눈에 보기

```
Assets/Resources/Language/
  Locales.csv     지원 언어 목록      code,displayName,font
  ko.csv          한국어 UI 문자열     key,text
  en.csv          영어 번역           key,text,source_ko
  <추가 언어>.csv                     key,text,source_ko
```

**언어를 하나 추가하는 데 필요한 일 = CSV 파일 1개 + Locales.csv 한 줄.**
게임 데이터(CardDB 등)도, 씬도, 코드도 건드리지 않는다.

조회 순서: `현재 언어 → 한국어 테이블 → DB CSV의 한국어 원문 → 키 그대로`

### 한국어 원문은 어디에 있나

| 대상 | 한국어 원문 위치 | 번역 키 |
|---|---|---|
| 카드 이름·설명 | `Assets/Resources/DB/CardDB.csv` | `card.<카드번호>.name` / `.desc` |
| 버프 | `DB/BuffDB.csv` | `buff.<enum>.name` / `.desc` |
| 툴팁 용어 | `DB/Description.csv` | `term.<용어키>.name` / `.desc` |
| 카드 특성 | `DB/CardCharacteristic.csv` | `characteristic.<enum>.name` |
| UI 문자열 | `Language/ko.csv` (다른 곳에 원문이 없으므로) | `ui.<이름>` |

즉 **한국어 작업은 지금까지와 똑같이 DB CSV에서** 하면 되고, 다른 언어만 스트링 테이블로 덮어쓴다.
카드를 추가할 때 번역 파일을 함께 고칠 필요가 없다 — 번역이 없으면 한국어가 나온다.

---

## 2. 언어 추가 절차

1. Unity 메뉴 **Tools ▸ 현지화 ▸ 현지화 도구** 실행
2. 로케일 코드 입력(`ja`, `zh-Hans`, `fr`, `es` …) → **내보내기**
   → `Assets/Resources/Language/<code>.csv` 생성. 모든 키가 `source_ko`(한국어 원문)와 함께 들어 있다
3. 번역가에게 이 CSV 한 장을 전달 → `text` 열만 채워서 회신
4. 받은 파일을 같은 경로에 덮어쓰기
5. `Locales.csv`에 한 줄 추가
   ```
   ja,日本語,,
   ```
   `displayName`은 **해당 언어 표기법으로** 적는다(사용자가 자기 언어를 찾을 수 있어야 하므로).
6. 게임 실행 → 환경설정(ESC) 드롭다운에 자동으로 노출

> 카드가 늘어난 뒤 다시 내보내면 **이미 번역된 값은 그대로 유지**되고 새 키만 빈칸으로 추가된다.

### 점검

- **번역 누락 리포트**: 언어별로 비어 있는 키를 집계
- **마크업 검증**: 중괄호 짝, `@{용어}` 키 존재, `*{카드번호}` 존재 여부 확인 (번역문도 함께 검사)

---

## 2-1. 현재 커버리지 (2026-08-04)

| 항목 | 키 수 | 한국어 | 영어 |
|---|---|---|---|
| 카드 이름·설명 | 796 | DB CSV | ✅ 전량 |
| 버프 | 86 | DB CSV | ✅ 전량 (ERIS_2ND/3RD 설명 2건은 원문도 미작성) |
| 툴팁 용어 | 78 | DB CSV | ✅ 전량 |
| 카드 특성 | 17 | DB CSV | ✅ 전량 |
| UI (씬·프리팹·코드) | 102 | ko.csv | ✅ 전량 |

**영어 번역은 1차 번역이다.** 출시 전 원어민 검수를 권장한다.
고유명사는 §3-1 용어집을 따르며, 용어를 바꾸려면 **CSV 한 칸만 고치면 된다** —
설명문은 이름을 직접 쓰지 않고 `@{용어}` · `*{카드번호}` 토큰으로 참조하기 때문이다.

### 3-1. 용어집 (영어 기준)

| 한국어 | English | 한국어 | English |
|---|---|---|---|
| 이치 | Ichi | 철귀 | Iron Demon |
| 힘의 이치 | Ichi of Power | 크기 | Size |
| 방어의 이치 | Ichi of Defense | 꽃가루 | Pollen |
| 이치의 저주 | Curse of Ichi | 개화 | Bloom |
| 이치의 축복 | Blessing of Ichi | 화합 | Harmony |
| 방어 | Block | 붕괴 | Collapse |
| 고행 / 고행길 | Penance / Path of Penance | 쇠락 | Decay |
| 위대한 자 | The Great One | 압도 | Overwhelm |
| 영웅 카드 | Hero Card | 기사도 | Chivalry |
| 파괴의 권능 | Authority of Destruction | 은하수 | Galaxy |
| 창조의 권능 | Authority of Creation | 별무리 | Starcluster |
| 뽑을/버린/잊혀진 덱 | Draw / Discard / Forgotten Pile | 고정 피해 | True Damage |
| 구원·영원·근원·찰나 | Salvation · Eternal · Origin · Ephemeral | 해방·중력·숙련 | Liberation · Gravity · Mastery |

강화(`_E`) 카드 이름은 `Enhanced <기본 이름>` 규칙으로 자동 생성했다.

---

## 3. 카드 설명문 마크업

모든 토큰은 **중괄호로 범위를 명시**한다. 공백에 의존하지 않으므로 단어 사이를 띄우지 않는 중국어·일본어에서도 그대로 동작한다.

| 마크업 | 뜻 | 예 |
|---|---|---|
| `@{용어키}` | 툴팁 용어 (Description.csv의 `info` 키) | `@{압도}를 제거` |
| `*{카드번호}` | 다른 카드 참조 → 그 카드의 번역된 이름으로 치환 | `*{H0} 카드 수만큼` |
| `~{텍스트}` | 굵게 강조 | `~{뽑을덱} 에서` |
| `!{수}` | 피해량 (힘의 이치·개화 반영) | `피해 !{15} 줍니다` |
| `#{수}` | 방어량 (방어의 이치 반영) | `방어 #{6} 얻습니다` |
| `^{수}` | 체력 | `^{5} 회복` |
| `&{수}` | 철귀 크기 | `크기 &{3}` |
| `${수}{수}` | 다단히트 — 피해량, 타수 | `피해 ${4}{3} 줍니다` |

### 번역가에게 전달할 규칙

- **중괄호 안은 절대 번역하지 않는다.** `@{압도}` 의 `압도`는 표시 문구가 아니라 **조회 키**다
- 중괄호 밖은 자유롭게 번역·재배치해도 된다. 어순이 달라도 무방하다
  - 한국어 `@{압도}를 8스택 쌓고 피해 !{15} 줍니다`
  - 영어 `Deal !{15} damage and gain 8 @{압도} stacks`
- 숫자 뒤 **조사(을/를)는 한국어에서만 자동으로 붙는다** — 다른 언어에서는 붙지 않으므로 문장을 그대로 쓰면 된다
- 다단히트 서식은 `markup.multi_hit` 키로 언어별로 바꿀 수 있다
  - 한국어 `{damage} {hits}번` → `4를 3번`
  - 영어 `{damage} ×{hits}` → `4 ×3`

---

## 4. UI 텍스트 연결

TMP 텍스트에 **`TextUpdater`** 컴포넌트를 붙이면 현재 언어로 채워진다.

- 인스펙터의 **키(key)** 필드에 번역 키를 입력한다 (예: `ui.Main_Settings`)
- 비워두면 `ui.<게임오브젝트 이름>`을 키로 쓴다 (기존 방식과의 호환)
- 번역이 없으면 **씬에 작성된 원래 문자열이 그대로 유지**된다 — 빈 화면이 되지 않는다

내용이 런타임에 정해지는 텍스트(채팅 등)는 **`FontUpdater`**를 붙여 폰트만 교체한다.

### 키를 붙이면 안 되는 텍스트

코드가 런타임에 값을 덮어쓰는 텍스트에는 `TextUpdater`를 붙이지 않는다 — 언어를 바꾸는 순간 값이 라벨로 되돌아간다.
현재 의도적으로 제외한 것들:

`TextMaxActionCost` · `TextCurrentActionCost` · `TextCardQueueName/Type/Desc` · `TextCardOwnerName` ·
`TextHazardState` · `TextRoomType` · `ToastMessageText`

이런 문구는 대신 **코드에서 키로 조회**한다 (§4-2).

### 4-2. 코드 안의 문자열

```csharp
// 원문을 폴백 인자로 남기므로, 번역이 없으면 지금과 똑같이 보인다
toast.Text(M_LanguageManager.Get("ui.msg.battle_elite", "전투 : 엘리트 몬스터"));

// 값이 끼어드는 문장은 치환자를 쓴다 (어순이 다른 언어를 위해 접미사 이어붙이기 금지)
text = M_LanguageManager.Get("ui.map.turn_count", "{0}턴").Replace("{0}", value.ToString());
```

> `"{0}턴"`처럼 **문장 전체를 하나의 키**로 둔다. `value + Get("턴")` 식으로 조각을 이어붙이면
> 어순이 다른 언어에서 문장이 깨지고, CSV 파서가 앞뒤 공백을 잘라 `30turns`가 된다.

### 4-3. CSV 작성 규칙

| 상황 | 표기 |
|---|---|
| 줄바꿈 | `\n` **두 글자**로 적는다 (로더가 실제 개행으로 변환). CSV는 한 줄이 한 항목이라 실제 개행을 넣으면 파싱이 깨진다 |
| 콤마·따옴표 포함 | 값을 `"`로 감싼다 (`""`로 따옴표 이스케이프) |
| 값 치환자 | `{0}`, `{1}` … 코드가 `.Replace("{0}", …)`로 채운다 |
| 앞뒤 공백 | **보존되지 않는다** (파서가 Trim). 공백이 의미가 있으면 문장 전체를 키로 만들 것 |

---

## 5. 폰트

`Locales.csv`의 `font` 열에 **Resources 기준 TMP 폰트 에셋 경로**를 적으면 그 언어에서 폰트를 교체한다.
비워두면 각 텍스트의 기존 폰트를 그대로 쓴다 (현재 ko/en 모두 비어 있음).

> 예전에는 실행 중 `TMP_FontAsset.CreateFontAsset(ttf)`로 폰트를 만들었지만 폐기했다.
> 한중일은 글리프가 수만 자라 런타임 생성 비용이 크다. **TMP 폰트 에셋을 미리 만들어 두고**
> `Assets/Resources/Language/Fonts/` 에 넣은 뒤 경로를 지정하는 방식으로 간다.
> CJK 추가 시에는 라틴 폰트에 CJK 폰트를 **fallback 체인**으로 연결하는 구성을 권장한다.
> 재배포 가능한 라이선스(Noto Sans 계열 등) 확인이 필요하다.

---

## 6. 초기 언어 결정

`저장된 사용자 선택(PlayerPrefs) → 스팀 클라이언트 언어 → 한국어`

스팀 언어명(`koreana`, `schinese` 등)은 `M_LanguageManager.ResolveSteamLocale()`에서 로케일 코드로 매핑한다.
지원 목록(`Locales.csv`)에 없는 언어면 한국어로 떨어진다.

---

## 7. 멀티플레이 주의점 (중요)

`Card`는 Mirror로 통째로 직렬화되므로 **클라이언트가 받은 `CardBase.name`/`description`은 "보낸 쪽 언어"의 문자열**이다.
따라서 화면에 카드 글자를 찍을 때는 반드시 로컬 카드 데이터를 거쳐야 각자 자기 언어로 본다.

```csharp
// 이렇게 (O)
textCardName.text = CardData.instance.GetLocalizedName(card.baseCard);
textCardDescription.text = CardData.instance.GetLocalizedDescription(card.baseCard);

// 이러면 상대 언어가 그대로 보인다 (X)
textCardName.text = card.baseCard.name;
```

같은 이유로 툴팁 목록(`info`/`cardInfo`)도 `CardData.instance.GetLocalCardBase(...)`로 얻은 쪽을 쓴다.
`CardBase`에 필드를 추가하면 **카드 동기화 대역폭이 늘어난다** — 번역 원문 같은 로컬 전용 데이터는 `CardData`가 따로 들고 있다.

---

## 8. 구성 요소

| 파일 | 역할 |
|---|---|
| `Assets/Script/Mangers/UIManager/M_LanguageManager.cs` | 로케일 목록·테이블 로드, 언어 전환, 조회(`Get`), 스팀 언어 감지 |
| `Assets/Script/Language/LocKey.cs` | 번역 키 생성 규칙 (런타임·에디터 도구가 공유) |
| `Assets/Script/Language/TextUpdater.cs` | TMP 텍스트 ↔ 번역 키 바인딩 |
| `Assets/Script/Language/FontUpdater.cs` | 폰트만 교체 |
| `Assets/Script/Card/CardMarkup.cs` | 설명문 마크업 파서 (언어별 규칙이 모이는 곳) |
| `Assets/Editor/LocalizationTools.cs` | 스켈레톤 내보내기 / 누락 리포트 / 마크업 검증 |

언어가 바뀌면 `M_LanguageManager.onLocaleChanged` → `CardData`·`BuffData`가 표시 문자열을 다시 만들고,
그 다음 `languageChangedCallback` → 화면의 `TextUpdater`들이 갱신된다.
