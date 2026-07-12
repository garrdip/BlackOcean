using System.Collections.Generic;
using UnityEngine;

namespace ProjectD
{
    /// <summary>
    /// OnGUI로 그리는 테스트 버튼 영역을 등록해 두고, 구체 맵 입력(SphereMapView)이
    /// 해당 영역에서 시작된 클릭을 무시할 수 있게 하는 헬퍼.
    /// (IMGUI는 EventSystem을 쓰지 않아 IsPointerOverGameObject로 감지되지 않기 때문)
    /// </summary>
    public static class Map3DGuiArea
    {
        static readonly List<Rect> s_rects = new List<Rect>();
        static int s_frame = -1;

        /// <summary>OnGUI에서 버튼을 그릴 때마다 해당 Rect를 등록한다. (GUI 좌표계, 매 프레임 초기화)</summary>
        public static void Register(Rect rect)
        {
            if (Time.frameCount != s_frame)
            {
                s_frame = Time.frameCount;
                s_rects.Clear();
            }
            s_rects.Add(rect);
        }

        /// <summary>Input.mousePosition(좌하단 원점)이 등록된 버튼 영역 위인지 검사</summary>
        public static bool Contains(Vector2 mousePosition)
        {
            // OnGUI는 Update보다 늦게 돌기 때문에 직전 프레임에 등록된 rect도 유효로 취급
            if (Time.frameCount - s_frame > 1)
                return false;
            var guiPos = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            foreach (Rect rect in s_rects)
            {
                if (rect.Contains(guiPos))
                    return true;
            }
            return false;
        }
    }
}
