
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MenuUI : MonoBehaviour
{
    public GameObject panelSettings;
    public GameObject multiplayCanvas;
    public GameObject menuCanvas;
    public GameObject deckBookCanvas;
    public GameObject debris;
    public GameObject logoGroup;

    public Button buttonSinglePlay;
    public Button buttonMultiPlay;
    public Button buttonDeckBook;
    public Button buttonSettings;
    public Button buttonQuit;
    public Button buttonCloseDeckBook;


    void Start()
    {
        DeckBookUI.instance.onChangeDeckBookOpenState += OnChangeDeckBookOpenState;
        buttonSinglePlay.onClick.AddListener(() => HandleSinglePlay());
        buttonMultiPlay.onClick.AddListener(() => HandleMultiPlay());
        buttonDeckBook.onClick.AddListener(() => HandleOpenDeckBook());
        buttonSettings.onClick.AddListener(() => HandleSettings());
        buttonQuit.onClick.AddListener(() => HandleQuit());
        panelSettings.SetActive(false);
    }

    public void HandleSinglePlay()
    {

    }   

    public void HandleMultiPlay()
    {
        menuCanvas.SetActive(false);
        deckBookCanvas.SetActive(false);
        multiplayCanvas.SetActive(true);
    }

    public void HandleOpenDeckBook()
    {
        DeckBookUI.instance.HandleOpenDeckBook();
    }

    public void HandleCloseDeckBook()
    {
        DeckBookUI.instance.HandleCloseDeckBook();
    }

    public void OnChangeDeckBookOpenState(bool isOpen)
    {
        if(menuCanvas != null && debris != null && logoGroup != null){
            menuCanvas.SetActive(!isOpen);
            debris.SetActive(!isOpen);
            logoGroup.SetActive(!isOpen);
        }
    }

    public void HandleSettings()
    {
        panelSettings.SetActive(true);
    }

    public void HandleQuit()
    {

    }
}
