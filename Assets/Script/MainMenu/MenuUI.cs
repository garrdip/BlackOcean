
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MenuUI : MonoBehaviour
{
    public DeckBookController deckBookController;

    public GameObject panelSettings;
    public GameObject multiplayCanvas;
    public GameObject menuCanvas;
    public GameObject deckBookCanvas;

    public Button buttonSinglePlay;
    public Button buttonMultiPlay;
    public Button buttonDeckBook;
    public Button buttonSettings;
    public Button buttonQuit;
    public Button buttonCloseDeckBook;


    void Start()
    {
        buttonSinglePlay.onClick.AddListener(() => HandleSinglePlay());
        buttonMultiPlay.onClick.AddListener(() => HandleMultiPlay());
        buttonDeckBook.onClick.AddListener(() => HandleDeckBook());
        buttonSettings.onClick.AddListener(() => HandleSettings());
        buttonQuit.onClick.AddListener(() => HandleQuit());
        buttonCloseDeckBook.onClick.AddListener(() => HandleCloseDeckBook());
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

    public void HandleDeckBook()
    {
        deckBookCanvas.SetActive(true);
        menuCanvas.SetActive(false);
        multiplayCanvas.SetActive(false);
        deckBookController.GetCardDataFromDatabase();
    }

    public void HandleCloseDeckBook()
    {
        menuCanvas.SetActive(true);
        multiplayCanvas.SetActive(false);
        deckBookCanvas.SetActive(false);
    }

    public void HandleSettings()
    {
        panelSettings.SetActive(true);
    }

    public void HandleQuit()
    {

    }
}
