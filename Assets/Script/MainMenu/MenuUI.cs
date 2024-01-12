
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
        buttonSinglePlay.onClick.AddListener(() => HandleSinglePlay());
        buttonMultiPlay.onClick.AddListener(() => HandleMultiPlay());
        buttonDeckBook.onClick.AddListener(() => HandleDeckBook());
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

    public void HandleDeckBook()
    {
        deckBookCanvas.SetActive(true);
        menuCanvas.SetActive(false);
        multiplayCanvas.SetActive(false);
        debris.SetActive(false);
        logoGroup.SetActive(false);
    }

    public void HandleCloseDeckBook()
    {
        menuCanvas.SetActive(true);
        multiplayCanvas.SetActive(false);
        deckBookCanvas.SetActive(false);
        debris.SetActive(true);
        logoGroup.SetActive(true);
    }

    public void HandleSettings()
    {
        panelSettings.SetActive(true);
    }

    public void HandleQuit()
    {

    }
}
