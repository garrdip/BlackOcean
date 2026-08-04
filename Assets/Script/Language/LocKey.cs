/// <summary>
/// 번역 키 생성 규칙 한 곳 모음.
/// 런타임 조회(각 DB 로더)와 에디터 내보내기(LocalizationTools)가 같은 함수를 쓰므로
/// 키 규칙이 어긋나 "번역했는데 안 나오는" 사고가 생기지 않는다.
///
/// 키 체계
///   ui.&lt;오브젝트/식별자&gt;          UI 문자열 (한국어 원문은 Language/ko.csv 에만 존재)
///   card.&lt;카드번호&gt;.name / .desc   카드 이름·설명 (한국어 원문은 DB/CardDB.csv)
///   buff.&lt;enum&gt;.name / .desc      버프 (한국어 원문은 DB/BuffDB.csv)
///   term.&lt;용어키&gt;.name / .desc     툴팁 용어 (한국어 원문은 DB/Description.csv)
///   characteristic.&lt;enum&gt;.name    카드 특성명 (한국어 원문은 DB/CardCharacteristic.csv)
///   markup.*                       설명문 조립용 서식 문자열
/// </summary>
public static class LocKey
{
    public static string Ui(string name) => "ui." + name;

    public static string CardName(string cardNumber) => "card." + cardNumber + ".name";
    public static string CardDesc(string cardNumber) => "card." + cardNumber + ".desc";

    public static string BuffName(string buffEnum) => "buff." + buffEnum + ".name";
    public static string BuffDesc(string buffEnum) => "buff." + buffEnum + ".desc";

    public static string TermName(string termId) => "term." + termId + ".name";
    public static string TermDesc(string termId) => "term." + termId + ".desc";

    public static string Characteristic(string characteristicEnum) => "characteristic." + characteristicEnum + ".name";

    /// <summary>$피해$타수 조립 서식. {damage}/{hits} 치환자 사용.</summary>
    public const string MarkupMultiHit = "markup.multi_hit";
}
