using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProjectD;
using Spine.Unity;
using Spine.Unity.Examples;
using Gpm.Ui;
using AYellowpaper.SerializedCollections;
using System.Linq;


// M_TurnManager partial — 카드 큐 파이프라인 (등록/실행/SyncList 콜백)
public partial class M_TurnManager
{

    public IEnumerator ProcessCardQueue()
    {
        // 무한루프에서 인스턴스 생성시 생기는 가비지 방지를 위해 함수호출에서 미리 인스턴스 생성하여 캐싱후 루프 안에서 사용
        WaitForSeconds waitForLoop = new WaitForSeconds(0.01f);
        while (true)
        {
            yield return waitForLoop;
            if(CardData.instance.isCardOperating || monsterDeathOperating){
                continue;
            }
            else
            {
                if(cardTargetPairQueue.Count != 0){
                    CardData.instance.isCardOperating = true;
                    isCardQueueOperating = true;
                    (GamePlayerDeck gpd, int totalCost, CardOnHand cardOnHand,List<TargetObject> tar) = cardTargetPairQueue.Dequeue(); // 큐에서 하나씩 빼서 카드의 타겟에 대한 로직 수행
                    
                    SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.INCREASE); // 해당 인덱스의 카드 큐를 현재 카드 큐로 설정
                    if(cardOnHand.card.baseCard.isTargetable && tar[1] == null)
                    {
                        gpd.ReturnToCardOnHand(cardOnHand);
                        cardQueueList.RemoveAt(cardQueueList.Count - 1);
                        SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.DECREASE);
                        gpd.currentIchi += totalCost;
                        CardData.instance.isCardOperating = false;
                    }
                    else
                    {
                        if(tar[1].isDying)
                        {
                            gpd.ReturnToCardOnHand(cardOnHand);
                            cardQueueList.RemoveAt(cardQueueList.Count - 1);
                            SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.DECREASE);
                            gpd.currentIchi += totalCost;
                            CardData.instance.isCardOperating = false;
                            continue;
                        }

                        foreach(int index in tar[0].buffCardUseEffect.Keys)
                        {
                            yield return tar[0].buffCardUseEffect[index](tar[0],index,cardOnHand.card);
                        }

                        // 별무리 판정은 사용 '전' 보유 기준 — 이 카드가 부여하는 스택은 다음 카드부터 적용
                        bool hasByeolmuri = tar[0].HasBuff(BuffType.BYEOLMURI);
                        tar[0].cardDamageDealt = 0;
                        tar[0].ApplyPowerOfDestruction(cardOnHand.card); // 파괴의권능(에리스 패시브) — 체력 소모 및 피해 배수 설정

                        yield return CardData.instance.RunCard(cardOnHand.card,tar);

                        // 도돌이표: 이번 턴에 사용되는 공격 카드는 2번 사용 (대상이 첫 실행으로 사망하면 중단)
                        if(cardOnHand.card.baseCard.cardType == CardType.ATTACK && tar[0].HasBuff(BuffType.REPEATMARK)
                            && !(cardOnHand.card.baseCard.isTargetable && tar[1] != null && tar[1].isDying))
                            yield return CardData.instance.RunCard(cardOnHand.card,tar);

                        tar[0].destructionMultiplier = 1; // 파괴의권능 배수는 이 카드 실행(도돌이표 반복 포함)까지만 유효

                        // 별무리: 공격 = 준 피해 절반 방어 / 전략 = 사용 후 다시 패 / 그 외 = 비용 2 감소(지불 시 적용) — 사용 후 스택 1 제거
                        bool byeolmuriReturnToHand = false;
                        if(hasByeolmuri)
                        {
                            if(cardOnHand.card.baseCard.cardType == CardType.ATTACK)
                                tar[0].GainDefense(tar[0].cardDamageDealt / 2);
                            else if(cardOnHand.card.baseCard.cardType == CardType.STRATEGY)
                                byeolmuriReturnToHand = true;
                            tar[0].GainBuff(BuffType.BYEOLMURI, -1, false, false, false, false, tar[0], null);
                        }

                        if(CardData.instance.CheckCardCharacteristic(cardOnHand.card,CardCharacteristic.HWAHAP))
                            yield return CardData.instance.HWAHAP(tar[0]);
                        if(CardData.instance.CheckCardCharacteristic(cardOnHand.card,CardCharacteristic.SOOKREON))
                            cardOnHand.card.costAddition --;
                        if(CardData.instance.CheckCardCharacteristic(cardOnHand.card,CardCharacteristic.JOONGREUK))
                            cardOnHand.card.costAddition ++;

                        // 서버에서 변경된 카드 상태(경험치, 특성에 의한 비용 가감)를 클라이언트에 동기화
                        // — 클래스 타입 SyncVar는 같은 참조의 내부 필드 변경으로는 전파되지 않으므로 복사본으로 재할당한다
                        cardOnHand.card = new Card(cardOnHand.card);

                        if(cardOnHand.card.isReturnable){
                            gpd.ReturnToCardOnHand(cardOnHand);
                            cardQueueList.RemoveAt(cardQueueList.Count - 1);
                            SerCurrentCardQueue(cardOnHand, gpd.netId, INDEX_OPERATION.DECREASE);
                        }else if(byeolmuriReturnToHand){
                            gpd.ReturnToCardOnHand(cardOnHand); // 별무리 전략 강화 — 카드는 사용 처리되고 패로 복귀 (큐 표시는 유지)
                        }else{
                            gpd.destroyCardList.Add(cardOnHand);
                        }
                        gpd.numOfUsedCard++;
                        if(cardOnHand.card.baseCard.cardType == CardType.ATTACK)
                        {
                            gpd.numOfUsedAttackCardOnTurn++; // E54 산개 성단 — 실행 후 증가라 해당 카드 자신은 미포함
                            gpd.numOfUsedAttackCardOnBattle++; // E15 권능:파괴
                        }
                        // 카드 사용후 효과 여기서 발동

                    }
                }
                else
                {
                    isCardQueueOperating = false;
                }
            }
        }
    }


    // 카드 큐 Synclist에 데이터 추가
    [Server]
    public void AddCardQueueList(CardOnHand cardOnHand, uint netId)
    {
        if(!cardOnHand.card.baseCard.cardNumber.Equals("HA")){ // 철귀 이동 카드는 제외
            CardQueue cardQueue = new CardQueue(){
                cardOwnerNetId = netId,
                card = cardOnHand.card
            };
            M_TurnManager.instance.cardQueueList.Add(cardQueue);
        }
    }


    // 해당 인덱스의 카드 큐를 현재 카드 큐로 설정
    [Server]
    public void SerCurrentCardQueue(CardOnHand cardOnHand, uint netId, INDEX_OPERATION operation)
    {
        if(!cardOnHand.card.baseCard.cardNumber.Equals("HA")){ // 철귀 이동 카드는 제외  
            currentCardQueueIndex = operation == INDEX_OPERATION.INCREASE ? (currentCardQueueIndex + 1) : (currentCardQueueIndex - 1);
            CardQueue cardQueue = new CardQueue(){
                cardOwnerNetId = netId,
                card = cardOnHand.card
            };
            cardQueueList[currentCardQueueIndex] = cardQueue;
        }
    }


    // 카드 큐 시스템
    // 1. 큐에서 Enqueue 될 때 Synclist에도 동일하게 추가(Add)하여 카드 큐 오브젝트를 생성
    // 2. 큐에서 Dequeue 될 때 타겟이 유효하지 않아 되돌아올 경우 Synclist에서 마지막 데이터를 제거(RemoveAt)하여 카드 큐 오브젝트 제거
    // 3. 큐에서 Dequeue 될 때 카드 사용이 유효하면, Synclist에 데이터 변경(Set)하여 현재 수행된 카드 큐 오브젝트 표시
    private void OnCardQueueUpdated(SyncList<CardQueue>.Operation op, int index, CardQueue oldVal, CardQueue newVal)
    {
        switch (op)
        {
            case SyncList<CardQueue>.Operation.OP_ADD:
                GameUIManager.instance.infiniteScroll.InsertData(newVal);
                int lastIndex = GameUIManager.instance.infiniteScroll.GetItemCount() - 1;
                GameUIManager.instance.infiniteScroll.MoveTo(lastIndex, InfiniteScroll.MoveToType.MOVE_TO_CENTER, 0.5f);
                break;
            case SyncList<CardQueue>.Operation.OP_REMOVEAT:
                GameUIManager.instance.infiniteScroll.RemoveData(index);
                break;
            case SyncList<CardQueue>.Operation.OP_SET:
                for(int i=0; i<GameUIManager.instance.infiniteScroll.GetDataList().Count; i++){
                    InfiniteScrollData infiniteScrollData = GameUIManager.instance.infiniteScroll.GetDataList()[i];
                    CardQueue cardQueue = (CardQueue)infiniteScrollData;
                    cardQueue.isCurrent = (i == index) ? true : false;
                }
                onCurrentCardQueueUpdated?.Invoke(index);
                break;
            case SyncList<CardQueue>.Operation.OP_CLEAR:
                GameUIManager.instance.infiniteScroll.Clear();
                break;
        }
    }
}
