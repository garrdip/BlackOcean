using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

/// <summary>
/// 레벨업 성장치 랜덤 분배 테이블.
/// CharacterStatDB의 GrowX = "1레벨 → 최대 레벨(LevelDB)까지의 총 성장치". 이 총량을 (MaxLevel-1)회의 레벨업 칸에
/// 시드 기반으로 랜덤 분배한다 — 레벨업마다 오르는 양은 플레이어(시드)마다 다르지만, 만렙에서의 합은 항상 GrowX로 같다.
///
/// 분배 방식: 각 칸에 [100-v, 100+v] 범위의 정수 가중치(v = BalanceDB LEVEL_GROWTH_VARIANCE_PERCENT)를 시드 난수로 부여하고
/// 총량을 가중치 비례로 배분(정수 최대 나머지법) → 합이 정확히 총량. 순수 함수 (seed, character, stat, level)이므로
/// 별도 상태 저장이 필요 없고(GamePlayer.growthSeed만 보존), 다음 레벨 성장치 미리보기도 같은 함수로 계산할 수 있다.
/// 난수/배분 모두 정수 연산(splitmix32)이라 플랫폼·런타임과 무관하게 같은 시드 → 같은 결과.
/// </summary>
public static class LevelGrowthTable
{
    public enum Stat { HP, STR, AGI, INT, DEF, MDEF, CTRL } // HP = 최대 HP (GrowHP)

    static readonly Dictionary<long, int[]> cache = new Dictionary<long, int[]>();

    /// <summary>레벨 reachedLevel(2 ~ MaxLevel)에 도달할 때 오르는 성장치. 범위 밖이면 0</summary>
    public static int GetGrowth(int seed, Character character, Stat stat, int reachedLevel)
    {
        int[] table = GetTable(seed, character, stat);
        int index = reachedLevel - 2; // Lv.2 도달 = 첫 칸
        if (index < 0 || index >= table.Length) return 0;
        return table[index];
    }

    /// <summary>1레벨 → 최대 레벨까지 총 성장치 (CharacterStatDB GrowX). 시드와 무관</summary>
    public static int GetTotal(Character character, Stat stat)
    {
        CharacterStatData.Entry entry = CharacterStatData.Get(character);
        if (entry == null) return 0;
        switch (stat)
        {
            case Stat.HP: return entry.growHP;
            case Stat.STR: return entry.growStr;
            case Stat.AGI: return entry.growAgi;
            case Stat.INT: return entry.growInt;
            case Stat.DEF: return entry.growDef;
            case Stat.MDEF: return entry.growMdef;
            case Stat.CTRL: return entry.growCtrl;
        }
        return 0;
    }

    static int[] GetTable(int seed, Character character, Stat stat)
    {
        long key = ((long)seed << 32) | (uint)(((int)character << 8) | (int)stat);
        if (cache.TryGetValue(key, out int[] cached)) return cached;
        int[] table = Build(seed, character, stat);
        cache[key] = table;
        return table;
    }

    static int[] Build(int seed, Character character, Stat stat)
    {
        int slots = LevelData.MaxLevel - 1; // 레벨업 횟수
        int total = GetTotal(character, stat);
        int[] table = new int[Math.Max(0, slots)];
        if (slots <= 0 || total <= 0) return table; // 음수 성장은 지원하지 않음 (0 처리)

        int variance = Mathf.Clamp(BalanceData.Get("LEVEL_GROWTH_VARIANCE_PERCENT", 50), 0, 100);
        uint state = (uint)seed ^ ((uint)character * 0x9E3779B9u) ^ ((uint)stat * 0x85EBCA6Bu);

        // 칸별 가중치 [100-v, 100+v]
        long[] weights = new long[slots];
        long weightSum = 0;
        for (int i = 0; i < slots; i++)
        {
            weights[i] = 100 - variance + (long)(Next(ref state) % (uint)(variance * 2 + 1));
            weightSum += weights[i];
        }

        // 가중치 비례 배분 — 몫을 먼저 주고, 나머지는 잉여(나머지)가 큰 칸부터 +1 (동률은 인덱스 순 → 결정적)
        long[] remainders = new long[slots];
        int assigned = 0;
        for (int i = 0; i < slots; i++)
        {
            long scaled = (long)total * weights[i];
            table[i] = (int)(scaled / weightSum);
            remainders[i] = scaled % weightSum;
            assigned += table[i];
        }
        int[] order = new int[slots];
        for (int i = 0; i < slots; i++) order[i] = i;
        Array.Sort(order, (a, b) =>
        {
            int cmp = remainders[b].CompareTo(remainders[a]);
            return cmp != 0 ? cmp : a.CompareTo(b);
        });
        for (int k = 0; k < total - assigned; k++) table[order[k]]++;
        return table;
    }

    // splitmix32 — 시드 결정적 난수 (System.Random 구현 차이에 의존하지 않기 위해 자체 구현)
    static uint Next(ref uint state)
    {
        state += 0x9E3779B9u;
        uint z = state;
        z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
        z = (z ^ (z >> 13)) * 0xC2B2AE35u;
        return z ^ (z >> 16);
    }
}
