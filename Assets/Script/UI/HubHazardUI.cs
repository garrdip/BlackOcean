using TMPro;
using UnityEngine;

/// <summary>
/// 거점 우상단 위험도 표시 UI (위험도 시스템) — 옛 육각형 맵 시절의 MapDangerLayout(GameCanvas/MapInfo 잔존분)을 복제해 재사용한다.
/// Hub 루트 아래(HubCanvas)에 있어 거점 화면에서만 보이며, M_HubManager.hazardLevel(SyncVar)을 표시한다.
/// </summary>
public class HubHazardUI : MonoBehaviour
{
    [Header("위험도 숫자 텍스트 (TextDangerGage)")]
    public TextMeshProUGUI hazardValueText;

    void Update()
    {
        if(M_HubManager.instance == null || hazardValueText == null) return;
        hazardValueText.text = M_HubManager.instance.hazardLevel.ToString();
    }
}
