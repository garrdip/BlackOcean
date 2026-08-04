# 다국어(현지화) 시스템 가이드

> 작성일: 2026-08-04 · 방식: **키 기반 스트링 테이블**
> 기준 언어는 **한국어(ko)**. 번역이 없는 항목은 자동으로 한국어로 표시된다.

---

## 1. 한눈에 보기

```
Assets/Resources/Language/
  Locales.csv     지원 언어 목록      code,displayName,font,priorityFont
  ko.csv          한국어 UI 문자열     key,text
  en.csv          영어 번역           key,text,source_ko
  ja.csv          일본어 번역          key,text,source_ko
  zh-Hans.csv     중국어 간체 번역      key,text,source_ko
  es.csv          스페인어 번역         key,text,source_ko
  de.csv          독일어 번역           key,text,source_ko
  ru.csv          러시아어 번역         key,text,source_ko
  fr.csv          프랑스어 번역         key,text,source_ko
  <추가 언어>.csv                     key,text,source_ko
```

현재 지원 **8개 언어**: 한국어(ko) · English(en) · 日本語(ja) · 简体中文(zh-Hans) ·
Español(es) · Deutsch(de) · Русский(ru) · Français(fr)

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
   zh-Hant,繁體中文,,NotoSansTC SDF,
   ```
   `displayName`은 **해당 언어 표기법으로** 적는다(사용자가 자기 언어를 찾을 수 있어야 하므로).
   `font` 열은 비워두고, CJK라면 `priorityFont`에 폴백 폰트 이름을 적는다 (§5 참고).
6. 그 언어의 문자가 기존 폰트에 없다면 **폴백 폰트 등록**이 필요하다 (§5)
7. 게임 실행 → 환경설정(ESC) 드롭다운에 자동으로 노출

> 번역 테이블은 **최초 1회만 로드**된다. 에디터에서 CSV를 고치거나 언어를 추가한 뒤에는
> 현지화 도구의 **「언어 다시 읽기」**를 눌러야 반영된다 (게임을 새로 실행하면 자동 반영).

> 카드가 늘어난 뒤 다시 내보내면 **이미 번역된 값은 그대로 유지**되고 새 키만 빈칸으로 추가된다.

### 점검

- **번역 누락 리포트**: 언어별로 비어 있는 키를 집계
- **마크업 검증**: 중괄호 짝, `@{용어}` 키 존재, `*{카드번호}` 존재 여부 확인 (번역문도 함께 검사)

---

## 2-1. 현재 커버리지 (2026-08-04)

언어별 1,079키 = 카드 796 · 버프 86 · 용어 78 · 특성 17 · UI 102.

| 언어 | 상태 | 폰트 |
|---|---|---|
| 한국어 (ko) | 기준 언어 — 원문은 DB CSV + ko.csv | NotoSansKR |
| English (en) | ✅ 전량 | 기본 폰트 |
| 日本語 (ja) | ✅ 전량 | NotoSansJP (추가) |
| 简体中文 (zh-Hans) | ✅ 전량 | NotoSansSC (추가) |
| Español (es) | ✅ 전량 | 기본 폰트 |
| Deutsch (de) | ✅ 전량 | 기본 폰트 |
| Русский (ru) | ✅ 전량 | NotoSansExtra (추가) |
| Français (fr) | ✅ 전량 | 기본 + NotoSansExtra |

`ERIS_2ND`/`ERIS_3RD` 버프 설명 2건만 비어 있는데, 한국어 원문 자체가 미작성이라 번역 대상이 아니다.

**한국어를 제외한 7개 언어 모두 1차 번역이다.** 출시 전 원어민 검수를 권장한다.
고유명사는 §3-1 용어집을 따르며, 용어를 바꾸려면 **CSV 한 칸만 고치면 된다** —
설명문은 이름을 직접 쓰지 않고 `@{용어}` · `*{카드번호}` 토큰으로 참조하기 때문이다.

### 3-1. 용어집

**동아시아권**

| 한국어 | English | 日本語 | 简体中文 |
|---|---|---|---|
| 이치 | Ichi | 理 | 理 |
| 힘의 이치 / 방어의 이치 | Ichi of Power / of Defense | 力の理 / 防御の理 | 力之理 / 防御之理 |
| 이치의 저주 / 축복 | Curse / Blessing of Ichi | 理の呪い / 理の祝福 | 理之诅咒 / 理之祝福 |
| 방어 | Block | 防御 | 格挡 |
| 철귀 / 크기 | Iron Demon / Size | 鉄鬼 / サイズ | 铁鬼 / 尺寸 |
| 꽃가루 / 개화 | Pollen / Bloom | 花粉 / 開花 | 花粉 / 绽放 |
| 화합 | Harmony | 和合 | 和合 |
| 붕괴 / 쇠락 | Collapse / Decay | 崩壊 / 衰退 | 崩坏 / 衰败 |
| 압도 / 기사도 | Overwhelm / Chivalry | 圧倒 / 騎士道 | 压倒 / 骑士道 |
| 고행 / 고행길 | Penance / Path of Penance | 苦行 / 苦行の道 | 苦行 / 苦行之路 |
| 위대한 자 / 영웅 카드 | The Great One / Hero Card | 偉大なる者 / 英雄カード | 伟大者 / 英雄牌 |
| 파괴 / 창조의 권능 | Authority of Destruction / Creation | 破壊 / 創造の権能 | 破坏 / 创造之权能 |
| 은하수 / 별무리 | Galaxy / Starcluster | 銀河 / 星団 | 银河 / 星团 |
| 고정 피해 | True Damage | 固定ダメージ | 固定伤害 |
| 뽑을 / 버린 / 잊혀진 덱 | Draw / Discard / Forgotten Pile | 山札 / 捨て札 / 忘却札 | 抽牌堆 / 弃牌堆 / 遗忘堆 |
| 캐릭터명 | Geork / Eris / Danhyang | ゲオルク / エリス / ダンヒャン | 格奥尔克 / 艾莉丝 / 丹香 |

**유럽권**

| 한국어 | Español | Deutsch | Русский | Français |
|---|---|---|---|---|
| 이치 | Ichi | Ichi | Ичи | Ichi |
| 힘의 이치 / 방어의 이치 | Ichi de Poder / de Defensa | Ichi der Kraft / der Abwehr | Ичи Силы / Защиты | Ichi de Puissance / de Défense |
| 이치의 저주 / 축복 | Maldición / Bendición de Ichi | Fluch / Segen des Ichi | Проклятие / Благословение Ичи | Malédiction / Bénédiction d’Ichi |
| 방어 | Bloqueo | Block | Броня | Blocage |
| 철귀 / 크기 | Demonio de Hierro / Tamaño | Eisendämon / Größe | Железный демон / Размер | Démon de Fer / Taille |
| 꽃가루 / 개화 | Polen / Floración | Pollen / Blüte | Пыльца / Цветение | Pollen / Floraison |
| 화합 | Armonía | Harmonie | Гармония | Harmonie |
| 붕괴 / 쇠락 | Colapso / Declive | Kollaps / Verfall | Коллапс / Упадок | Effondrement / Déclin |
| 압도 / 기사도 | Abrumar / Caballerosidad | Überwältigen / Ritterlichkeit | Подавление / Рыцарство | Domination / Chevalerie |
| 고행 / 고행길 | Penitencia / Senda de Penitencia | Buße / Pfad der Buße | Епитимья / Путь епитимьи | Pénitence / Voie de la Pénitence |
| 위대한 자 / 영웅 카드 | El Gran Ser / Carta de Héroe | Der Große / Heldenkarte | Великий / Карта героя | Le Grand / Carte de Héros |
| 파괴 / 창조의 권능 | Autoridad de Destrucción / Creación | Macht der Zerstörung / Schöpfung | Власть Разрушения / Созидания | Autorité de Destruction / Création |
| 은하수 / 별무리 | Galaxia / Cúmulo Estelar | Galaxie / Sternhaufen | Галактика / Звёздное скопление | Galaxie / Amas d’Étoiles |
| 고정 피해 | Daño Fijo | Fixschaden | Чистый урон | Dégâts Fixes |
| 뽑을 / 버린 / 잊혀진 덱 | Mazo de Robo / Pila de Descarte / Pila de Olvido | Nachziehstapel / Ablagestapel / Vergessensstapel | Колода добора / Сброс / Забвение | Pioche / Défausse / Oubli |
| 캐릭터명 | Geork / Eris / Danhyang | Geork / Eris / Danhyang | Георк / Эрис / Данхян | Geork / Eris / Danhyang |

강화(`_E`) 카드 이름은 언어별 접사 규칙으로 자동 생성했다 —
`Enhanced X` / `強化X` / `强化X` / `Mejorada: X` / `Verbessert: X` / `Улучшено: X` / `Améliorée : X`.

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
  - 일본어 `{damage}ダメージを{hits}回`

### 굴절어(독일어 등) 주의

용어 토큰은 **항상 사전형(주격)으로 삽입**된다. 격·성·수에 따라 어미가 바뀌는 언어에서는
토큰을 전치사·관사 뒤처럼 굴절이 필요한 자리에 두면 문장이 어긋난다.

```
✗ Füge !{5} @{고정피해} zu.        → „Füge 5 Fixer Schaden zu“  (fixen 이어야 함)
✓ 용어명을 격 중립 복합어로       → „Fixschaden“
✗ Tausche den Platz mit der @{전열}. → „mit der Vorderste Reihe“ (vordersten 이어야 함)
✓ 토큰을 주격 자리로 재구성       → „Du und die @{전열} tauscht die Plätze.“
```

즉 **①용어명을 굴절에 덜 민감한 형태로 짓고 ②문장을 토큰이 주격에 오도록 쓰는** 두 가지로 해결한다.

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

글리프 보충은 **`TMP Settings`의 전역 폴백 목록**이 담당한다. 등록된 폰트:

| 폰트 에셋 | 담당 | 모드 | 소스 |
|---|---|---|---|
| `NotoSansKR-Bold SDF` | 한글 | Static (11,522자) | 기존 자산 |
| `NotoSansJP SDF` | 일본어 가나·한자 | **Dynamic** | `Assets/Font/NotoSansJP.otf` (11MB) |
| `NotoSansSC SDF` | 중국어 간체 한자 | **Dynamic** | `Assets/Font/NotoSansSC.otf` (10MB) |
| `NotoSansExtra SDF` | 키릴·라틴 확장·그리스 | **Dynamic** | `Assets/Font/NotoSansExtra.otf` (46KB) |

어떤 텍스트든 자기 폰트에 없는 글자가 나오면 이 목록에서 순서대로 찾아 쓴다.
씬의 폰트 지정을 건드리지 않으므로 **디자인은 그대로 두고 없는 글리프만 보충**되며,
`TextUpdater`를 붙이지 않은 런타임 텍스트(채팅·스팀 닉네임 등)까지 함께 해결된다.

그래서 `Locales.csv`의 `font` 열은 **비워두는 것이 기본**이다. 여기에 경로를 넣으면
그 언어에서 모든 `TextUpdater` 텍스트의 폰트가 통째로 교체되므로, 언어별 전용 서체를
쓰려는 의도가 있을 때만 채운다 (경로는 `Resources` 기준).

### priorityFont — 한자 자형 문제

일본어와 중국어는 **같은 유니코드 코드포인트라도 자형이 다른 한자**가 있다(直·骨·今 등의 세부 형태).
폴백은 앞에서부터 찾으므로 순서를 고정해 두면 중국어 화면에 일본어 자형이 섞인다.

그래서 `Locales.csv`에 **`priorityFont`** 열을 두고, 언어를 바꿀 때 해당 폰트를
폴백 목록 맨 앞으로 올린다 (`M_LanguageManager.ApplyFallbackPriority`).

```
code,displayName,font,priorityFont,note
ja,日本語,,NotoSansJP SDF,
zh-Hans,简体中文,,NotoSansSC SDF,
```

폰트를 교체하는 게 아니라 **'없는 글리프를 어디서 먼저 가져올지'만 조정**하므로,
각 텍스트의 디자인 폰트는 그대로 유지된다.

### CJK 폰트 준비 방법

두 폰트 모두 시스템의 `NotoSansCJK-Regular.ttc`에서 해당 언어 페이스만 추출한 뒤
표시에 필요한 유니코드 범위로 서브셋했다 (글리프 22,479자, 15.7MB → 10MB 수준). **SIL OFL 1.1**.
상세와 라이선스는 `Assets/Font/NotoSansJP-LICENSE.txt` · `NotoSansSC-LICENSE.txt`.

- TMP 폰트 에셋은 **Dynamic 모드**라 필요한 글리프만 실행 중에 아틀라스로 굽는다
  (에셋 자체는 8KB 수준. 참고로 한글 Static 에셋은 38MB다).
- 상용 한자만 남기는 축소도 가능하지만, 채팅·스팀 닉네임처럼 임의의 문자가 들어오는
  경로가 있어 통합 한자 전체를 남겼다.

> 번체(zh-Hant)를 추가하려면 같은 방식으로 TC 페이스를 서브셋해 폴백에 등록하고
> `priorityFont`를 지정하면 된다. 번역은 간체에서 자동 변환되지 않으므로 별도 작업이 필요하다.

### 키릴 문자권(러시아어) · 라틴 확장

기존 폰트에는 **키릴이 하나도 없었고**, 프랑스어 합자 `œ Œ` 도 대부분 빠져 있었다.
두 결손을 한 번에 메우려고 `NotoSansExtra.otf`(46KB, 글리프 302자)를 만들어 폴백 끝에 넣었다.
포함 범위: 라틴 확장 A/B · 결합 발음기호 · 그리스 · 키릴(+보충) · 통화기호.
자형 충돌이 없으므로 `priorityFont`는 지정하지 않는다.

### 라틴 문자권(영어·스페인어·독일어·프랑스어)

별도 폰트가 필요 없다. 악센트·움라우트(á é í ó ú ü ñ ¿ ¡ / ä ö ü ß)는 기본 폰트(LiberationSans·Anton)와
CJK 폴백 폰트에 모두 들어 있어 그대로 표시된다. `priorityFont`도 비워둔다.

> 예외: 대문자 에스체트 `ẞ`(U+1E9E)는 어느 폰트에도 없다. 독일어 관례상 대문자에서는
> `SS`로 적으므로 번역문에서 이 글자를 쓰지 않는 것으로 처리했다.

> 예전에는 실행 중 `TMP_FontAsset.CreateFontAsset(ttf)`로 폰트를 만들었지만 폐기했다 —
> CJK는 글리프가 수만 자라 런타임 생성 비용이 크다.

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
