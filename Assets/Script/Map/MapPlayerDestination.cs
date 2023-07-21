using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class MapPlayerDestination : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeGamePlayer))]
    public GamePlayer gamePlayer;

    public SpriteRenderer spriteRenderer;
    public Sequence sequence;

    void Start()
    {
        transform.position = new Vector3(-100f, 0f, 0f);
        transform.SetParent(M_MapManager.instance.roommaps.transform);
    }

    // 오브젝트 활성화 시 시퀀스 재시작
    void OnEnable()
    {
        sequence.Restart();
    }

    // 오브젝트 비활성화 시 시퀀스 중지
    void OnDisable()
    {
        sequence.Pause();
    }

    // 오브젝트 파괴 시 시퀀스 제거
    void OnDestroy()
    {
        sequence.Kill();
    }

    // 맵 플레이어가 이동할 방 클릭시 해당 위치에 생성되는 표시 오브젝트의 업다운 바운스 애니매이션 함수
    public void MoveBounce(Vector3 position, bool isNewPosition)
    {
        // 시퀀스가 이전에 이미 있으면 제거
        if(sequence != null && sequence.active)
        {
            sequence.Kill();
            sequence = null;
        }
        // 업다운 바운스 무한 반복
        Tweener upTweener = transform.DOMoveY(transform.localPosition.y + 0.2f, 0.3f);
        Tweener downTweener = transform.DOMoveY(transform.localPosition.y, 0.3f);
        sequence = DOTween.Sequence()
            .Append(upTweener)
            .Append(downTweener)
            .SetLoops(-1);

        // 새로운 위치로 이동된 경우 시퀀스 재시작
        if(isNewPosition){
            sequence.Restart();
        }
    }

    // GamePlayer참조값에서 selectOrder값에 따라 해당 플레이어 소유의 표시 색상 변경
    public void OnChangeGamePlayer(GamePlayer oldValue, GamePlayer newValue)
    {
        if(newValue != null){
            switch(newValue.selectOrder)
            {
                case 0:
                    spriteRenderer.color = Color.red;
                    break;
                case 1:
                    spriteRenderer.color = Color.blue;
                    break;
                case 2:
                    spriteRenderer.color = Color.green;
                    break;
            }
        }
    }
}
