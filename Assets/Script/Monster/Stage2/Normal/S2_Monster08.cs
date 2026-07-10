using System.Collections;
using Mirror;

// Stage2 일반 몬스터 (S2_M08). 행동 패턴은 MonsterDB에 데이터가 추가되면 DoAction 오버라이드로 구현한다.
public class S2_Monster08 : SpawnedMonster
{
    [Server]
    public override IEnumerator OnHitAnimation()
    {
        return PlayHitAnimationSequence("Defense0", 0.667f);
    }
}
