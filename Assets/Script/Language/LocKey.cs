/// <summary>
/// 번역 키 생성 규칙 한 곳 모음.
/// 런타임 조회(각 DB 로더)와 에디터 내보내기(LocalizationTools)가 같은 함수를 쓰므로
/// 키 규칙이 어긋나 "번역했는데 안 나오는" 사고가 생기지 않는다.
///
/// 키 체계
///   ui.&lt;오브젝트/식별자&gt;          UI 문자열 (한국어 원문은 Language/ko.csv 에만 존재)
///   buff.&lt;enum&gt;.name / .desc      버프 (한국어 원문은 DB/BuffDB.csv)
/// (카드 card.* / 특성 characteristic.* / 툴팁 용어 term.* / 서식 markup.* 은 카드 시스템 제거로 폐기 — 2026-09-01)
/// </summary>
public static class LocKey
{
    public static string Ui(string name) => "ui." + name;

    public static string BuffName(string buffEnum) => "buff." + buffEnum + ".name";
    public static string BuffDesc(string buffEnum) => "buff." + buffEnum + ".desc";
}
