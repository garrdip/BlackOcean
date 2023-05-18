using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using ProjectD;
using Steamworks;
using TMPro;

public class TargetObject : NetworkBehaviour
{
    [Header("HP 슬라이더")]
    public Slider hpbar;

    [Header("타겟 이름")]
    public TextMeshProUGUI textTargetName;

    [Header("삼각형 화살표")]
    public GameObject currentPlayerMark;

    [Header("Cost 아이콘")]
    public GameObject currentPlayerTargetCosts;

    [Header("바닥 오오라")]
    public GameObject currentPlayerGroundIndicator;

    [Header("타겟 오브젝트 타입")]
    [SyncVar]
    public ObjectType objectType;

    // Player 의 경우 
    [SyncVar (hook = nameof(InitTargetObjectPlayer))]
    public GamePlayer player;

    public CloneGamePlayer cloneGamePlayer;

    // Monster 의 경우
    [SyncVar (hook = nameof(InitTargetObjectEmemy))]
    public SpawnedMonster monster;

    public List<GameObject> characters;
    public List<GameObject> monsters;

    public TargetObject clone;
    public bool isCloneData = false;

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
            textTargetName.text = SteamFriends.GetFriendPersonaName((CSteamID)newVal.steamID);
            hpbar.maxValue = newVal.MaxHP;
            hpbar.value = newVal.HP;
            if(newVal.isLocalPlayer){
                currentPlayerMark.SetActive(true);
                currentPlayerTargetCosts.SetActive(true);
                currentPlayerGroundIndicator.SetActive(true);
                float hpbarWidth = hpbar.GetComponent<RectTransform>().rect.width;
                float hpbarHeight = hpbar.GetComponent<RectTransform>().rect.height;
                hpbar.GetComponent<RectTransform>().sizeDelta = new Vector2(hpbarWidth + 300f, hpbarHeight + 100f);
            }
        }
    }

    public void InitTargetObjectEmemy(SpawnedMonster oldVal, SpawnedMonster newVal)
    {
        StartCoroutine(nameof(EmemyTargetObjectGenerator));
        textTargetName.text = newVal.monsterData.name;
        hpbar.maxValue = newVal.MAXHP;;
        hpbar.value = newVal.HP;
    }

    IEnumerator EmemyTargetObjectGenerator()
    {
        WaitForSeconds loopSecond = new WaitForSeconds(0.01f);
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
            yield return loopSecond;
        }
    }

    void FixedUpdate()
    {
        if(isServer)
        {
            if(objectType == ObjectType.PLAYER) 
            {
                // 플레이어 사망시 처리
            }
            else
            {
                if(monster == null)
                {
                    if(isCloneData)
                        M_TurnManager.instance.cloneMonsterList.Remove(this);
                    else
                        M_TurnManager.instance.spawnedMonsterList.Remove(this);
                        
                    NetworkServer.Destroy(this.gameObject);
                }
            }
        }
    }

}
