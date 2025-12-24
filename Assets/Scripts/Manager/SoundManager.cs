using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundData
{
    public string soundName; // 부를 이름 (예: "Click", "Plant")
    public AudioClip clip;   // 실제 오디오 파일
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Sound Settings")]
    public AudioSource sfxSource;   // 효과음 틀어줄 스피커
    public AudioSource bgmSource;   // 배경음악 틀어줄 스피커 (나중을 위해)

    [Header("Registered Sounds")]
    public List<SoundData> sfxList; // 인스펙터에서 등록할 소리 목록

    // 소리를 빠르게 찾기 위한 딕셔너리
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 소리 매니저는 사라지지 않음
            InitSoundDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 리스트에 있는 걸 딕셔너리로 옮겨담기 (성능 최적화)
    void InitSoundDictionary()
    {
        foreach (var data in sfxList)
        {
            if (!sfxDictionary.ContainsKey(data.soundName))
            {
                sfxDictionary.Add(data.soundName, data.clip);
            }
        }
    }

    // 다른 스크립트에서 이 함수만 부르면 소리가 남
    public void PlaySFX(string name)
    {
        if (sfxDictionary.ContainsKey(name))
        {
            sfxSource.PlayOneShot(sfxDictionary[name]);
        }
        else
        {
            Debug.LogWarning($"'{name}'을(를) 찾을 수 없습니다.");
        }
    }
}