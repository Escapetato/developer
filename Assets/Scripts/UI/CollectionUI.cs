using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionUI : MonoBehaviour
{
    public static CollectionUI Instance { get; private set; }

    [Header("--- Data Sources ---")]
    public List<ItemData> allBaseCrops;
    public List<EvolutionRecipe> allRecipes;

    [Header("--- Category Buttons (탭) ---")]
    // ★ [추가] 탭 버튼들을 관리할 리스트
    public List<CategoryButton> categoryButtons;

    [Header("--- Assets ---")]
    public Sprite lockedSprite;

    [Header("--- Left Page UI ---")]
    public Image baseCropImage;
    public TextMeshProUGUI baseCropName;
    public TextMeshProUGUI growTimeText;
    public TextMeshProUGUI toolText;

    public CollectionEvoSlot evoSlot1;
    public CollectionEvoSlot evoSlot2;

    [Header("--- Right Page UI ---")]
    public GameObject rightPageGroup;
    public Image resultCropImage;
    public TextMeshProUGUI resultName;
    public TextMeshProUGUI potionUsedText;
    public Image potionUsedImage;
    public TextMeshProUGUI descriptionText;

    [Header("--- Navigation ---")]
    public Button prevButton;
    public Button nextButton;

    private List<ItemData> currentCategoryList;
    private int currentIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (rightPageGroup != null) rightPageGroup.SetActive(false);
    }

    void OnEnable()
    {
        // 첫 번째 탭 버튼을 자동으로 누르게 함
        if (categoryButtons != null && categoryButtons.Count > 0)
        {
            SetCategoryButton(categoryButtons[0]);
        }
        else
        {
            // 버튼 연결 안 했을 때를 대비한 안전장치
            SetCategory("Vegetable");
        }
    }

    // ★ [추가] 탭 버튼 클릭 시 호출되는 함수 (이미지 교체 + 페이지 갱신)
    public void SetCategoryButton(CategoryButton clickedButton)
    {
        // 1. 모든 버튼을 '선택 안 됨(Default)' 상태로
        foreach (CategoryButton btn in categoryButtons)
        {
            btn.SetSelected(false);
        }

        // 2. 클릭한 버튼만 '선택 됨(Selected)' 상태로
        clickedButton.SetSelected(true);

        // 3. 실제 카테고리 데이터 갱신
        SetCategory(clickedButton.categoryName);
    }

    public void SetCategory(string category)
    {
        currentCategoryList = new List<ItemData>();

        foreach (var item in allBaseCrops)
        {
            if (item.collectionCategory == category)
            {
                currentCategoryList.Add(item);
            }
        }

        currentIndex = 0;
        UpdateLeftPage();
    }

    public void OnPrevBtnClick()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = currentCategoryList.Count - 1;
        UpdateLeftPage();
    }

    public void OnNextBtnClick()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;
        currentIndex++;
        if (currentIndex >= currentCategoryList.Count) currentIndex = 0;
        UpdateLeftPage();
    }

    private void UpdateLeftPage()
    {
        // 1. 데이터 안전 점검
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;

        if (rightPageGroup != null) rightPageGroup.SetActive(false);
        if (evoSlot1 != null) evoSlot1.SetSelected(false);
        if (evoSlot2 != null) evoSlot2.SetSelected(false);

        ItemData currentBaseItem = currentCategoryList[currentIndex];

        // 2. 메인 작물(베이스) 해금 여부 확인
        bool isBaseUnlocked = GameProgressionManager.Instance.IsItemUnlocked(currentBaseItem);

        // [디버깅용 로그] 메인 작물 상태 확인
        Debug.Log($"현재 작물: {currentBaseItem.itemName}, 메인 해금여부: {isBaseUnlocked}");

        if (isBaseUnlocked)
        {
            if (baseCropImage != null) baseCropImage.sprite = currentBaseItem.itemIcon;
            if (baseCropName != null) baseCropName.text = currentBaseItem.itemName;
            if (growTimeText != null) growTimeText.text = "재배 시간: " + currentBaseItem.growTimeDisplay;
            if (toolText != null) toolText.text = "수확 도구: " + currentBaseItem.harvestToolName;
        }
        else
        {
            if (baseCropImage != null && lockedSprite != null) baseCropImage.sprite = lockedSprite;
            if (baseCropName != null) baseCropName.text = "???";
            if (growTimeText != null) growTimeText.text = "재배 시간: ???";
            if (toolText != null) toolText.text = "수확 도구: ???";
        }

        // 3. 진화 레시피 찾기
        List<EvolutionRecipe> myRecipes = new List<EvolutionRecipe>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.material == currentBaseItem)
            {
                myRecipes.Add(recipe);
            }
        }

        // 슬롯 1 처리
        if (myRecipes.Count > 0)
        {
            // 원래 결과물이 해금되었는지?
            bool isResultOriginallyUnlocked = GameProgressionManager.Instance.IsItemUnlocked(myRecipes[0].resultItem);

            // ★ [최종 판정] 메인 작물도 뚫리고(AND) && 결과물도 뚫려야 진짜 보여줌
            bool finalShow = isBaseUnlocked && isResultOriginallyUnlocked;

            // 디버깅: 왜 false인지 확인해보세요
            // Debug.Log($"슬롯1 판정: 메인({isBaseUnlocked}) && 결과({isResultOriginallyUnlocked}) = 최종({finalShow})");

            evoSlot1.Setup(myRecipes[0], finalShow);
        }
        else
        {
            evoSlot1.Setup(null, false);
        }

        // 슬롯 2 처리
        if (myRecipes.Count > 1)
        {
            bool isResultOriginallyUnlocked = GameProgressionManager.Instance.IsItemUnlocked(myRecipes[1].resultItem);

            // ★ [최종 판정] 여기도 똑같이 && 연산자 필수
            bool finalShow = isBaseUnlocked && isResultOriginallyUnlocked;

            evoSlot2.Setup(myRecipes[1], finalShow);
        }
        else
        {
            evoSlot2.Setup(null, false);
        }
    }

    public void OnEvoSlotClicked(CollectionEvoSlot clickedSlot, EvolutionRecipe recipe)
    {
        ShowRightPage(recipe);

        if (clickedSlot == evoSlot1)
        {
            evoSlot1.SetSelected(true);
            evoSlot2.SetSelected(false);
        }
        else if (clickedSlot == evoSlot2)
        {
            evoSlot1.SetSelected(false);
            evoSlot2.SetSelected(true);
        }
    }

    public void ShowRightPage(EvolutionRecipe recipe)
    {
        if (rightPageGroup != null) rightPageGroup.SetActive(true);

        if (resultCropImage != null) resultCropImage.sprite = recipe.resultItem.itemIcon;
        if (resultName != null) resultName.text = recipe.resultItem.itemName;

        if (potionUsedText != null) potionUsedText.text = "생명의 물방울: " + recipe.potion.itemName;
        if (potionUsedImage != null) potionUsedImage.sprite = recipe.potion.itemIcon;

        if (descriptionText != null) descriptionText.text = recipe.resultItem.itemDescription;
    }

    public void OnCloseBtnClick()
    {
        gameObject.SetActive(false);
    }

    // ================================================================
    //  ▼ [추가] DBManager와 소통하기 위한 저장/불러오기 코드
    // ================================================================

        [Header("--- Save Data (DB 연결용) ---")]
    // 해금된 레시피들을 담아두는 리스트 (저장 대상)
    public List<EvolutionRecipe> unlockedRecipes = new List<EvolutionRecipe>();

    // 1. [저장] DBManager가 "데이터 내놔" 할 때 주는 함수
    public List<string> GetDogamSaveData()
    {
        List<string> saveList = new List<string>();

        foreach (var recipe in unlockedRecipes)
        {
            if (recipe != null)
                saveList.Add(recipe.name); // 레시피 파일 이름 저장
        }

        return saveList;
    }

    // 2. [로드] DBManager가 "데이터 받아" 할 때 복구하는 함수
    public void LoadDogamData(List<string> loadedNames)
    {
        if (loadedNames == null) return;

        unlockedRecipes.Clear(); // 리스트 초기화

        foreach (string name in loadedNames)
        {
            // 전체 레시피 리스트(allRecipes)에서 이름 똑같은 거 찾기
            EvolutionRecipe found = allRecipes.Find(x => x.name == name);

            if (found != null)
            {
                if (!unlockedRecipes.Contains(found))
                {
                    unlockedRecipes.Add(found);

                    // ★ [중요] GameProgressionManager한테도 해금됐다고 알려주기 (싱크 맞추기)
                    // (만약 GameProgressionManager에 UnlockItem 함수가 있다면 호출해주세요)
                    // 예: GameProgressionManager.Instance.UnlockItem(found.resultItem); 
                }
            }
        }

        Debug.Log($"도감 데이터 복구 완료! ({unlockedRecipes.Count}개)");

        // 화면 갱신 (현재 보고 있는 페이지 업데이트)
        if (categoryButtons != null && categoryButtons.Count > 0)
        {
            // 현재 보고 있는 카테고리 다시 로드해서 잠금 풀린 거 반영
            UpdateLeftPage();
        }
    }

    // 3. [보너스] 게임 플레이 중 진화 성공했을 때 호출할 함수
    public void UnlockRecipe(EvolutionRecipe newRecipe)
    {
        if (!unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
            // 저장 필요하면 여기서 DBManager.Instance.SaveAllData(...) 호출
        }
    }

}