using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using AYellowpaper.SerializedCollections;
using ProjectD;


/*  
    [사운드 매니저 v1.05.0 made by JJSmith (Curookie)]

    [사용법]
    1) BGM, VOICE, SFX 타입에 따라 해당 Dictionary에 저장

    2) 재생방법 (반복재생, OneShot 등 지원)
        - BGM
        AudioClip bgmClip = M_SoundManager.instance.bgmClips[BGM_TYPE.타입이름].Find((audioClip) => audioClip.name.Equals("클립이름"));
        M_SoundManager.instance.PlayBGM(bgmClip, MusicTransition.CrossFade, 2f);
        - VOICE
        AudioClip voiceClip = M_SoundManager.instance.bgmClips[VOICE_TYPE.타입이름].Find((audioClip) => audioClip.name.Equals("클립이름"));
        M_SoundManager.instance.PlayVoice(voiceClip, voiceClip.length);
        - SFX
        AudioClip sfxClip = M_SoundManager.instance.bgmClips[SFX_TYPE.타입이름].Find((audioClip) => audioClip.name.Equals("클립이름"));
        M_SoundManager.instance.PlaySFX(sfxClip, sfxClip.length);

    3) 배경음 재생모드 
        - 페이드 없음(Swift) (playback 쓰려면 Swift로 해야함)
        - 페이드 아웃/인(LinearFade)
        - 크로스 페이드(CrossFade) 
    
    4) PlayerPrefabs으로 설정을 저장하며 토글 속성 있다. ex)IsMusicOn 배경음, IsSoundOn 효과음";
*/


[RequireComponent (typeof (AudioSource))]
public class M_SoundManager : MonoBehaviour {

    [Header ("BGM 목록")]
    [SerializedDictionary("BGM_TYPE", "BGM 오디오 클립 목록")]
    public SerializedDictionary<BGM_TYPE, List<AudioClip>> bgmClips = new SerializedDictionary<BGM_TYPE, List<AudioClip>>();

    [Header ("SFX 목록")]
    [SerializedDictionary("SFX_TYPE", "SFX 오디오 클립 목록")]
    public SerializedDictionary<string, List<AudioClip>> sfxClips = new SerializedDictionary<string, List<AudioClip>>();

    [Header ("VOICE 목록")]
    [SerializedDictionary("VOICE_TYPE", "VOICE 오디오 클립 목록")]
    public SerializedDictionary<VOICE_TYPE, List<AudioClip>> voiceClips = new SerializedDictionary<VOICE_TYPE, List<AudioClip>>();

    [Header ("BGM 설정")]
    [Tooltip ("BGM On/Off")]
    [SerializeField] bool _musicOn = true;

    [Tooltip ("BGM 볼륨")]
    [Range (0, 1)]
    [SerializeField] float _musicVolume = 1f;

    [Tooltip ("시작 시 BGM 사용여부")]
    [SerializeField] bool _useMusicVolOnStart = false;

    [Tooltip ("Target Group BGM 신호를 위한 설정, 사용 안 할경우 비워놓으면 됨.")]
    [SerializeField] AudioMixerGroup _musicMixerGroup = null;

    [Tooltip ("BGM 볼륨믹서 명")]
    [SerializeField] string _volumeOfMusicMixer = string.Empty;

    [Space (3)]

    // 효과음 오브젝트 풀
    List<SoundEffect> sfxPool = new List<SoundEffect> ();

    [Header ("SFX 설정")]
    [Tooltip ("SFX On/Off")]
    [SerializeField] bool _soundFxOn = true;

    [Tooltip ("SFX 볼륨")]
    [Range (0, 1)]
    [SerializeField] float _soundFxVolume = 1f;

    [Tooltip ("시작 시 SFX 사용여부")]
    [SerializeField] bool _useSfxVolOnStart = false;

    [Tooltip ("Target Group SFX 신호를 위한 설정, 사용 안 할경우 비워놓으면 됨.")]
    [SerializeField] AudioMixerGroup _soundFxMixerGroup = null;

    [Tooltip ("SFX 볼륨믹서 명")]
    [SerializeField] string _volumeOfSFXMixer = string.Empty;

    [Space (3)]

    // 음성 오브젝트 풀
    List<VoiceEffect> voicePool = new List<VoiceEffect> ();

    [Header ("VOICE 설정")]
    [Tooltip ("VOICE On/Off")]
    [SerializeField] bool _voiceOn = true;
    
    [Tooltip ("VOICE 볼륨")]
    [Range (0, 1)]
    [SerializeField] float _voiceVolume = 1f;
      
    [Tooltip ("시작 시 VOICE 사용여부")]
    [SerializeField] bool _useVoiceVolOnStart = false;

    [Tooltip ("Target Group VOICE 신호를 위한 설정, 사용 안 할경우 비워놓으면 됨.")]
    [SerializeField] AudioMixerGroup _voiceMixerGroup = null;

    [Tooltip ("VOICE 볼륨믹서 명")]
    [SerializeField] string _volumeOfVoiceMixer = string.Empty;

    // 오디오 매니저 배경음
    static BackgroundMusic backgroundMusic;
    // 현재 오디오소스와 페이드를 위한 다음 오디오소스
    static AudioSource musicSource = null, crossfadeSource = null;
    // 현재 볼륨들과 제한 수치용 변수
    static float currentMusicVol = 0, currentSfxVol = 0, currentVoiceVol = 0, musicVolCap = 0, savedPitch = 1f;
    // On/Off 변수
    static bool musicOn = false, sfxOn = false, voiceOn = false;
    // 전환시간 변수
    static float transitionTime;

    // PlayerPrefabs 저장을 위한 키
    static readonly string BgMusicVolKey = "BGMVol";
    static readonly string SoundFxVolKey = "SFXVol";
    static readonly string VoiceVolKey = "VOICEVol";
    static readonly string BgMusicMuteKey = "BGMMute";
    static readonly string SoundFxMuteKey = "SFXMute";
    static readonly string VoiceMuteKey = "VOICEMute";

    // 유일한 인스턴스 변수
    private static M_SoundManager Instance;
    // 앱 켜졌는지 여부용
    private static bool alive = true;

    /// <summary>
    /// 속성 싱글톤 패턴으로 구현
    /// </summary>
    public static M_SoundManager instance {
        get {
            // 앱이 꺼젔거나 Destroy됬는지 체크
            if (!alive) {
                Debug.LogWarning (typeof (M_SoundManager) + "' is already destroyed on application quit.");
                return null;
            }

            //C# 2.0 Null 병합연산자
            return Instance ?? FindObjectOfType<M_SoundManager> ();
        }
    }

    void OnDestroy () {
        StopAllCoroutines ();
        SaveAllPreferences ();
    }

    void OnApplicationExit () {
        alive = false;
    }

    /// <summary>
    /// 오디오매니저 초기화 함수
    /// </summary>
    void Initialise () {
        gameObject.name = "M_SoundManager";

        // PlayerPrefs에서 값 가져오기
        _musicOn = LoadBGMMuteStatus ();
        _musicVolume = _useMusicVolOnStart ? _musicVolume : LoadBGMVolume ();
        _soundFxOn = LoadSFXMuteStatus ();
        _soundFxVolume = _useSfxVolOnStart ? _soundFxVolume : LoadSFXVolume ();
        _voiceOn = LoadVOICEMuteStatus ();
        _voiceVolume = _useVoiceVolOnStart ? _voiceVolume : LoadVOICEVolume ();

        // 기존 오디오소스 컴포넌트 장착
        if (musicSource == null) {
            musicSource = gameObject.GetComponent<AudioSource> ();
            // 오디오소스 컴포넌트 없으면 생성해서 부착
            musicSource = musicSource ?? gameObject.AddComponent<AudioSource> ();
        }

        musicSource = ConfigureAudioSource (musicSource);

        // 씬 전환시에도 파괴되지 않도록 설정
        DontDestroyOnLoad (this.gameObject);
    }

    void Awake () {
        if (Instance == null) {
            Instance = this;
            Initialise ();
        } else if (Instance != this) {
            Destroy (this.gameObject);
        }
        SceneManager.activeSceneChanged += OnChangedActiveScene;
    }

    void Start () {
        if (musicSource != null) {
            StartCoroutine (OnUpdate ());
        }
    }

    /// <summary>
    /// 업데이트 함수 용 Enumerator
    /// </summary>
    IEnumerator OnUpdate(){
        while (alive) {
            ManageSoundEffects ();
            ManageVoiceEffects();

            // 배경음 볼륨 바뀌었나 체크
            if(IsMusicAltered ()){
                //ToggleBGMMute (!musicOn);

                if(!FloatEquals (currentMusicVol, _musicVolume)){
                    currentMusicVol = _musicVolume;
                }
                if(_musicMixerGroup != null && !string.IsNullOrEmpty (_volumeOfMusicMixer)) {
                    float vol;
                    _musicMixerGroup.audioMixer.GetFloat (_volumeOfMusicMixer, out vol);
                    vol = NormaliseVolume (vol);
                    currentMusicVol = vol;
                }
                SetBGMVolume (currentMusicVol);
            }

            // 효과음 볼륨 바뀌었나 체크
            if(IsSoundFxAltered ()){
                //ToggleSFXMute (!sfxOn);
                if(!FloatEquals (currentSfxVol, _soundFxVolume)){
                    currentSfxVol = _soundFxVolume;
                }
                if(_soundFxMixerGroup != null && !string.IsNullOrEmpty (_volumeOfSFXMixer)){
                    float vol;
                    _soundFxMixerGroup.audioMixer.GetFloat (_volumeOfSFXMixer, out vol);
                    vol = NormaliseVolume (vol);
                    currentSfxVol = vol;
                }
                SetSFXVolume (currentSfxVol);
            }

            // 음성 볼륨 바뀌었나 체크
            if (IsVoiceAltered ()) {
                //ToggleVoiceMute (!voiceOn);
                if(!FloatEquals (currentVoiceVol, _voiceVolume)){
                    currentVoiceVol = _voiceVolume;
                }
                if(_voiceMixerGroup != null && !string.IsNullOrEmpty (_volumeOfVoiceMixer)){
                    float vol;
                    _voiceMixerGroup.audioMixer.GetFloat (_volumeOfVoiceMixer, out vol);
                    vol = NormaliseVolume (vol);
                    currentVoiceVol = vol;
                }
                SetVOICEVolume (currentVoiceVol);
            }

            // 크로스 페이드일 경우
            if (crossfadeSource != null) {
                CrossFadeBackgroundMusic ();
                yield return null;
            } else {
                // 페이드 인/ 아웃일 경우
                if (backgroundMusic.NextClip != null) {
                    FadeOutFadeInBackgroundMusic ();
                    yield return null;
                }
            }
            yield return new WaitForEndOfFrame ();
        }
    }

    /// <summary>
    /// 씬 전환 시 해당 오디오 배경음 재생
    /// </summary>
    private void OnChangedActiveScene(Scene current, Scene next)
    {
        switch(next.name){
            case "MenuScene":
                AudioClip mainTitleClip = bgmClips[BGM_TYPE.MainTitle].Find((audioClip) => audioClip.name.Equals("MainTitle"));
                PlayBGM(mainTitleClip, MusicTransition.Swift);
                break;
            case "GameScene":
                AudioClip mapClip = bgmClips[BGM_TYPE.Map].Find((audioClip) => audioClip.name.Equals("Stage_1_Map"));
                PlayBGM(mapClip, MusicTransition.CrossFade, 2f);
                break;
        }
    }

    // 선택한 캐릭터의 선택 음성 count 만큼 조회
    public List<AudioClip> GetCharacterVoiceClips(Character character, int startIndex, int count)
    {
        List<AudioClip> clips = new List<AudioClip>();
        switch(character){
            case Character.GEORK:
                for(int i=startIndex; i<(startIndex + count); i++){
                    AudioClip audioClip = M_SoundManager.instance.voiceClips[VOICE_TYPE.Geork][i];
                    clips.Add(audioClip);
                }
                break;
            case Character.ERIS:
                for(int i=startIndex; i<(startIndex + count); i++){
                    AudioClip audioClip = M_SoundManager.instance.voiceClips[VOICE_TYPE.Eris][i];
                    clips.Add(audioClip);
                }
                break;
            case Character.HONGDANHYANG:
                for(int i=startIndex; i<(startIndex + count); i++){
                    AudioClip audioClip = M_SoundManager.instance.voiceClips[VOICE_TYPE.HongDanHyang][i];
                    clips.Add(audioClip);
                }
                break;
        }
        return clips;
    }

    /// <summary>
    /// 내부 설정에 기반해서 2D용 오디오소스 생성하는 함수
    /// </summary>
    /// <returns>An AudioSource with 2D features</returns>
    AudioSource ConfigureAudioSource (AudioSource audioSource) {
        audioSource.outputAudioMixerGroup = _musicMixerGroup;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;   //2D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.loop = true;
        // PlayerPrefs에서 값 가져오기
        audioSource.volume = LoadBGMVolume ();
        audioSource.mute = !_musicOn;
        

        return audioSource;
    }

    bool FloatEquals (float num1, float num2, float threshold = .0001f) {
        return Math.Abs (num1 - num2) < threshold;
    }


    #region BGM
    /// <summary>
    /// 배경음 볼륨 상태가 변했는지 체크하는 함수
    /// </summary>
    private bool IsMusicAltered () {
        bool flag = musicOn != _musicOn || musicOn != !musicSource.mute || !FloatEquals (currentMusicVol, _musicVolume);

        // 믹서 그룹을 사용할 경우
        if (_musicMixerGroup != null && !string.IsNullOrEmpty (_volumeOfMusicMixer.Trim ())) {
            float vol;
            _musicMixerGroup.audioMixer.GetFloat (_volumeOfMusicMixer, out vol);
            vol = NormaliseVolume (vol);

            return flag || !FloatEquals (currentMusicVol, vol);
        }

        return flag;
    }

    /// <summary>
    /// 크로스 페이드 인 아웃 함수
    /// </summary>
    private void CrossFadeBackgroundMusic () {
        if (backgroundMusic.MusicTransition == MusicTransition.CrossFade) {
            // 전환이 진행중일 경우
            if (musicSource.clip.name != backgroundMusic.NextClip.name) {
                transitionTime -= Time.deltaTime;

                musicSource.volume = Mathf.Lerp (0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

                crossfadeSource.volume = Mathf.Clamp01 (musicVolCap - musicSource.volume);
                crossfadeSource.mute = musicSource.mute;

                if (musicSource.volume <= 0.00f) {
                    SetBGMVolume (musicVolCap);
                    PlayBackgroundMusic (backgroundMusic.NextClip, crossfadeSource.time, crossfadeSource.pitch);
                }
            }
        }
    }

    /// <summary>
    /// 페이드 인/아웃 함수
    /// </summary>
    private void FadeOutFadeInBackgroundMusic () {
        if (backgroundMusic.MusicTransition == MusicTransition.LinearFade) {
            // 페이드 인
            if (musicSource.clip.name == backgroundMusic.NextClip.name) {
                transitionTime += Time.deltaTime;

                musicSource.volume = Mathf.Lerp (0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

                if (musicSource.volume >= musicVolCap) {
                    SetBGMVolume (musicVolCap);
                    PlayBackgroundMusic (backgroundMusic.NextClip, musicSource.time, savedPitch);
                }
            }
            // 페이드 아웃
            else {
                transitionTime -= Time.deltaTime;

                musicSource.volume = Mathf.Lerp (0, musicVolCap, transitionTime / backgroundMusic.TransitionDuration);

                // 페이드 아웃 끝나는 시점 페이드 인 시작
                if (musicSource.volume <= 0.00f) {
                    musicSource.volume = transitionTime = 0;
                    PlayMusicFromSource (ref musicSource, backgroundMusic.NextClip, 0, musicSource.pitch);
                }
            }
        }
    }

    /// <summary>
    /// 특정한 오디오소스에서 클립을 재생하는 함수   
    /// </summary>
    /// <param name="audio_source">참조하는 오디오소스/ 채널</param>
    /// <param name="clip">재생할 클립</param>
    /// <param name="playback_position">시작시점</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    private void PlayMusicFromSource (ref AudioSource audio_source, AudioClip clip, float playback_position, float pitch) {
        try {
            audio_source.clip = clip;
            audio_source.time = playback_position;
            audio_source.pitch = pitch = Mathf.Clamp (pitch, -3f, 3f);
            audio_source.Play ();
        } catch (NullReferenceException nre) {
            Debug.LogError (nre.Message);
        } catch (Exception e) {
            Debug.LogError (e.Message);
        }
    }

    /// <summary>
    /// 현재 오디오소스에서 배경음 클립을 재생하는 함수 (내장함수)
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="playback_position">시작시점</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    private void PlayBackgroundMusic (AudioClip clip, float playback_position, float pitch) {
        PlayMusicFromSource (ref musicSource, clip, playback_position, pitch);
        // 다음 클립변수에 있는 클립 제거
        backgroundMusic.NextClip = null;
        // 현재 클립변수에 넣어두기
        backgroundMusic.CurrentClip = clip;
        // 크로스페이드에 있는 클립도 비우기
        if (crossfadeSource != null) {
            Destroy (crossfadeSource);
            crossfadeSource = null;
        }
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법 </param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="playback_position">시작시점</param>
    public void PlayAudioClipBGM (AudioClip clip, MusicTransition transition, float transition_duration, float volume, float pitch, float playback_position = 0) {
        // 요구클립이 없거나 똑같은 클립이면 재생하지 않음.
        if (clip == null || ( backgroundMusic.CurrentClip != null && backgroundMusic.CurrentClip.name.Equals(clip))) {
            return;
        }

        // 첫 번째로 플레이한 음악이거나 전환시간이 0이면 - 전환효과 없는 케이스
        if (backgroundMusic.CurrentClip == null || transition_duration <= 0) {
            transition = MusicTransition.Swift;
        }

        // 전환효과 없는 케이스 시작
        if (transition == MusicTransition.Swift) {
            PlayBackgroundMusic (clip, playback_position, pitch);
            SetBGMVolume (volume);
        } else {
            // 전환효과 진행중일 때 막음
            if (backgroundMusic.NextClip != null) {
                Debug.LogWarning ("Trying to perform a transition on the background music while one is still active");
                return;
            }

            // 전환효과 변수에 전환방법대로 지정, 그 외 변수들도..
            backgroundMusic.MusicTransition = transition;
            transitionTime = backgroundMusic.TransitionDuration = transition_duration;
            musicVolCap = _musicVolume;
            backgroundMusic.NextClip = clip;

            // 크로스페이드 처리
            if (backgroundMusic.MusicTransition == MusicTransition.CrossFade) {
                // 전환효과 진행중일 때 막음
                if (crossfadeSource != null) {
                    Debug.LogWarning ("Trying to perform a transition on the background music while one is still active");
                    return;
                }

                // 크로스페이드 오디오 초기화
                crossfadeSource = ConfigureAudioSource (gameObject.AddComponent<AudioSource> ());

                crossfadeSource.volume = Mathf.Clamp01 (musicVolCap - currentMusicVol);
                crossfadeSource.priority = 0;

                PlayMusicFromSource (ref crossfadeSource, backgroundMusic.NextClip, 0, pitch);
            }
        }
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    /// <param name="transition_duration">전환시간</param>
    /// <param name="volume">사운드 크기</param>
    public void PlayBGM (AudioClip clip, MusicTransition transition, float transition_duration, float volume) {
        PlayAudioClipBGM (clip, transition, transition_duration, volume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    /// <param name="transition_duration">전환시간</param>
    public void PlayBGM (AudioClip clip, MusicTransition transition, float transition_duration) {
        PlayAudioClipBGM (clip, transition, transition_duration, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="transition">전환방법</param>
    public void PlayBGM (AudioClip clip, MusicTransition transition) {
        PlayAudioClipBGM (clip, transition, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 바로 재생
    /// 배경음은 한 번에 한 개만 재생.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    public void PlayBGM (AudioClip clip) {
        PlayAudioClipBGM (clip, MusicTransition.Swift, 1f, _musicVolume, 1f);
    }

    /// <summary>
    /// 배경음 중지
    /// </summary>
    public void StopBGM () {
        if (musicSource.isPlaying) {
            musicSource.Stop ();
        }
    }

    /// <summary>
    /// 배경음 일시정지
    /// </summary>
    public void PauseBGM () {
        if (musicSource.isPlaying) {
            musicSource.Pause ();
        }
    }

    /// <summary>
    /// 배경음 다시재생
    /// </summary>
    public void ResumeBGM () {
        if (!musicSource.isPlaying) {
            musicSource.UnPause ();
        }
    }
    #endregion

    #region SFX
    /// <summary>
    /// SFX Pool에 있는 효과음을 관리하는 함수  
    /// OnUpdate함수에서 불러온다.
    /// </summary>
    private void ManageSoundEffects () {
        for (int i = sfxPool.Count - 1; i >= 0; i--) {
            SoundEffect sfx = sfxPool[i];
            // 재생 중
            if (sfx.Source.isPlaying && !float.IsPositiveInfinity (sfx.Time)) {
                sfx.Time -= Time.deltaTime;
                sfxPool[i] = sfx;
            }
            
            // 끝났을 때
            if (sfxPool[i].Time <= 0.0001f || HasPossiblyFinished (sfxPool[i])) {
                sfxPool[i].Source.Stop ();
                // 콜백함수 실행
                if (sfxPool[i].Callback != null) {
                    sfxPool[i].Callback.Invoke ();
                }

                // 클립 제거 후
                Destroy (sfxPool[i].gameObject);

                // 풀에서 항목빼기
                sfxPool.RemoveAt (i);
                break;
            }
        }
    }

    // 효과음 완전히 끝났는 지 체크용 함수
    bool HasPossiblyFinished (SoundEffect soundEffect) {
        return !soundEffect.Source.isPlaying && FloatEquals (soundEffect.PlaybackPosition, 0) && soundEffect.Time <= 0.09f;
    }

    /// <summary>
    /// 효과음 볼륨 상태가 변했는지 체크하는 함수
    /// </summary>
    private bool IsSoundFxAltered () {
        bool flag = _soundFxOn != sfxOn || !FloatEquals (currentSfxVol, _soundFxVolume);

        // 믹서 그룹을 사용할 경우
        if (_soundFxMixerGroup != null && !string.IsNullOrEmpty (_volumeOfSFXMixer.Trim ())) {
            float vol;
            _soundFxMixerGroup.audioMixer.GetFloat (_volumeOfSFXMixer, out vol);
            vol = NormaliseVolume (vol);

            return flag || !FloatEquals (currentSfxVol, vol);
        }

        return flag;
    }

    /// <summary>
    /// 모든 효과음에서 사용되는 내장 기본함수
    /// 효과음에 대한 특정 항목을 초기화함.
    /// </summary>
    /// <param name="audio_clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <returns>Newly created gameobject with sound effect and audio source attached</returns>
    private GameObject CreateSoundFx (AudioClip audio_clip, Vector2 location) {
        // 임시 오브젝트
        GameObject host = new GameObject ("TempSFX");
        host.transform.position = location;
        host.transform.SetParent (transform);
        host.AddComponent<SoundEffect> ();

        // 오디오소스 추가
        AudioSource audioSource = host.AddComponent<AudioSource> () as AudioSource;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // 믹서 그룹을 사용할 경우
        audioSource.outputAudioMixerGroup = _soundFxMixerGroup;

        audioSource.clip = audio_clip;
        audioSource.mute = !_soundFxOn;

        return host;
    }

    /// <summary>
    /// 모든 효과음에서 사용되는 내장 기본함수 (Vector3.zero 위치 생성)
    /// 효과음에 대한 특정 항목을 초기화함.
    /// </summary>
    /// <param name="audio_clip">재생할 클립</param>
    /// <returns>Newly created gameobject with sound effect and audio source attached</returns>
    private GameObject CreateSoundFx (AudioClip audio_clip, Vector3 location) {
        // 임시 오브젝트
        GameObject host = new GameObject ("TempSFX");
        host.transform.position = location;
        host.transform.SetParent (transform);
        host.AddComponent<SoundEffect> ();

        // 오디오소스 추가
        AudioSource audioSource = host.AddComponent<AudioSource> () as AudioSource;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 50;

        // 믹서 그룹을 사용할 경우
        audioSource.outputAudioMixerGroup = _soundFxMixerGroup;

        audioSource.clip = audio_clip;
        audioSource.mute = !_soundFxOn;

        return host;
    }

    /// <summary>
    /// 효과음이 효과음 풀에 존재하면 인덱스 알려주는 함수
    /// </summary>
    /// <param name="name">효과음 이름</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <returns>Index of sound effect or -1 is none exists</returns>
    public int IndexOfSoundFxPool (string name, bool singleton = false) {
        int index = 0;
        while (index < sfxPool.Count) {
            if (sfxPool[index].Name == name && singleton == sfxPool[index].Singleton) {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="duration">재생시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayAudioClipSFX (AudioClip clip, Vector2 location, float duration, float volume, bool singleton = false, float pitch = 1f, Action callback = null) {
        if (duration <= 0 || clip == null) {
            return null;
        }

        /*
        int index = IndexOfSoundFxPool (clip.name, true);

        if (index >= 0) {
            // 효과음 풀에 존재하면 재생시간 재설정해서 내보냄
            SoundEffect singletonSFx = sfxPool[index];
            singletonSFx.Duration = singletonSFx.Time = duration;
            sfxPool[index] = singletonSFx;

            return sfxPool[index].Source;
        }
        */

        GameObject host = null;
        AudioSource source = null;

        host = CreateSoundFx (clip, location);
        source = host.GetComponent<AudioSource> ();
        source.loop = duration > clip.length;
        source.volume = _soundFxVolume * volume;
        source.pitch = pitch;

        // 재사용 가능한 사운드 생성
        SoundEffect sfx = host.GetComponent<SoundEffect> ();
        sfx.Singleton = singleton;
        sfx.Source = source;
        sfx.OriginalVolume = volume;
        sfx.Duration = sfx.Time = duration;
        sfx.Callback = callback;

        // 풀에 넣는다.
        sfxPool.Add (sfx);

        source.Play ();

        return source;
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="duration">재생시간</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX (AudioClip clip, Vector2 location, float duration, bool singleton = false, Action callback = null) {
        return PlayAudioClipSFX (clip, location, duration, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 시간만큼 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="duration">재생시간</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlaySFX (AudioClip clip, float duration, bool singleton = false, Action callback = null) {
        return PlayAudioClipSFX (clip, Vector2.zero, duration, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatAudioClipSFX (AudioClip clip, Vector2 location, int repeat, float volume, bool singleton = false, float pitch = 1f, Action callback = null) {
        if (clip == null) {
            return null;
        }

        if (repeat != 0) {
            int index = IndexOfSoundFxPool (clip.name, true);

            if (index >= 0) {
                // 효과음 풀에 존재하면 재생시간 재설정해서 내보냄
                SoundEffect singletonSFx = sfxPool[index];
                singletonSFx.Duration = singletonSFx.Time = repeat > 0 ? clip.length * repeat : float.PositiveInfinity;
                sfxPool[index] = singletonSFx;

                return sfxPool[index].Source;
            }

            GameObject host = CreateSoundFx (clip, location);
            AudioSource source = host.GetComponent<AudioSource> ();
            source.loop = repeat != 0;
            source.volume = _soundFxVolume * volume;
            source.pitch = pitch;

            // 재사용 가능한 사운드 생성
            SoundEffect sfx = host.GetComponent<SoundEffect> ();
            sfx.Singleton = singleton;
            sfx.Source = source;
            sfx.OriginalVolume = volume;
            sfx.Duration = sfx.Time = repeat > 0 ? clip.length * repeat : float.PositiveInfinity;
            sfx.Callback = callback;

            // 풀에 넣는다.
            sfxPool.Add (sfx);

            source.Play ();

            return source;
        }

        // repeat 길이가 1보다 작거나 같으면 재생
        return PlayOneShot (clip, location, volume, pitch, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX (AudioClip clip, Vector2 location, int repeat, bool singleton = false, Action callback = null) {
        return RepeatAudioClipSFX (clip, location, repeat, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 지정된 횟수만큼 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="repeat">클립을 얼마나 반복할지 정한다. 무한은 음수를 입력하면 됨.</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource RepeatSFX (AudioClip clip, int repeat, bool singleton = false, Action callback = null) {
        return RepeatAudioClipSFX (clip, Vector2.zero, repeat, _soundFxVolume, singleton, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot (AudioClip clip, Vector2 location, float volume, float pitch = 1f, Action callback = null) {
        if (clip == null) {
            return null;
        }

        GameObject host = CreateSoundFx (clip, location);
        AudioSource source = host.GetComponent<AudioSource> ();
        source.loop = false;
        source.volume = _soundFxVolume * volume;
        source.pitch = pitch;

        // 재사용 가능한 사운드 생성
        SoundEffect sfx = host.GetComponent<SoundEffect> ();
        sfx.Singleton = false;
        sfx.Source = source;
        sfx.OriginalVolume = volume;
        sfx.Duration = sfx.Time = clip.length;
        sfx.Callback = callback;

        // 풀에 넣는다.
        sfxPool.Add (sfx);

        source.Play ();

        return source;
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot (AudioClip clip, Vector2 location, Action callback = null) {
        return PlayOneShot (clip, location, _soundFxVolume, 1f, callback);
    }

    /// <summary>
    /// 월드 스페이스(2D)에서 효과음을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayOneShot (AudioClip clip, Action callback = null) {
        return PlayOneShot (clip, Vector2.zero, _soundFxVolume, 1f, callback);
    }

    /// <summary>
    /// 특정 위치 월드 스페이스(3D)에서 효과음을 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수 (3D Sound Settings 활용)
    /// </summary>
    /// <returns>An AudioSource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 재생 위치 (3D)</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayClipAtPoint (AudioClip clip, Vector3 location, float volume, float pitch = 1f, Action callback = null) {
        if (clip == null) {
            return null;
        }
        //AudioSource.PlayClipAtPoint(clip, location);
        
        GameObject host = CreateSoundFx (clip, location);
        AudioSource source = host.GetComponent<AudioSource> ();
        source.loop = false;
        source.volume = _soundFxVolume * volume;
        source.pitch = pitch;

        // 재사용 가능한 사운드 생성
        SoundEffect sfx = host.GetComponent<SoundEffect> ();
        sfx.Singleton = false;
        sfx.Source = source;
        sfx.OriginalVolume = volume;
        sfx.Duration = sfx.Time = clip.length;
        sfx.Callback = callback;

        // 풀에 넣는다.
        sfxPool.Add (sfx);

        source.Play();
        //AudioSource source = null;

        return source;
    }

    /// <summary>
    /// 모든 효과음을 일시정지
    /// </summary>
    public void PauseAllSFX () {
        // SoundEffect 다 돌기
        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect> ()) {
            if (sfx.Source.isPlaying) sfx.Source.Pause ();
        }
    }

    /// <summary>
    /// 모든 효과음을 다시재생
    /// </summary>
    public void ResumeAllSFX () {
        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect> ()) {
            if (!sfx.Source.isPlaying) sfx.Source.UnPause ();
        }
    }

    /// <summary>
    /// 모든 효과음을 중지
    /// </summary>
    public void StopAllSFX () {
        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect> ()) {
            if (sfx.Source) {
                sfx.Source.Stop ();
                Destroy (sfx.gameObject);
            }
        }

        sfxPool.Clear ();
    }
    #endregion


    #region  VOICE
    /// <summary>
    /// VoicePool에 있는 음성을 관리하는 함수  
    /// OnUpdate함수에서 불러온다.
    /// </summary>
    private void ManageVoiceEffects () {
        for (int i = voicePool.Count - 1; i >= 0; i--) {
            VoiceEffect voiceEffect = voicePool[i];
            // 재생 중
            if (voiceEffect.Source.isPlaying && !float.IsPositiveInfinity (voiceEffect.Time)) {
                voiceEffect.Time -= Time.deltaTime;
                voicePool[i] = voiceEffect;
            }
            
            // 끝났을 때
            if (voicePool[i].Time <= 0.0001f || HasVoicePossiblyFinished (voicePool[i])) {
                voicePool[i].Source.Stop ();
                // 콜백함수 실행
                if (voicePool[i].Callback != null) {
                    voicePool[i].Callback.Invoke ();
                }

                // 클립 제거 후
                Destroy (voicePool[i].gameObject);

                // 풀에서 항목빼기
                voicePool.RemoveAt (i);
                break;
            }
        }
    }

    // 완전히 끝났는 지 체크용 함수
    bool HasVoicePossiblyFinished (VoiceEffect voiceEffect) {
        return !voiceEffect.Source.isPlaying && FloatEquals (voiceEffect.PlaybackPosition, 0) && voiceEffect.Time <= 0.09f;
    }

    /// <summary>
    /// 음성 볼륨 상태가 변했는지 체크하는 함수
    /// </summary>
    private bool IsVoiceAltered () {
        bool flag = _voiceOn != voiceOn || !FloatEquals (currentVoiceVol, _voiceVolume);
        return flag;
    }

    public int IndexOfVoicePool (string name, bool singleton = false) {
        int index = 0;
        while (index < voicePool.Count) {
            if (voicePool[index].Name == name) {
                return index;
            }
            index++;
        }
        return -1;
    }

    /// <summary>
    /// 음성 데이터와 오디오 클립을 가진 오브젝트 생성
    /// </summary>
    /// <param name="audio_clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <returns>Newly created gameobject with sound effect and audio source attached</returns>
    private GameObject CreateVoice (AudioClip audio_clip, Vector2 location) {
        // 임시 오브젝트
        GameObject host = new GameObject ("TempVoice");
        host.transform.position = location;
        host.transform.SetParent (transform);
        host.AddComponent<VoiceEffect> ();

        // 오디오소스 추가
        AudioSource audioSource = host.AddComponent<AudioSource> () as AudioSource;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // 믹서 그룹을 사용할 경우
        audioSource.outputAudioMixerGroup = _voiceMixerGroup;

        audioSource.clip = audio_clip;
        audioSource.mute = !_voiceOn;

        return host;
    }

    /// <summary>
    /// 지정된 시간만큼 음성을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="location">클립의 생성 위치 (2D)</param>
    /// <param name="duration">재생시간</param>
    /// <param name="volume">사운드 크기</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="pitch">클립의 피치 레벨 설정</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayAudioClipVoice (AudioClip clip, Vector2 location, float duration, float volume, bool singleton = false, float pitch = 1f, Action callback = null) {
        if (duration <= 0 || clip == null) {
            return null;
        }
        /*
        int index = IndexOfVoicePool (clip.name, true);

        if (index >= 0) {
            // 효과음 풀에 존재하면 재생시간 재설정해서 내보냄
            VoiceEffect voiceEffect = voicePool[index];
            voiceEffect.Duration = voiceEffect.Time = duration;
            voicePool[index] = voiceEffect;

            return voicePool[index].Source;
        }
        */

        GameObject host = null;
        AudioSource source = null;

        host = CreateVoice (clip, location);
        source = host.GetComponent<AudioSource> ();
        source.loop = duration > clip.length;
        source.volume = _voiceVolume * volume;
        source.pitch = pitch;

        // 재사용 가능한 사운드 생성
        VoiceEffect voice = host.GetComponent<VoiceEffect> ();
        voice.Singleton = singleton;
        voice.Source = source;
        voice.OriginalVolume = 1;
        voice.Duration = voice.Time = duration;
        voice.Callback = callback;

        // 풀에 넣는다.
        voicePool.Add (voice);

        source.Play ();

        return source;
    }

    /// <summary>
    /// 음성 사운드 크기조정 함수
    /// </summary>
    /// <param name="volume">New volume for all the sound effects.</param>
    private void SetVOICEVolume (float volume) {
        try {
            volume = Mathf.Clamp01 (volume);
            currentVoiceVol = _voiceVolume = volume;

            foreach (VoiceEffect voiceEffect in FindObjectsOfType<VoiceEffect> ()) {
                voiceEffect.Source.volume = _voiceVolume * voiceEffect.OriginalVolume;
                voiceEffect.Source.mute = !_voiceOn;
            }
        } catch (NullReferenceException nre) {
            Debug.LogError (nre.Message);
        } catch (Exception e) {
            Debug.LogError (e.Message);
        }
    }

    /// <summary>
    /// 지정된 시간만큼 음성을 생성해 재생하고 끝나면 지정된 콜백 함수를 호출하는 함수
    /// </summary>
    /// <returns>An audiosource</returns>
    /// <param name="clip">재생할 클립</param>
    /// <param name="duration">재생시간</param>
    /// <param name="singleton">효과음이 싱글톤인지 여부</param>
    /// <param name="callback">재생이 끝나면 콜백할 액션</param>
    public AudioSource PlayVoice (AudioClip clip, float duration, bool singleton = false, Action callback = null) {
        return PlayAudioClipVoice (clip, Vector2.zero, duration, _voiceVolume, singleton, 1f, callback);
    }
    
    /// <summary>
    /// 모든 음성을 일시정지
    /// </summary>
    public void PauseAllVoice () {
        // VoiceEffect 다 돌기
        foreach (VoiceEffect voiceEffect in FindObjectsOfType<VoiceEffect> ()) {
            if (voiceEffect.Source.isPlaying) voiceEffect.Source.Pause ();
        }
    }

    /// <summary>
    /// 모든 음성을 다시재생
    /// </summary>
    public void ResumeAllVoice () {
        foreach (VoiceEffect voiceEffect in FindObjectsOfType<VoiceEffect> ()) {
            if (!voiceEffect.Source.isPlaying) voiceEffect.Source.UnPause ();
        }
    }

    /// <summary>
    /// 모든 음성을 중지
    /// </summary>
    public void StopAllVoice () {
        foreach (VoiceEffect voiceEffect in FindObjectsOfType<VoiceEffect> ()) {
            if (voiceEffect.Source) {
                voiceEffect.Source.Stop ();
                Destroy (voiceEffect.gameObject);
            }
        }
        voicePool.Clear ();
    }
    #endregion


    #region  AUDIO CONFIG
    /// <summary>
    /// 배경음, 효과음 On/Off 토글 함수
    /// </summary>
    /// <param name="flag">On - true, Off - false</param>
    private void ToggleMute (bool flag) {
        ToggleBGMMute (flag);
        ToggleSFXMute (flag);
    }

    /// <summary>
    /// 배경음 On/Off 토글 함수
    /// </summary>
    /// <param name="flag">On - true, Off - false</param>
    private void ToggleBGMMute (bool flag) {
        musicOn = _musicOn = flag;
        musicSource.mute = !musicOn;
    }

    /// <summary>
    /// 효과음 On/Off 토글 함수
    /// </summary>
    /// <param name="flag">On - true, Off - false</param>
    private void ToggleSFXMute (bool flag) {
        sfxOn = _soundFxOn = flag;

        foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect> ()) {
            sfx.Source.mute = !sfxOn;
        }
    }

    /// <summary>
    /// 음성 On/Off 토글 함수
    /// </summary>
    /// <param name="flag">On - true, Off - false</param>
    private void ToggleVoiceMute (bool flag) {
        voiceOn = _voiceOn = flag;

        foreach (VoiceEffect voiceEffect in FindObjectsOfType<VoiceEffect> ()) {
            voiceEffect.Source.mute = !voiceOn;
        }
    }

    /// <summary>
    /// 배경음 사운드 크기조정 함수
    /// </summary>
    /// <param name="volume">New volume of the background music.</param>
    private void SetBGMVolume (float volume) {
        try {
            volume = Mathf.Clamp01 (volume);
            // 모든 사운드 크기 변수에 할당
            musicSource.volume = currentMusicVol = _musicVolume = volume;

            if (_musicMixerGroup != null && !string.IsNullOrEmpty (_volumeOfMusicMixer.Trim ())) {
                float mixerVol = -80f + (volume * 100f);
                _musicMixerGroup.audioMixer.SetFloat (_volumeOfMusicMixer, mixerVol);
            }
        } catch (NullReferenceException nre) {
            Debug.LogError (nre.Message);
        } catch (Exception e) {
            Debug.LogError (e.Message);
        }
    }

    /// <summary>
    /// 효과음 사운드 크기조정 함수
    /// </summary>
    /// <param name="volume">New volume for all the sound effects.</param>
    private void SetSFXVolume (float volume) {
        try {
            volume = Mathf.Clamp01 (volume);
            currentSfxVol = _soundFxVolume = volume;

            foreach (SoundEffect sfx in FindObjectsOfType<SoundEffect> ()) {
                sfx.Source.volume = _soundFxVolume * sfx.OriginalVolume;
                sfx.Source.mute = !_soundFxOn;
            }

            if (_soundFxMixerGroup != null && !string.IsNullOrEmpty (_volumeOfSFXMixer.Trim ())) {
                float mixerVol = -80f + (volume * 100f);
                _soundFxMixerGroup.audioMixer.SetFloat (_volumeOfSFXMixer, mixerVol);
            }
        } catch (NullReferenceException nre) {
            Debug.LogError (nre.Message);
        } catch (Exception e) {
            Debug.LogError (e.Message);
        }
    }

    /// <summary>
    /// 오디오 관리자 사운드 크기를 0- 1 로 정규화하는 함수
    /// </summary>
    /// <returns>The normalised volume between the range of zero and one.</returns>
    /// <param name="vol">사운드 크기</param>
    private float NormaliseVolume (float vol) {
        vol += 80f;
        vol /= 100f;
        return vol;
    }

    /// <summary>
    /// 배경음 사운드 크기를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns></returns>
    private float LoadBGMVolume () {
        return PlayerPrefs.HasKey (BgMusicVolKey) ? PlayerPrefs.GetFloat (BgMusicVolKey) : _musicVolume;
    }

    /// <summary>
    /// 효과음 사운드 크기를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns></returns>
    private float LoadSFXVolume () {
        return PlayerPrefs.HasKey (SoundFxVolKey) ? PlayerPrefs.GetFloat (SoundFxVolKey) : _soundFxVolume;
    }

    /// <summary>
    /// 음성 사운드 크기를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns></returns>
    private float LoadVOICEVolume () {
        return PlayerPrefs.HasKey (VoiceVolKey) ? PlayerPrefs.GetFloat (VoiceVolKey) : _voiceVolume;
    }

    /// <summary>
    /// int값을 bool값으로 변환하는 함수
    /// </summary>
    private bool ToBool (int integer) {
        return integer == 0 ? false : true;
    }

    /// <summary>
    /// 배경음 On/Off 여부를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns>Returns the value of the background music mute key from the saved preferences if it exists or the defaut value if it does not</returns>
    private bool LoadBGMMuteStatus () {
        return PlayerPrefs.HasKey (BgMusicMuteKey) ? ToBool (PlayerPrefs.GetInt (BgMusicMuteKey)) : _musicOn;
    }

    /// <summary>
    /// 효과음 On/Off 여부를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns>Returns the value of the sound effect mute key from the saved preferences if it exists or the defaut value if it does not</returns>
    private bool LoadSFXMuteStatus () {
        return PlayerPrefs.HasKey (SoundFxMuteKey) ? ToBool (PlayerPrefs.GetInt (SoundFxMuteKey)) : _soundFxOn;
    }

    /// <summary>
    /// 음성 On/Off 여부를 PlayerPrefs에서 가져오는 함수
    /// </summary>
    /// <returns>Returns the value of the sound effect mute key from the saved preferences if it exists or the defaut value if it does not</returns>
    private bool LoadVOICEMuteStatus () {
        return PlayerPrefs.HasKey (VoiceMuteKey) ? ToBool (PlayerPrefs.GetInt (VoiceMuteKey)) : _voiceOn;
    }

    /// <summary>
    /// 배경음 On/Off 여부와 사운드 크기를 PlayerPrefs에 저장하는 함수
    /// </summary>
    public void SaveBGMPreferences () {
        PlayerPrefs.SetInt (BgMusicMuteKey, _musicOn ? 1 : 0);
        PlayerPrefs.SetFloat (BgMusicVolKey, _musicVolume);
        PlayerPrefs.Save ();
    }

    /// <summary>
    /// 효과음 On/Off 여부와 사운드 크기를 PlayerPrefs에 저장하는 함수
    /// </summary>
    public void SaveSFXPreferences () {
        PlayerPrefs.SetInt (SoundFxMuteKey, _soundFxOn ? 1 : 0);
        PlayerPrefs.SetFloat (SoundFxVolKey, _soundFxVolume);
        PlayerPrefs.Save ();
    }
    
    /// <summary>
    /// 음성 On/Off 여부와 사운드 크기를 PlayerPrefs에 저장하는 함수
    /// </summary>
    public void SaveVOICEPreferences () {
        PlayerPrefs.SetInt (VoiceMuteKey, _voiceOn ? 1 : 0);
        PlayerPrefs.SetFloat (VoiceVolKey, _voiceVolume);
        PlayerPrefs.Save ();
    }

    /// <summary>
    /// 모든 PlayerPrefs 초기화 하는 함수
    /// </summary>
    public void ClearAllPreferences () {
        PlayerPrefs.DeleteKey (BgMusicVolKey);
        PlayerPrefs.DeleteKey (SoundFxVolKey);
        PlayerPrefs.DeleteKey (VoiceVolKey);
        PlayerPrefs.DeleteKey (BgMusicMuteKey);
        PlayerPrefs.DeleteKey (SoundFxMuteKey);
        PlayerPrefs.DeleteKey (VoiceMuteKey);
        PlayerPrefs.Save ();
    }

    /// <summary>
    /// 모든 사운드 옵션을 PlayerPrefs에 저장하는 함수
    /// </summary>
    public void SaveAllPreferences () {
        PlayerPrefs.SetFloat (SoundFxVolKey, _soundFxVolume);
        PlayerPrefs.SetFloat (BgMusicVolKey, _musicVolume);
        PlayerPrefs.SetFloat (VoiceVolKey, _voiceVolume);
        PlayerPrefs.SetInt (SoundFxMuteKey, _soundFxOn ? 1 : 0);
        PlayerPrefs.SetInt (BgMusicMuteKey, _musicOn ? 1 : 0);
        PlayerPrefs.SetInt (VoiceMuteKey, _voiceOn ? 1 : 0);
        PlayerPrefs.Save ();
    }

    /// <summary>
    /// 현재 배경음 클립을 가져오는 속성
    /// </summary>
    /// <value>The current music clip.</value>
    public AudioClip CurrentMusicClip {
        get { return backgroundMusic.CurrentClip; }
    }

    /// <summary>
    /// 효과음 풀을 가져오는 속성
    /// </summary>
    public List<SoundEffect> SoundFxPool {
        get { return sfxPool; }
    }

    /// <summary>
    /// 배경음이 재생중인지 체크하는 속성
    /// </summary>
    public bool IsMusicPlaying {
        get { return musicSource != null && musicSource.isPlaying; }
    }

    /// <summary>
    /// 배경음 사운드 크기를 가져오거나 지정하는 속성
    /// </summary>
    /// <value>사운드 크기</value>
    public float MusicVolume {
        get { return _musicVolume; }
        set { SetBGMVolume (value); }
    }

    /// <summary>
    /// 효과음 사운드 크기를 가져오거나 지정하는 속성
    /// </summary>
    /// <value>사운드 크기</value>
    public float SoundVolume {
        get { return _soundFxVolume; }
        set { SetSFXVolume (value); }
    }

    /// <summary>
    /// 음성 사운드 크기를 가져오거나 지정하는 속성
    /// </summary>
    /// <value>사운드 크기</value>
    public float VoiceVolume {
        get { return _voiceVolume; }
        set { SetVOICEVolume (value); }
    }

    /// <summary>
    /// 배경음 On/Off 체크하거나 지정하는 속성
    /// </summary>
    /// <value><c>true</c> - BGM On; <c>false</c> - BGM Off</value>
    public bool IsMusicOn {
        get { return _musicOn; }
        set { ToggleBGMMute (value); }
    }

    /// <summary>
    /// 효과음 On/Off 체크하거나 지정하는 속성
    /// </summary>
    /// <value><c>true</c> - SFX On; <c>false</c> - SFX Off</value>
    public bool IsSoundOn {
        get { return _soundFxOn; }
        set { ToggleSFXMute (value); }
    }

    /// <summary>
    /// 음성 On/Off 체크하거나 지정하는 속성
    /// </summary>
    /// <value><c>true</c> - SFX On; <c>false</c> - SFX Off</value>
    public bool IsVoiceOn {
        get { return _voiceOn; }
        set { ToggleVoiceMute (value); }
    }

    /// <summary>
    /// 배경음과 효과음 On/Off 체크하거나 지정하는 속성
    /// </summary>
    /// <value><c>true</c> - BGM+SFX On; <c>false</c> - BGM+SFX Off</value>
    public bool IsMasterMute {
        get { return !_musicOn && !_soundFxOn; }
        set { ToggleMute (value); }
    }

}
#endregion

/// <summary>
/// 전환효과
/// </summary>
public enum MusicTransition {
    /// <summary>
    /// (없음) 다음음악이 즉시 재생
    /// </summary>
    Swift,
    /// <summary>
    /// (페이드 인/아웃) 페이드 아웃되고 다음 음악 페이드 인
    /// </summary>
    LinearFade,
    /// <summary>
    /// (크로스) 현재음악과 다음음악이 크로스
    /// </summary>
    CrossFade
}

/// <summary>
/// 배경음 설정
/// </summary>
[System.Serializable]
public struct BackgroundMusic {
    /// <summary>
    /// 배경음 현재 클립
    /// </summary>
    public AudioClip CurrentClip;
    /// <summary>
    /// 배경음 다음 클립
    /// </summary>
    public AudioClip NextClip;
    /// <summary>
    /// 전환효과
    /// </summary>
    public MusicTransition MusicTransition;
    /// <summary>
    /// 전환효과 시간
    /// </summary>
    public float TransitionDuration;
}

/// <summary>
/// 효과음 구조와 설정
/// </summary>
[System.Serializable]
public class SoundEffect : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float originalVolume;
    [SerializeField] private float duration;
    [SerializeField] private float playbackPosition;
    [SerializeField] private float time;
    [SerializeField] private Action callback;
    [SerializeField] private bool singleton;

    /// <summary>
    /// 효과음 이름 속성
    /// </summary>
    /// <value>이름</value>
    public string Name {
        get { return audioSource.clip.name; }
    }

    /// <summary>
    /// 효과음 길이 속성 (초 단위)
    /// </summary>
    /// <value>길이</value>
    public float Length {
        get { return audioSource.clip.length; }
    }

    /// <summary>
    /// 효과음 재생된 시간 속성 (초 단위)
    /// </summary>
    /// <value>재생된 시간</value>
    public float PlaybackPosition {
        get { return audioSource.time; }
    }

    /// <summary>
    /// 효과음 클립 속성
    /// </summary>
    /// <value>오디오 클립</value>
    public AudioSource Source {
        get { return audioSource; }
        set { audioSource = value; }
    }

    /// <summary>
    /// 효과음 원본 볼륨 속성
    /// </summary>
    /// <value>원본 사운드 크기</value>
    public float OriginalVolume {
        get { return originalVolume; }
        set { originalVolume = value; }
    }

    /// <summary>
    /// 효과음 총 재생시간 속성 (초단위)
    /// </summary>
    /// <value>총 재생시간</value>
    public float Duration {
        get { return duration; }
        set { duration = value; }
    }

    /// <summary>
    /// 효과음 남은 재생시간 속성 (초단위)
    /// </summary>
    /// <value>남은 재생시간</value>
    public float Time {
        get { return time; }
        set { time = value; }
    }

    /// <summary>
    /// 효과음 정규화된 재생진행도 속성 (정규화 0~1)
    /// </summary>
    /// <value>정규화된 재생진행도</value>
    public float NormalisedTime {
        get { return Time / Duration; }
    }

    /// <summary>
    /// 효과음 완료 시 콜백 액션 속성
    /// </summary>
    /// <value>콜백 액션</value>
    public Action Callback {
        get { return callback; }
        set { callback = value; }
    }

    /// <summary>
    /// 효과음 반복 시 싱글톤 여부, 반복할 경우에 true 아니면 false
    /// </summary>
    /// <value><c>true</c> 반복 시; 그 외, <c>false</c>.</value>
    public bool Singleton {
        get { return singleton; }
        set { singleton = value; }
    }
}

/// <summary>
/// 음성 구조와 설정
/// </summary>
[System.Serializable]
public class VoiceEffect : MonoBehaviour {
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float originalVolume;
    [SerializeField] private float duration;
    [SerializeField] private float playbackPosition;
    [SerializeField] private float time;
    [SerializeField] private Action callback;
    [SerializeField] private bool singleton;

    /// <summary>
    /// 음성 이름 속성
    /// </summary>
    /// <value>이름</value>
    public string Name {
        get { return audioSource.clip.name; }
    }

    /// <summary>
    /// 음성 길이 속성 (초 단위)
    /// </summary>
    /// <value>길이</value>
    public float Length {
        get { return audioSource.clip.length; }
    }

    /// <summary>
    /// 음성 재생된 시간 속성 (초 단위)
    /// </summary>
    /// <value>재생된 시간</value>
    public float PlaybackPosition {
        get { return audioSource.time; }
    }

    /// <summary>
    /// 음성 클립 속성
    /// </summary>
    /// <value>오디오 클립</value>
    public AudioSource Source {
        get { return audioSource; }
        set { audioSource = value; }
    }

    /// <summary>
    /// 음성 원본 볼륨 속성
    /// </summary>
    /// <value>원본 사운드 크기</value>
    public float OriginalVolume {
        get { return originalVolume; }
        set { originalVolume = value; }
    }

    /// <summary>
    /// 음성 총 재생시간 속성 (초단위)
    /// </summary>
    /// <value>총 재생시간</value>
    public float Duration {
        get { return duration; }
        set { duration = value; }
    }

    /// <summary>
    /// 음성 남은 재생시간 속성 (초단위)
    /// </summary>
    /// <value>남은 재생시간</value>
    public float Time {
        get { return time; }
        set { time = value; }
    }

    /// <summary>
    /// 음성 정규화된 재생진행도 속성 (정규화 0~1)
    /// </summary>
    /// <value>정규화된 재생진행도</value>
    public float NormalisedTime {
        get { return Time / Duration; }
    }

    /// <summary>
    /// 음성 완료 시 콜백 액션 속성
    /// </summary>
    /// <value>콜백 액션</value>
    public Action Callback {
        get { return callback; }
        set { callback = value; }
    }

    /// <summary>
    /// 음성 반복 시 싱글톤 여부, 반복할 경우에 true 아니면 false
    /// </summary>
    /// <value><c>true</c> 반복 시; 그 외, <c>false</c>.</value>
    public bool Singleton {
        get { return singleton; }
        set { singleton = value; }
    }
}

public enum SOUND_TYPE {
    BGM,
    VOICE,
    SFX
}

public enum BGM_TYPE {
    Battle,
    Boss,
    Event,
    MainTitle,
    Map
}

public enum VOICE_TYPE {
    Geork,
    Eris,
    HongDanHyang,
    MoonGirl,
    Apates,
    Geras,
    Hifnos,
    Kearce,
    Momos,
    Moros,
    Nemesis,
    Oijis,
    Oneyrus,
    Pilotes,
    RyuJinSol,
    ShadowMan,
    Sofia,
    Todd
}