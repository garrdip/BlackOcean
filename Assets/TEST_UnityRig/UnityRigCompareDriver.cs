// Spine vs Unity 2D Animation 리그 비교 테스트 드라이버
// 숫자키 1~4: Idle / Attack0 / Buff0 / Defence0 동시 재생
using UnityEngine;

public class UnityRigCompareDriver : MonoBehaviour
{
    public Spine.Unity.SkeletonAnimation spine;
    public Animator unityRig;

    static readonly string[] Anims = { "Idle", "Attack0", "Buff0", "Defence0" };

    void Start()
    {
        Play(0);
    }

    void Update()
    {
        for (int i = 0; i < Anims.Length; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Play(i);
        // Walk는 유니티 리그 전용 (스파인 원본에는 없는 커스텀 애니메이션)
        if (Input.GetKeyDown(KeyCode.Alpha5) && unityRig != null)
            unityRig.CrossFade("Walk", 0.1f, 0, 0f);
    }

    public void Play(int index)
    {
        bool loop = index == 0;
        if (spine != null)
        {
            spine.AnimationState.SetAnimation(0, Anims[index], loop);
            if (!loop) spine.AnimationState.AddAnimation(0, "Idle", true, 0);
        }
        if (unityRig != null)
            unityRig.CrossFade(Anims[index], 0.05f, 0, 0f);
    }

    [ContextMenu("Play Walk")]
    public void PlayWalk()
    {
        if (unityRig != null)
            unityRig.CrossFade("Walk", 0.1f, 0, 0f);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 700, 30), "[1] Idle  [2] Attack0  [3] Buff0  [4] Defence0  [5] Walk(유니티만)   |   왼쪽: Spine  /  오른쪽: Unity 2D Animation");
    }
}
