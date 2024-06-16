using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardQueueUI : MonoBehaviour
{
    public ScrollRect scrollRect;
    public ScrollButtonDirection direction;
    public float scrollSpeed;
    private bool isPointerDown = false;
    public Button leftScrollButton;
    public Button rightScrollButton;

    public enum ScrollButtonDirection
    {
        NONE,
        LEFT,
        RIGHT
    }

    void Start()
    {
        scrollSpeed = 500f;
    }

    void Update()
    {
        UpdateScrollButtonVisibility();
        HandleScrollViewByScrollButton();   
    }

    // 버튼으로 스크롤 뷰 제어
    private void HandleScrollViewByScrollButton()
    {
        if(isPointerDown){
            float contentWidth = scrollRect.content.rect.width;
            float viewportWidth = scrollRect.viewport.rect.width;
            float maxScrollWidth = contentWidth - viewportWidth;
            if(maxScrollWidth <= 0){
                return;
            }
            float scrollAmount = (scrollSpeed / maxScrollWidth) * Time.deltaTime;
            if(direction == ScrollButtonDirection.LEFT){
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - scrollAmount);
            }else{
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + scrollAmount);
            }
        }
    }

    // 스크롤 뷰 내부 컨텐츠요소의 길이에 따라 스크롤 버튼의 활성화 상태 변경
    private void UpdateScrollButtonVisibility()
    {
        float contentWidth = scrollRect.content.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;

        if(contentWidth <= viewportWidth){
            leftScrollButton.gameObject.SetActive(false);
            rightScrollButton.gameObject.SetActive(false);
        }else{
            if(scrollRect.horizontalNormalizedPosition <= 0){
                leftScrollButton.gameObject.SetActive(false); // 스크롤바가 왼쪽 끝에 있는 경우 왼쪽 버튼 비활성화
            }else{
                leftScrollButton.gameObject.SetActive(true);
            }
            if(scrollRect.horizontalNormalizedPosition >= 1){
                rightScrollButton.gameObject.SetActive(false); // 스크롤바가 오른쪽 끝에 있는 경우 오른쪽 버튼 비활성화
            }else{
                rightScrollButton.gameObject.SetActive(true);
            }
        }
    }

    // ------------------------------------ 스크롤 버튼 이벤트 트리거 컴포넌트에 할당된 함수 -------------------------------------//
    public void OnPointerDownLeftScrollButton()
    {
        isPointerDown = true;
        direction = ScrollButtonDirection.LEFT;
    }

    public void OnPointerUpLeftScrollButton()
    {
        isPointerDown = false;
        direction = ScrollButtonDirection.NONE;
    }

    public void OnPointerDownRightScrollButton()
    {
        isPointerDown = true;
        direction = ScrollButtonDirection.RIGHT;
    }

    public void OnPointerUpRightScrollButton()
    {
        isPointerDown = false;
        direction = ScrollButtonDirection.NONE;
    }
}
