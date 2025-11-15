using System.Collections.Generic;
using UnityEngine;

public class GameProgressionManager : MonoBehaviour
{
    // 1. 싱글톤 설정
    public static GameProgressionManager Instance { get; private set; }

    // 2. 해금된 씨앗 목록 (HashSet이 List보다 검색이 빠름)
    public HashSet<ItemData> unlockedSeeds = new HashSet<ItemData>();

    // [테스트용] Inspector에서 미리 해금할 씨앗
    public List<ItemData> startingSeeds;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 테스트용: 게임 시작 시 startingSeeds에 있는 건 모두 해금
        foreach (ItemData seed in startingSeeds)
        {
            UnlockSeed(seed);
        }
    }

    // 3. (퀘스트 팀이 호출) 씨앗을 해금하는 함수
    public void UnlockSeed(ItemData seed)
    {
        if (seed != null && !unlockedSeeds.Contains(seed))
        {
            unlockedSeeds.Add(seed);
            Debug.Log(seed.itemName + " 씨앗 해금!");
        }
    }

    // 4. (상점 팀이 호출) 씨앗이 해금되었는지 '확인'하는 함수
    public bool IsSeedUnlocked(ItemData seed)
    {
        return unlockedSeeds.Contains(seed);
    }
}