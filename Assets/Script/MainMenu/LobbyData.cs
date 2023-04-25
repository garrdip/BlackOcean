using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LobbyData : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite onMouseHighLight;
    public Sprite offMouseDim;
    public GameObject passwordIcon;
    public TextMeshProUGUI lobbyNameText;
    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Image>().sprite = onMouseHighLight;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Image>().sprite = offMouseDim;
    }

    public void SetLockState(bool hasPassword)
    {
        passwordIcon.SetActive(hasPassword);
    }

    public void SetLobbyName(string lobbyName)
    {
        lobbyNameText.text = lobbyName;
    }
}
