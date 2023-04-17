using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectD;
using Mirror;

public class CharacterSelectUI : InstanceD<CharacterSelectUI>
{
    [Header("Character Buttons")]
    public List<Button> characters;
    public GameObject RoomUIObject;
    public GameObject CharacterSelectUIObject;

    public GameObject selectedBackGround;

    [Header("Selected Icons Below Character")]
    public List<GameObject> selectedIcons;

    public List<GameObject> characterIllustration;
    public Button backButton;

    const float DIMMING = 0.5f;
    const float BRIGHT = 1f;


    void Start()
    {
        characters[0].onClick.AddListener(() => CharacterChange(Character.HONGDANHYANG));
        characters[1].onClick.AddListener(() => CharacterChange(Character.GEORK));
        characters[2].onClick.AddListener(() => CharacterChange(Character.ERIS));
        backButton.onClick.AddListener(() => BackToRoomUI());
        selectedIcons[0].SetActive(false);
        selectedIcons[1].SetActive(true);
        selectedIcons[2].SetActive(false);
        selectedIcons[3].SetActive(true);
        selectedIcons[4].SetActive(false);
        selectedIcons[5].SetActive(true);
        selectedBackGround.SetActive(false);
        characterIllustration[0].GetComponent<Image>().color = new Color(DIMMING,DIMMING,DIMMING);
        characterIllustration[1].GetComponent<Image>().color = new Color(DIMMING,DIMMING,DIMMING);
        characterIllustration[2].GetComponent<Image>().color = new Color(DIMMING,DIMMING,DIMMING);
    }

    void CharacterChange(Character character)
    {
        selectedBackGround.SetActive(true);
        RoomPlayer player = NetworkClient.connection.identity.gameObject.GetComponent<RoomPlayer>();
        player.character = character;
        if(player.isServer) RoomUI.instance.CMDReadyCheck();
        switch(character)
        {
            case Character.HONGDANHYANG :
                selectedBackGround.transform.localPosition = new Vector3(-500,50,0);
                break;
            case Character.GEORK :
                selectedBackGround.transform.localPosition = new Vector3(0,50,0);
                break;
            case Character.ERIS :
                selectedBackGround.transform.localPosition = new Vector3(500,50,0);
                break;
        }
        selectedIcons[0].SetActive(character == Character.HONGDANHYANG ? true : false);
        selectedIcons[1].SetActive(character == Character.HONGDANHYANG ? false : true);
        selectedIcons[2].SetActive(character == Character.GEORK ? true : false);
        selectedIcons[3].SetActive(character == Character.GEORK ? false : true);
        selectedIcons[4].SetActive(character == Character.ERIS ? true : false);
        selectedIcons[5].SetActive(character == Character.ERIS ? false : true);
        characterIllustration[0].GetComponent<Image>().color = character == Character.HONGDANHYANG ?  new Color(BRIGHT,BRIGHT,BRIGHT) : new Color(DIMMING,DIMMING,DIMMING);
        characterIllustration[1].GetComponent<Image>().color = character == Character.GEORK ?  new Color(BRIGHT,BRIGHT,BRIGHT) : new Color(DIMMING,DIMMING,DIMMING);
        characterIllustration[2].GetComponent<Image>().color = character == Character.ERIS ?  new Color(BRIGHT,BRIGHT,BRIGHT) : new Color(DIMMING,DIMMING,DIMMING);
    }


    void BackToRoomUI()
    {
        RoomUIObject.SetActive(true);
        CharacterSelectUIObject.SetActive(false);
    }
}
