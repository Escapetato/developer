using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundData
{
    public string soundName;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Sound Settings")]
    public AudioSource sfxSource;
    public AudioSource bgmSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Range(0.1f, 3.0f)]
    public float fadeDuration = 0.3f;

    [Header("Data Lists")]
    public List<SoundData> sfxList;
    public List<SoundData> bgmList;

    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();

    private Coroutine currentFadeCoroutine; // 현재 실행 중인 페이드 작업을 저장

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // ▼▼▼ [이 줄 추가!] 부모(@Managers) 밑에 있으면 같이 죽으니까 탈출! ▼▼▼
            transform.SetParent(null);
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            DontDestroyOnLoad(gameObject);
            InitSoundDictionary();

            if (bgmSource != null) bgmSource.volume = bgmVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume;

            if (bgmSource != null)
            {
                bgmSource.loop = true;
            }
        }
        else
        {
            // 이미 Auth 씬에서 만들어진 SoundManager가 넘어왔다면,
            // Main 씬에 원래 있던 놈은 파괴
            Destroy(gameObject);
        }
    }

    void InitSoundDictionary()
    {
        foreach (var data in sfxList)
        {
            if (!sfxDictionary.ContainsKey(data.soundName)) sfxDictionary.Add(data.soundName, data.clip);
        }
        foreach (var data in bgmList)
        {
            if (!bgmDictionary.ContainsKey(data.soundName)) bgmDictionary.Add(data.soundName, data.clip);
        }
    }

    public void PlaySFX(string name)
    {
        if (sfxDictionary.ContainsKey(name)) sfxSource.PlayOneShot(sfxDictionary[name]);
        else Debug.LogWarning($"SFX '{name}' 없음!");
    }

    public void PlayBGM(string name)
    {
        if (bgmDictionary.ContainsKey(name))
        {
            AudioClip nextClip = bgmDictionary[name];

            // 1. 이미 똑같은 노래가 나오고 있으면 무시 (괜히 페이드하지 않음)
            if (bgmSource.clip == nextClip && bgmSource.isPlaying) return;

            // 2. 이미 페이드 중이었다면 멈추고 새로운 페이드 시작
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);

            // 3. 부드럽게 전환 시작
            currentFadeCoroutine = StartCoroutine(FadeToBGM(nextClip));
        }
        else
        {
            Debug.LogWarning($"BGM '{name}' 없음!");
        }
    }

    IEnumerator FadeToBGM(AudioClip newClip)
    {
        // 1단계: 기존 음악 페이드 아웃 (볼륨 1 -> 0)
        float startVolume = bgmSource.volume;

        // 만약 음악이 켜져 있었다면 서서히 줄이기
        if (bgmSource.isPlaying)
        {
            while (bgmSource.volume > 0)
            {
                bgmSource.volume -= startVolume * Time.deltaTime / fadeDuration;
                yield return null; // 한 프레임 대기
            }
        }

        bgmSource.volume = 0;
        bgmSource.Stop();

        // 2단계: 음악 교체
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 3단계: 새 음악 페이드 인 (볼륨 0 -> 1)
        while (bgmSource.volume < bgmVolume)
        {
            bgmSource.volume += Time.deltaTime / fadeDuration;
            yield return null;
        }

        bgmSource.volume = bgmVolume;
    }
}