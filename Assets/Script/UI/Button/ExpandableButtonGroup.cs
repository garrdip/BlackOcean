using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class ExpandableButtonGroup : MonoBehaviour
{
    private Canvas canvas;
    public List<Button> expandableButtons = new List<Button>();
    private const float defaultStartAngle = 180f;
    private const float defaultEndAngle = 90f;
    private const int defaultCount = 3;
    private const float angleChangePerButton = 10f; // 버튼 갯수당 추가 및 감소 될 각도 비율값
    private float radius;


    void Awake()
    {
        canvas = transform.parent.GetComponent<Canvas>();
    }

    void Start()
    {
        canvas.GetComponent<GraphicRaycaster>().enabled = false; // 월드 스페이스 캔버스 이기 때문에 인게임 오브젝트의 클릭이벤트를 블로킹하지 않도록 비활성화. Expand될때 버튼 클릭할수 있도록 활성화.
        radius = 120f + (expandableButtons.Count * 10f);
        foreach(Button button in expandableButtons){
            button.GetComponent<CanvasGroup>().alpha = 0f;
            button.interactable = false;
        }
    }

    void OnDestroy()
    {
        foreach(Button button in expandableButtons){
            button.GetComponent<RectTransform>().DOKill();   
            button.GetComponent<CanvasGroup>().DOKill();    
        }
    }

    public void OpenExpandableButtonGroup()
    {
        canvas.GetComponent<GraphicRaycaster>().enabled = true; 
        if(expandableButtons.Count == 1){
            // 버튼 1개일때는 중앙 위쪽에 위치
            expandableButtons[0].interactable = true;
            expandableButtons[0].GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
            expandableButtons[0].transform.DOLocalMove(new Vector3(0f, radius, 0f), 0.5f);
        }else{
            // 버튼 여러개일 경우, 4사분면 의 부채꼴 호의 경로에 위치. 4사분면에서 3개를 기준으로 갯수가 늘어날수록 부채꼴의 각도가 늘어나, 호가 길어지도록 함.
            float startAngle = defaultStartAngle + ((expandableButtons.Count - defaultCount) * angleChangePerButton);
            float endAngle = defaultEndAngle - ((expandableButtons.Count - defaultCount) * angleChangePerButton);
            float angleIncrement = (startAngle - endAngle) / (expandableButtons.Count - 1); // 각 버튼 간의 각도(간격) 차이 계산
            for(int i = 0; i < expandableButtons.Count; i++){
                
                float angle = startAngle - angleIncrement * i; // 버튼 수에 따라 각 버튼의 각도 계산
                float angleRad = angle * Mathf.Deg2Rad; // 각도를 라디안으로 변환

                // 버튼의 위치 계산
                float x = radius * Mathf.Cos(angleRad);
                float y = radius * Mathf.Sin(angleRad);
                Vector3 expandPosition = new Vector3(x, y, 0f);

                expandableButtons[i].interactable = true;
                expandableButtons[i].GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
                expandableButtons[i].transform.DOLocalMove(expandPosition, 0.5f);
            }
        }
    }

    public void HideExpandableButtonGroup()
    {
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        foreach(Button button in expandableButtons){
            button.interactable = false;
            button.GetComponent<RectTransform>().DOLocalMove(Vector3.zero, 0.5f);
            button.GetComponent<CanvasGroup>().DOFade(0f, 0.5f);
        }
    }
}
