using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class DeckBookUI : SingletonD<DeckBookUI>
{
    public delegate void OnChangeDeckBookOpenState(bool isOpen);
    public OnChangeDeckBookOpenState onChangeDeckBookOpenState;

    public GameObject dekcBookMenu;
    public GameObject cellPrefab;
    public Button buttonCloseDeckBook;
    public List<Button> tabButtons = new List<Button>();
    public List<GameObject> tabFrames = new List<GameObject>();
    public int currentTabIndex;


    void Start()
    {
        DontDestroyOnLoad(gameObject);
        M_NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as M_NetworkRoomManager;
        networkRoomManager.persistentComponents.Add(gameObject.name, gameObject);
        buttonCloseDeckBook.onClick.AddListener(() => HandleCloseDeckBook());
        for(int i=0; i<tabButtons.Count; i++){
            int buttonIndex = i;
            tabButtons[i].onClick.AddListener(() => ShowTab(buttonIndex));
        }
    }

    public void HandleCloseDeckBook()
    {
        dekcBookMenu.SetActive(false);
        if(onChangeDeckBookOpenState != null){
            onChangeDeckBookOpenState.Invoke(false);
        }
    }

    public void HandleOpenDeckBook()
    {
        dekcBookMenu.SetActive(true);
        if(onChangeDeckBookOpenState != null){
            onChangeDeckBookOpenState.Invoke(true);
        }
    }

    public void ShowTab(int index)
    {
        tabFrames[index].SetActive(true);
        tabButtons[index].image.color = new Color32(255, 255, 255, 255);
        currentTabIndex = index;
        HideOtherTabs(index);
    }

    public void HideOtherTabs(int index)
    {
        for(int i=0; i<tabButtons.Count; i++){
            if(i != index){
                tabButtons[i].image.color = new Color32(255, 255, 255, 70);
                tabFrames[i].SetActive(false);
            }
        }
    }
}
