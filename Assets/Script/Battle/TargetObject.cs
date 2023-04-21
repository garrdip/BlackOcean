using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;

public class TargetObject : NetworkBehaviour
{
    [Header("HP Slider")]
    public Slider hpbar;

    [SyncVar]
    public ObjectType objectType;

    // Player 의 경우 
    [SyncVar (hook = nameof(InitTargetObjectPlayer))]
    public GamePlayer player;

    // Monster 의 경우
    [SyncVar (hook = nameof(InitTargetObjectEmemy))]
    public SpawnedMonster monster;

    public List<GameObject> characters;
    public List<GameObject> monsters;

    public void InitTargetObjectPlayer(GamePlayer oldVal, GamePlayer newVal)
    {
        if(objectType == ObjectType.PLAYER)
        {
            switch(player.character)
            {
                case Character.GEORK :
                    Instantiate(characters[2],transform.position,Quaternion.identity,transform);
                break;
                case Character.ERIS :
                    Instantiate(characters[1],transform.position,Quaternion.identity,transform);
                break;
                case Character.HONGDANHYANG :
                    Instantiate(characters[0],transform.position,Quaternion.identity,transform);
                break;
            }
            hpbar.maxValue = newVal.MaxHP;
            hpbar.value = newVal.HP;
        }
    }
    public void InitTargetObjectEmemy(SpawnedMonster oldVal, SpawnedMonster newVal)
    {
        StartCoroutine(nameof(EmemyTargetObjectGenerator));
        hpbar.maxValue = newVal.MAXHP;;
        hpbar.value = newVal.HP;
    }

    IEnumerator EmemyTargetObjectGenerator()
    {
        while(true)
        {
            if(objectType == ObjectType.ENEMY && monster.monsterData != null)
            {
                switch(monster.monsterData.name)
                {
                    case "Monster_Goblin" :
                        Instantiate(monsters.Find(prefab => prefab.name == "Goblin"),transform.position,Quaternion.identity,transform);
                    break;
                    case "Monster_Troll" :
                        Instantiate(monsters.Find(prefab => prefab.name == "Troll"),transform.position,Quaternion.identity,transform);
                    break;
                }
                break;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

}
