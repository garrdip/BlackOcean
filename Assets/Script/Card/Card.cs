using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class Card : NetworkBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int index;
    public string cardName;

    private int originIndex;
    
    // 크기값
    private Vector3 originalScale = new Vector3(1f, 1f, 1f);
    private Vector3 targetScale = new Vector3(1.3f, 1.3f, 1.3f);

    // 위치값
    private Vector3 originalPosition;
    private Vector3 targetPosition;

    // 회전값
    private Vector3 originRotation;
    private Vector3 targetRotation;

    // 마우스가 오브젝트 위에 있는지 여부
    private bool isMouseOver = false; 

    private bool isCardClicked = false;

    void Start()
    {
        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        switch(index){
            case 0:
                rectTransform.anchoredPosition = new Vector3(-400f, 80f, 0f);
                originalPosition = rectTransform.anchoredPosition;
                originRotation = new Vector3(0f, 0f, 15f);
                rectTransform.rotation = Quaternion.Euler(originRotation);
                break;
            case 1:
                rectTransform.anchoredPosition = new Vector3(-250f, 120f, 0f);
                originalPosition = rectTransform.anchoredPosition;
                originRotation = new Vector3(0f, 0f, 10f);
                rectTransform.rotation = Quaternion.Euler(originRotation);
                break;
            case 2:
                rectTransform.anchoredPosition = new Vector3(-80f, 140f, 0f);
                originalPosition = rectTransform.anchoredPosition;
                originRotation = new Vector3(0f, 0f, 5f);
                rectTransform.rotation = Quaternion.Euler(originRotation);
                break;
            case 3:
                rectTransform.anchoredPosition = new Vector3(80f, 140f, 0f);
                originalPosition = rectTransform.anchoredPosition;
                originRotation = new Vector3(0f, 0f, -5f);
                rectTransform.rotation = Quaternion.Euler(originRotation);
                break;
            case 4:
                rectTransform.anchoredPosition = new Vector3(250f, 120f, 0f);
                originalPosition = rectTransform.anchoredPosition;
                originRotation = new Vector3(0f, 0f, -10f);
                rectTransform.rotation = Quaternion.Euler(originRotation);
                break;
            case 5:
                rectTransform.anchoredPosition = new Vector3(400f, 80f, 0f);
                originalPosition = rectTransform.anchoredPosition;
                originRotation = new Vector3(0f, 0f, -15f);
                rectTransform.rotation = Quaternion.Euler(originRotation);
                break;
            default:
                break;
        }
    }

    void Update()
    {
        if (isMouseOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
            targetPosition = new Vector3(transform.localPosition.x, 300f, transform.localPosition.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            DeckUI.instance.EmitCardHoverAction(index);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Euler(originRotation);
        }
    }

    // 마우스 진입할 때 마우스 오버 상태값 true 변경 및 해당 카드를 오브젝트 트리 맨 앞으로 변경
    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        originIndex = transform.GetSiblingIndex();
        transform.SetSiblingIndex(999);
        
    }

    // 마우스 벗어날때 마우스 오버 상태값 false 변경 및 해당 카드를 오브젝트 트리의 원래 포지션으로 변경
    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
        transform.SetSiblingIndex(originIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isCardClicked = !isCardClicked;
        Debug.Log("클릭:" + isCardClicked);
        if(NetworkClient.connection != null){
            GamePlayer gamePlayer = NetworkClient.connection.identity.gameObject.GetComponent<GamePlayer>();
            gamePlayer.CmdSpawnArrowEmitter(transform.position);
        }
    }
}
