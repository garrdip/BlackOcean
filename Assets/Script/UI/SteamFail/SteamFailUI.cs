using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SteamFailUI : MonoBehaviour
{

    public Button terminateClientBtn;

    void Awake()
    {
        CreateClickBlocker();
    }

    void OnEnable()
    {
        // 다른 메뉴 UI(싱글플레이 버튼 등)보다 항상 위에 그려지도록 캔버스 맨 마지막 형제로 이동
        transform.SetAsLastSibling();
    }

    void Start()
    {
        terminateClientBtn.onClick.AddListener(() => HandleTerminateClient());
    }

    // 팝업 뒤 전체 화면을 덮는 반투명 차단 레이어 생성 — 팝업이 떠 있는 동안 다른 UI 클릭을 막는다.
    // (팝업 루트가 중앙 100x100의 작은 랙트라 앵커 스트레치로는 화면을 못 덮으므로 크기를 충분히 크게 잡는다)
    void CreateClickBlocker()
    {
        var go = new GameObject("ClickBlocker", typeof(RectTransform), typeof(Image));
        var rectTransform = (RectTransform)go.transform;
        rectTransform.SetParent(transform, false);
        rectTransform.SetAsFirstSibling(); // 팝업 내용(이미지·텍스트·버튼)보다 뒤에 그려지도록
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(30000f, 30000f); // 어떤 해상도·비율에서도 화면 전체를 덮도록
        var image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f); // 모달 팝업용 배경 딤. raycastTarget 기본값 true가 클릭을 차단한다
    }

    void HandleTerminateClient()
    {
        Application.Quit();
    }

}
