
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public Button buttonSinglePlay;
    public Button buttonMultiPlay;
    public Button buttonDeckBook;
    public Button buttonSettings;
    public Button buttonQuit;

    public GameObject panelSettings;
    public GameObject multiplayPanel;

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
        multiplayPanel.SetActive(true);
    }

    public void HandleDeckBook()
    {

    }

    public void HandleSettings()
    {
        panelSettings.SetActive(true);
    }

    public void HandleQuit()
    {

    }
}
