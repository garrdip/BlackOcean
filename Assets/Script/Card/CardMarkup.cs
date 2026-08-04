using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// 카드 설명문 마크업 파서 (다국어 대응).
///
/// 마크업 형식 — 모든 토큰은 중괄호로 범위를 명시한다.
///   @{용어키}      툴팁 용어 (Description.csv의 info 키). 뒤에 붙는 조사는 중괄호 밖에 둔다: @{압도}를
///   *{카드번호}    다른 카드 참조 — 해당 카드의 (번역된) 이름으로 치환
///   ~{텍스트}      굵게 강조 (뽑을덱/버린덱 등)
///   !{수}          피해량   (힘의 이치·개화 반영)
///   #{수}          방어량   (방어의 이치 반영)
///   ^{수}          체력
///   &amp;{수}          철귀 크기
///   ${수}{수}      다단히트 — 피해량, 타수
///
/// 예전 형식(공백으로 토큰을 끊고 조사를 최장 전방일치로 떼어내던 방식)은 단어 사이에 공백이 없는
/// 중국어·일본어에서 토큰을 찾지 못해 폐기했다. 중괄호 방식은 언어에 무관하게 파싱되고,
/// 번역가가 토큰 내용을 실수로 번역하는 사고도 막는다.
///
/// 조사(을/를)는 한국어일 때만 붙는다 (M_LanguageManager.IsKorean).
/// </summary>
public static class CardMarkup
{
    static readonly Regex TermPattern = new Regex(@"@\{([^{}]*)\}");
    static readonly Regex CardRefPattern = new Regex(@"\*\{([^{}]*)\}");
    static readonly Regex BoldPattern = new Regex(@"~\{([^{}]*)\}");
    static readonly Regex MultiHitPattern = new Regex(@"\$\{([^{}]*)\}\{([^{}]*)\}");
    static readonly Regex ValuePattern = new Regex(@"([!#\^&])\{([^{}]*)\}");

    const string DamageColor = "<color=green>";
    const string HpColor = "<#FF7F00>";
    const string BulkColor = "<color=purple>";
    const string ColorEnd = "</color>";

    /// <summary>
    /// 구조 토큰(@ * ~) 치환. 로드 시 1회 수행하며, 발견한 용어/카드 참조를 card에 등록한다.
    /// 수치 토큰(! # ^ &amp; $)은 표시 시점에 버프를 반영해야 하므로 여기서 건드리지 않는다.
    /// </summary>
    public static string ApplyStructural(string source, CardBase card, Func<string, string> cardNameByNumber,
                                         string[] termColors, string cardRefColor)
    {
        if (string.IsNullOrEmpty(source)) return source;

        int termColorIndex = 0;
        string result = TermPattern.Replace(source, match =>
        {
            string termKey = match.Groups[1].Value;
            int colorIndex = termColorIndex % termColors.Length;
            card?.info.Add(new Infomation(termKey, colorIndex));
            termColorIndex++;
            // 표시 문구는 번역된 용어명, 툴팁 조회 키는 원본 키(termKey)를 유지한다
            string display = M_LanguageManager.Get(LocKey.TermName(termKey), termKey);
            return termColors[colorIndex] + display + ColorEnd;
        });

        result = CardRefPattern.Replace(result, match =>
        {
            string cardNumber = match.Groups[1].Value;
            card?.cardInfo.Add(cardNumber);
            string name = cardNameByNumber != null ? cardNameByNumber(cardNumber) : null;
            return cardRefColor + (string.IsNullOrEmpty(name) ? cardNumber : name) + ColorEnd;
        });

        result = BoldPattern.Replace(result, match => "<b>" + match.Groups[1].Value + "</b>");
        return result;
    }

    /// <summary>
    /// 수치 토큰(! # ^ &amp; $) 치환. resolveValue로 버프 보정값을 받는다.
    /// resolveValue(sigil, 원본값) → 표시할 값. null이면 원본값 그대로 표시한다.
    /// </summary>
    public static string ApplyValues(string source, Func<char, int, int> resolveValue)
    {
        if (string.IsNullOrEmpty(source)) return source;

        string result = MultiHitPattern.Replace(source, match =>
        {
            string damageText = match.Groups[1].Value;
            string hitText = match.Groups[2].Value;
            string damage;
            if (int.TryParse(damageText, out int damageValue))
            {
                int shown = resolveValue != null ? resolveValue('$', damageValue) : damageValue;
                damage = DamageColor + shown + ColorEnd + Particle(shown);
            }
            else
            {
                damage = DamageColor + damageText + ColorEnd;
            }
            string hits = TermColorForHits() + hitText + ColorEnd;
            // 서식은 번역 가능 — 한국어 "{damage} {hits}번" / 영어 "{damage} ×{hits}"
            string format = M_LanguageManager.Get(LocKey.MarkupMultiHit, "{damage} {hits}번");
            return format.Replace("{damage}", damage).Replace("{hits}", hits);
        });

        result = ValuePattern.Replace(result, match =>
        {
            char sigil = match.Groups[1].Value[0];
            string raw = match.Groups[2].Value;
            string color = sigil == '^' ? HpColor : sigil == '&' ? BulkColor : DamageColor;

            if (!int.TryParse(raw, out int value))
                return color + raw + ColorEnd;

            int shown = resolveValue != null ? resolveValue(sigil, value) : value;
            // 크기(&)는 원래도 조사를 붙이지 않는다
            string particle = sigil == '&' ? "" : Particle(shown);
            return color + shown + ColorEnd + particle;
        });

        return result;
    }

    /// <summary>수치 토큰만 제거하지 않고 원본값 그대로 표시 (버프 미반영 정적 표시용).</summary>
    public static string ApplyValues(string source) => ApplyValues(source, null);

    // 한국어에서만 숫자 뒤에 조사(을/를)를 붙인다. 다른 언어는 문법이 다르므로 붙이지 않는다.
    static string Particle(int number)
    {
        if (!M_LanguageManager.IsKorean) return "";
        int lastDigit = Math.Abs(number) % 10;
        // 받침 있는 숫자(0,1,3,6,7,8) → '을', 없는 숫자(2,4,5,9) → '를'
        return (lastDigit == 0 || lastDigit == 1 || lastDigit == 3 || lastDigit == 6 || lastDigit == 7 || lastDigit == 8) ? "을" : "를";
    }

    static string TermColorForHits() => "<#0EB4FC>"; // 기존 colorList[2]와 동일한 타수 색
}
