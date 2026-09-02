// The SteamManager is designed to work with Steamworks.NET
// This file is released into the public domain.
// Where that dedication is not recognized you are granted a perpetual,
// irrevocable license to copy and modify this file as you see fit.
//
// Version: 1.0.13

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using System.Collections;
using Steamworks;
#endif

//
// The SteamManager provides a base implementation of Steamworks.NET on which you can build upon.
// It handles the basics of starting up and shutting down the SteamAPI for use.
//
[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour {
#if !DISABLESTEAMWORKS
	protected static bool s_EverInitialized = false;

	protected static SteamManager s_instance;
	protected static SteamManager Instance {
		get {
			if (s_instance == null) {
				return new GameObject("SteamManager").AddComponent<SteamManager>();
			}
			else {
				return s_instance;
			}
		}
	}

	protected bool m_bInitialized = false;
	public static bool Initialized {
		get {
			return Instance.m_bInitialized;
		}
	}

	// 인스턴스를 자동 생성하지 않는 초기화 확인.
	// Initialized는 s_instance가 없으면 새 SteamManager를 만들어 버려, 씬에 배치된 SteamManager가
	// 나중에 Awake될 때 중복으로 판정·파괴되는 부작용이 있다. 씬 로드 초기(다른 Awake)에서는 이것을 쓸 것.
	public static bool InitializedSafe {
		get {
			return s_instance != null && s_instance.m_bInitialized;
		}
	}

	protected SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;

	[AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
	protected static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText) {
		Debug.LogWarning(pchDebugText);
	}

#if UNITY_2019_3_OR_NEWER
	// In case of disabled Domain Reload, reset static members before entering Play Mode.
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void InitOnPlayMode()
	{
		s_EverInitialized = false;
		s_instance = null;
	}
#endif

	// BlackOcean Steam 앱 ID — 프로젝트 루트 steam_appid.txt(에디터/개발용)와 같은 값. 빌드 전 Assets/Editor/SteamAppIdBuildCheck가 일치 여부를 검사한다.
	// 배포 빌드에는 steam_appid.txt를 넣지 않는다(Valve 지침). 대신 Init 전에 SteamAppId/SteamGameId 환경 변수로 앱 ID를 주입한다 —
	// steam_api.dll이 steam_appid.txt를 읽어 만드는 것과 같은 변수라 파일 없이도 Init이 앱 ID를 찾고, RestartAppIfNecessary도 파일이 있을 때처럼 false를 돌려준다 (Facepunch.Steamworks SteamClient.Init과 같은 방식, 2026-09-02).
	public const uint AppId = 2359700;

	static void ApplyEmbeddedAppId() {
		System.Environment.SetEnvironmentVariable("SteamAppId", AppId.ToString());
		System.Environment.SetEnvironmentVariable("SteamGameId", AppId.ToString());
	}

	protected virtual void Awake() {
		// Only one instance of SteamManager at a time!
		if (s_instance != null) {
			// 같은 게임오브젝트에 다른 컴포넌트(M_SteamManager 등)가 함께 붙어 있을 수 있으므로
			// 게임오브젝트가 아니라 중복된 이 컴포넌트만 파괴한다
			Destroy(this);
			return;
		}
		s_instance = this;

		if(s_EverInitialized) {
			// This is almost always an error.
			// The most common case where this happens is when SteamManager gets destroyed because of Application.Quit(),
			// and then some Steamworks code in some other OnDestroy gets called afterwards, creating a new SteamManager.
			// You should never call Steamworks functions in OnDestroy, always prefer OnDisable if possible.
			throw new System.Exception("Tried to Initialize the SteamAPI twice in one session!");
		}

		// We want our SteamManager Instance to persist across scenes.
		DontDestroyOnLoad(gameObject);

		if (!Packsize.Test()) {
			Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", this);
		}

		if (!DllCheck.Test()) {
			Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", this);
		}

		ApplyEmbeddedAppId(); // steam_appid.txt 없이 앱 ID 내장 — RestartAppIfNecessary/Init보다 먼저

		try {
			// If Steam is not running or the game wasn't started through Steam, SteamAPI_RestartAppIfNecessary starts the
			// Steam client and also launches this game again if the User owns it. This can act as a rudimentary form of DRM.
			// Note that this will run which ever version you have installed in steam. Which may not be the precise executable
			// we were currently running.

			// Once you get a Steam AppID assigned by Valve, you need to replace AppId_t.Invalid with it and
			// remove steam_appid.txt from the game depot. eg: "(AppId_t)480" or "new AppId_t(480)".
			// See the Valve documentation for more information: https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
			if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid)) {
				Debug.Log("[Steamworks.NET] Shutting down because RestartAppIfNecessary returned true. Steam will restart the application.");

				Application.Quit();
				return;
			}
		}
		catch (System.DllNotFoundException e) { // We catch this exception here, as it will be the first occurrence of it.
			Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e, this);

			Application.Quit();
			return;
		}

		// Initializes the Steamworks API.
		// If this returns false then this indicates one of the following conditions:
		// [*] The Steam client isn't running. A running Steam client is required to provide implementations of the various Steamworks interfaces.
		// [*] The Steam client couldn't determine the App ID of game. If you're running your application from the executable or debugger directly then you must have a [code-inline]steam_appid.txt[/code-inline] in your game directory next to the executable, with your app ID in it and nothing else. Steam will look for this file in the current working directory. If you are running your executable from a different directory you may need to relocate the [code-inline]steam_appid.txt[/code-inline] file.
		// [*] Your application is not running under the same OS user context as the Steam client, such as a different user or administration access level.
		// [*] Ensure that you own a license for the App ID on the currently active Steam account. Your game must show up in your Steam library.
		// [*] Your App ID is not completely set up, i.e. in Release State: Unavailable, or it's missing default packages.
		// Valve's documentation for this is located here:
		// https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
		m_bInitialized = SteamAPI.Init();
		if (!m_bInitialized) {
			Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.", this);

			return;
		}

		s_EverInitialized = true;
	}

	// This should only ever get called on first load and after an Assembly reload, You should never Disable the Steamworks Manager yourself.
	protected virtual void OnEnable() {
		if (s_instance == null) {
			s_instance = this;
		}

		if (!m_bInitialized) {
			return;
		}

		if (m_SteamAPIWarningMessageHook == null) {
			// Set up our callback to receive warning messages from Steam.
			// You must launch with "-debug_steamapi" in the launch args to receive warnings.
			m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
			SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
		}
	}

	// OnApplicationQuit gets called too early to shutdown the SteamAPI.
	// Because the SteamManager should be persistent and never disabled or destroyed we can shutdown the SteamAPI here.
	// Thus it is not recommended to perform any Steamworks work in other OnDestroy functions as the order of execution can not be garenteed upon Shutdown. Prefer OnDisable().
	protected virtual void OnDestroy() {
		if (s_instance != this) {
			return;
		}

		s_instance = null;

		if (!m_bInitialized) {
			return;
		}

		SteamAPI.Shutdown();
	}

	protected virtual void Update() {
		if (!m_bInitialized) {
			return;
		}

		// Run Steam client callbacks
		SteamAPI.RunCallbacks();
	}
#else
	public static bool Initialized {
		get {
			return false;
		}
	}

	public static bool InitializedSafe {
		get {
			return false;
		}
	}
#endif // !DISABLESTEAMWORKS
}
