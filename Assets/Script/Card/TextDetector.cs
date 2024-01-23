using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


public class TextDetector : SingletonD<TextDetector>
{
    private GraphicRaycaster graphicRaycaster; // UI 전용 레이캐스터
    private List<RaycastResult> raycastResults; // graphicRaycaster로 감지한 오브젝트들의 목록
    private PointerEventData pointerEventData;
    public bool isWorldObject = false;
    public bool isUIObject = false;


    void Start()
    {
        pointerEventData = new PointerEventData(EventSystem.current);
        raycastResults = new List<RaycastResult>();
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        isWorldObject = false;
        isUIObject = false;
    }

    void Update()
    {
        if(isUIObject){
            DetectTextOnUI();
        }else{
            DetectTextOnWOrld();
        }
    }

    // 텍스트 감지 시작
    public void StartTextDetect(GraphicRaycaster graphicRaycaster = null)
    {
        gameObject.SetActive(true); // TextDetector 오브젝트 활성화
        this.graphicRaycaster = graphicRaycaster != null ? graphicRaycaster : null; // GraphicRaycaster 설정
        isWorldObject = graphicRaycaster != null ? false : true; // 월드 오브젝트인지 구분값 설정
        isUIObject = graphicRaycaster != null ? true : false; // UI 오브젝트인지 구분값 설정
    }

    // 텍스트 감지 중지
    public void StopTextDetect()
    {
        gameObject.SetActive(false); // Update 루프가 돌지 않도록 TextDetector 오브젝트 비활성화
        if(graphicRaycaster != null){
            graphicRaycaster = null; // GraphicRaycaster 초기화
        }
    }

    // 월드 오브젝트의 텍스트 감지
    private void DetectTextOnWOrld()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit)){
            if(hit.collider.GetComponent<CardOnHand>() != null){
                CardOnHand cardOnHand = hit.collider.GetComponent<CardOnHand>();
                TextMeshProUGUI textDescription = cardOnHand.textCardDescription;
                TMP_Text tmpText = textDescription.GetComponent<TMP_Text>();
                int wordIndex = TMP_TextUtilities.FindIntersectingWord(tmpText, Input.mousePosition, Camera.main);
                if(wordIndex != -1){
                    TMP_WordInfo tMP_WordInfo = tmpText.textInfo.wordInfo[wordIndex];
                    string word = tMP_WordInfo.GetWord();
                    // Debug.Log("월드 텍스트 감지" + word);
                    // TODO : word 값이 특수용어와 같을 경우 팝업 표시
                }else{
                    // TODO : 팝업 숨기기
                }
            }
        }
    }

    // UI 오브젝트의 텍스트 감지
    private void DetectTextOnUI()
    {
        if(graphicRaycaster != null){
            pointerEventData.position = Input.mousePosition;
            graphicRaycaster.Raycast(pointerEventData, raycastResults);  // pointerEventData 위치에 Raycast하여 결과 값 저장
            if(raycastResults.Count > 0){
                foreach(RaycastResult raycastResult in raycastResults){
                    if(IsTextChildOfCard(raycastResult)){ // 충돌한 UI가 카드오브젝트의 자식오브젝트 + TextMeshProUGUI인 경우
                        TextMeshProUGUI textDescription = raycastResult.gameObject.GetComponent<TextMeshProUGUI>();
                        TMP_Text tmpText = textDescription.GetComponent<TMP_Text>();
                        int wordIndex = TMP_TextUtilities.FindIntersectingWord(tmpText, pointerEventData.position, GetCameraByRenderMode(textDescription));
                        if(wordIndex != -1){
                            TMP_WordInfo tMP_WordInfo = tmpText.textInfo.wordInfo[wordIndex];
                            string word = tMP_WordInfo.GetWord();
                            // Debug.Log("UI 텍스트 감지" + word);
                            // TODO : word 값이 특수용어와 같을 경우 팝업 표시
                        }else{
                            // TODO : 팝업 숨기기
                        }
                    }
                }
            }
            raycastResults.Clear();
        }
    }

    private bool IsTextChildOfCard(RaycastResult raycastResult)
    {
        TextMeshProUGUI textDescription = raycastResult.gameObject.GetComponent<TextMeshProUGUI>();
        return
            raycastResult.gameObject.GetComponent<TextMeshProUGUI>()
            && 
            (textDescription.transform.GetComponentInParent<CardOnBook>() != null || 
            textDescription.transform.GetComponentInParent<CardOnDeck>() != null || 
            textDescription.transform.GetComponentInParent<CardOnHand>() != null);  
    }

    private Camera GetCameraByRenderMode(TextMeshProUGUI textDescription)
    {
        Camera cam = Camera.main;
        TMP_Text tmpText = textDescription.GetComponent<TMP_Text>();
        if(tmpText.GetType() == typeof(TextMeshProUGUI)){
            Canvas textCanvas = textDescription.GetComponentInParent<Canvas>();
            if(textCanvas != null){
                if(textCanvas.renderMode == RenderMode.ScreenSpaceOverlay){
                    cam = null;
                }else{
                    cam = textCanvas.worldCamera;
                }
            }
        }
        return cam;
    }

}
