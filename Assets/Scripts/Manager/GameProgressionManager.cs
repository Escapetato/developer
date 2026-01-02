using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance { get; private set; }

    public bool isShopUnlocked = false;

    // 도감 (아이템)
    public HashSet<ItemData> unlockedItems = new HashSet<ItemData>();

    // 도감 (레시피)
    public HashSet<EvolutionRecipe> unlockedRecipes = new HashSet<EvolutionRecipe>();

    public List<ItemData> startingItems;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }

        // 시작 아이템 해금 로직 (기존 코드 유지)
        if (startingItems != null)
        {
            foreach (ItemData item in startingItems)
            {
                UnlockItem(item, false);
            }
        }
    }

    void SaveToDB()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null && DBManager.Instance != null)
        {
            string myId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            DBManager.Instance.SaveAllData(myId);
        }
    }

    public void UnlockShop()
    {
        if (!isShopUnlocked)
        {
            isShopUnlocked = true;
            Debug.Log("🔓 상점 해금!");
            SaveToDB();
        }
    }

    // ▼▼▼ [수정됨] save 변수 추가 (기본값 true) ▼▼▼
    public void UnlockItem(ItemData item, bool save = true)
    {
        if (item != null && !unlockedItems.Contains(item))
        {
            unlockedItems.Add(item);
            Debug.Log(item.itemName + " 도감 해금!");

            // save가 true일 때만 저장 (Awake에서는 false로 들어옴)
            if (save) SaveToDB();
        }
    }

    // ▼▼▼ [수정됨] save 변수 추가 (기본값 true) ▼▼▼
    public void UnlockRecipe(EvolutionRecipe recipe, bool save = true)
    {
        if (recipe != null && !unlockedRecipes.Contains(recipe))
        {
            unlockedRecipes.Add(recipe);
            Debug.Log(recipe.name + " 레시피 해금!");

            // save가 true일 때만 저장
            if (save) SaveToDB();
        }
    }

    public bool IsItemUnlocked(ItemData item)
    {
        return unlockedItems.Contains(item);
    }

    public void LoadProgression(bool shopUnlocked, List<string> unlockedItemNames, List<string> unlockedRecipeNames)
    {
        isShopUnlocked = shopUnlocked;

        // 1. 아이템 복구
        unlockedItems.Clear();

        // 로드할 때는 기본 아이템도 다시 넣어주는 게 안전함
        foreach (ItemData item in startingItems)
        {
            if (item != null) unlockedItems.Add(item);
        }

        if (DBManager.Instance != null)
        {
            foreach (string name in unlockedItemNames)
            {
                ItemData item = DBManager.Instance.FindItemByName(name);
                if (item != null) unlockedItems.Add(item);
            }

            // 2. 레시피 복구
            unlockedRecipes.Clear();
            foreach (string name in unlockedRecipeNames)
            {
                EvolutionRecipe recipe = DBManager.Instance.FindRecipeByName(name);
                if (recipe != null) unlockedRecipes.Add(recipe);
            }
        }
        Debug.Log($"복구 완료: 아이템 {unlockedItems.Count}개 / 레시피 {unlockedRecipes.Count}개");
    }
}