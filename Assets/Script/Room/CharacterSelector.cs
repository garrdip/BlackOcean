using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using TMPro;
using ProjectD;


public class CharacterSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static CharacterSelector instance = null;
    public float hoverScale = 0.3f; // 마우스 Enter 또는  Exit시 커지는 사이즈 값
    public TextMeshProUGUI characterLabel;
    public Character character;

    public static CharacterSelector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CharacterSelector>();
                if (instance == null)
                {
                    GameObject container = new GameObject("CharacterSelectorSingleton");
                    instance = container.AddComponent<CharacterSelector>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        RoomUI.instance.onCharacterSelect += OnCharacterSelected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x + hoverScale, gameObject.transform.localScale.y + hoverScale, 0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x - hoverScale, gameObject.transform.localScale.y - hoverScale, 0);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(NetworkClient.connection != null){
            GameObject currentPlayer = NetworkClient.connection.identity.gameObject;
            RoomPlayer roomPlayer = currentPlayer.GetComponent<RoomPlayer>();
            Debug.Log(roomPlayer.name);
        }
        if(RoomUI.instance != null){
            RoomUI.instance.EmitCharacterSelectEvent(eventData.pointerCurrentRaycast.gameObject);
        }  
    }

    // 캐릭터 선택 이벤트 수신 : 이벤트를 수신받아 선택한 캐릭터의 텍스트는 빨간색 나머지는 흰색으로 변경
    public void OnCharacterSelected(GameObject selectedGameObject)
    {
        if(selectedGameObject.name.Equals(gameObject.name)){
            characterLabel.color = Color.red;
        }else{
            characterLabel.color = Color.white;
        }
    }
}
