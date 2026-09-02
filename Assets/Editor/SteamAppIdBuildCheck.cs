using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 빌드 전 검사 — 프로젝트 루트 steam_appid.txt(에디터/개발용)와 SteamManager.AppId(빌드에 내장되는 값)가 다르면 빌드를 중단한다.
// 배포 빌드에는 steam_appid.txt를 넣지 않는다 (Valve 지침). 앱 ID는 SteamManager가 SteamAppId/SteamGameId 환경 변수로 주입한다 (2026-09-02).
public class SteamAppIdBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "steam_appid.txt");
        if (!File.Exists(path)) return; // 파일이 없으면 내장 값만 쓰인다
        string text = File.ReadAllText(path).Trim();
        if (uint.TryParse(text, out uint fileAppId) && fileAppId == SteamManager.AppId) return;
        throw new BuildFailedException($"[SteamAppIdBuildCheck] steam_appid.txt({text})와 SteamManager.AppId({SteamManager.AppId})가 다릅니다. 두 값을 맞춘 뒤 다시 빌드하세요.");
    }
}
