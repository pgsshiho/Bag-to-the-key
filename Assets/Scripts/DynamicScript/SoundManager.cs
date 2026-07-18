using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    [SerializeField] private AudioSource bgm;
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MasterKey = "Master";
    private const string BgmKey = "BGM";
    private const string SfxKey = "SFX";

    public static AudioSource BGM => instance != null ? instance.bgm : null;
    public static AudioSource SFX => instance != null ? instance.sfx : null;

    void Awake()
    {
        // 싱글턴 중복 방지
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 저장된 값 로드 (없으면 1.0f)
        float savedMaster = PlayerPrefs.GetFloat(MasterKey, 1f);
        float savedBgm = PlayerPrefs.GetFloat(BgmKey, 1f);
        float savedSfx = PlayerPrefs.GetFloat(SfxKey, 1f);

        // 슬라이더가 연결되어 있으면 값 설정
        if (masterVolumeSlider != null) masterVolumeSlider.value = Mathf.Clamp01(savedMaster);
        if (bgmSlider != null) bgmSlider.value = Mathf.Clamp01(savedBgm);
        if (sfxSlider != null) sfxSlider.value = Mathf.Clamp01(savedSfx);

        // 오디오 믹서에 적용
        SetMasterVolume(savedMaster);
        SetBGMVolume(savedBgm);
        SetSFXVolume(savedSfx);

        // 슬라이더 이벤트 연결(선택 사항) - 이미 에디터에서 연결했다면 중복으로 연결하지 않음
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // value: 0..1 범위 기대
    public void SetMasterVolume(float value)
    {
        float v = Mathf.Clamp(value, 0.0001f, 1f);
        if (audioMixer != null) audioMixer.SetFloat(MasterKey, Mathf.Log10(v) * 20f);
        PlayerPrefs.SetFloat(MasterKey, value);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float value)
    {
        float v = Mathf.Clamp(value, 0.0001f, 1f);
        if (audioMixer != null) audioMixer.SetFloat(BgmKey, Mathf.Log10(v) * 20f);
        PlayerPrefs.SetFloat(BgmKey, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        float v = Mathf.Clamp(value, 0.0001f, 1f);
        if (audioMixer != null) audioMixer.SetFloat(SfxKey, Mathf.Log10(v) * 20f);
        PlayerPrefs.SetFloat(SfxKey, value);
        PlayerPrefs.Save();
    }

    // 모든 설정을 기본값(1.0)으로 초기화하고 UI를 갱신
    public void ResetToDefaults()
    {
        const float def = 1f;

        // 오디오 믹서와 PlayerPrefs에 적용
        SetMasterVolume(def);
        SetBGMVolume(def);
        SetSFXVolume(def);

        // 슬라이더 UI를 이벤트를 트리거하지 않고 업데이트
        if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(def);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(def);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(def);
    }

    // 간단한 SFX 재생 유틸리티
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfx == null || clip == null) return;
        sfx.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

}
