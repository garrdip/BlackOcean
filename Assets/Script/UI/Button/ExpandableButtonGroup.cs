using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class ExpandableButtonGroup : MonoBehaviour
{
    public List<Button> expandableButtons = new List<Button>();
    private const float defaultStartAngle = 180f;
    private const float defaultEndAngle = 90f;
    private const float angleChangePerButton = 10f; // 버튼 갯수당 추가 및 감소 될 각도 비율값
    private const int defaultCount = 3; // 기본 버튼 갯수
    private float radius;


    void Start()
    {
        radius = 160f + (expandableButtons.Count * 10f);
        foreach(Button button in expandableButtons){
            button.GetComponent<CanvasGroup>().alpha = 0f;
        }
    }

    public void OpenExpandableButtonGroup()
    {
        float startAngle = defaultStartAngle + (expandableButtons.Count - defaultCount) * angleChangePerButton;
        float endAngle = defaultEndAngle - (expandableButtons.Count - defaultCount) * angleChangePerButton;
        float angleIncrement = (startAngle - endAngle) / (expandableButtons.Count - 1); // 각 버튼 간의 각도(간격) 차이 계산
        for(int i = 0; i < expandableButtons.Count; i++){
            float angle = startAngle - angleIncrement * i; // 버튼 수에 따라 각 버튼의 각도 계산
            float angleRad = angle * Mathf.Deg2Rad; // 각도를 라디안으로 변환

            // 버튼의 위치 계산
            float x = radius * Mathf.Cos(angleRad);
            float y = radius * Mathf.Sin(angleRad);
            Vector3 expandPosition = new Vector3(x, y, 0);

            expandableButtons[i].GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
            expandableButtons[i].transform.DOLocalMove(expandPosition, 0.5f);
        }
    }

    public void HideExpandableButtonGroup()
    {
        foreach(Button button in expandableButtons){
            button.GetComponent<RectTransform>().DOLocalMove(Vector3.zero, 0.5f);
            button.GetComponent<CanvasGroup>().DOFade(0f, 0.5f);
        }
    }
}
