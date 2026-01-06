using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FertilizerPopupUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI fertilizerCountText; // 비료 보유 개수
    public Image appliedFieldImage;             // 적용할 밭 이미지 (선택 사항)
    public TMP_InputField applyAmountInput;    // 넣을 비료 개수 입력 필드
    public Button confirmButton;               // 확인 버튼
    public Button closeButton;                 // X 버튼

    [Header("Stepper Buttons")]
    public Button increaseButton; // + 버튼
    public Button decreaseButton; // - 버튼

    private Field currentField;
    private ItemData fertilizerData; // 사용할 비료 아이템 데이터
    
    private int maxAvailableAmount = 0; // 현재 보유하고 있는 최대 비료 개수
    private int currentApplyAmount = 1; // 현재 설정된 비료 개수

    // UIManager에서 호출될 함수
    public void Show(Field field)
    {
        currentField = field;

        // [TODO] 인벤토리에서 비료 아이템 데이터와 보유 개수를 찾아서 설정
        // 이 예시에서는 ItemData.itemCategory가 "Fertilizer"인 아이템을 비료로 가정합니다.
        fertilizerData = GetFertilizerItemData(); // 비료 ItemData를 찾는 함수

        if (fertilizerData == null)
        {
            Debug.LogError("인벤토리에서 비료 아이템(카테고리: Fertilizer)을 찾을 수 없습니다.");
            return;
        }

        // 팝업 초기 상태 설정
        maxAvailableAmount = InventoryManager.Instance.GetItemCount(fertilizerData);
        fertilizerCountText.text = $"보유 비료: {maxAvailableAmount} 개";

        // 초기 개수 설정 및 UI 업데이트
        currentApplyAmount = 1;
        if (maxAvailableAmount == 0) currentApplyAmount = 0; // 비료가 없으면 0으로 시작

        UpdateApplyAmountUI(); // UI에 현재 개수 반영

        // 기존 확인/닫기 버튼 리스너 연결 
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClicked);
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => UIManager.Instance.CloseAllPopups());

        // 증가/감소 버튼 리스너 연결
        increaseButton.onClick.RemoveAllListeners();
        increaseButton.onClick.AddListener(() => ChangeApplyAmount(1)); // 1 증가
        decreaseButton.onClick.RemoveAllListeners();
        decreaseButton.onClick.AddListener(() => ChangeApplyAmount(-1)); // 1 감소

        applyAmountInput.onValueChanged.RemoveAllListeners();

        if (appliedFieldImage != null)
        {
            Sprite fieldSprite = field.GetFieldSprite(); // 밭에서 이미지를 가져옴

            if (fieldSprite == null)
            {
                Debug.LogError($"{field.name}으로부터 가져온 스프라이트가 null입니다! Field 인스펙터를 확인하세요.");
            }
            else
            {
                appliedFieldImage.sprite = fieldSprite;
                appliedFieldImage.enabled = true; // 혹시 꺼져있을지 모르니 켜줌
                Debug.Log($"{field.name}의 이미지를 팝업에 적용했습니다: {fieldSprite.name}");
            }
        }
    }
    // 개수 변경 로직
    private void ChangeApplyAmount(int delta)
    {
        int newAmount = currentApplyAmount + delta;

        // 1. 유효성 검사 (최소값 1, 최대값 maxAvailableAmount)
        newAmount = Mathf.Clamp(newAmount, 1, maxAvailableAmount);

        // (비료가 0개일 경우, 증가/감소 버튼을 비활성화하는 것이 더 좋지만, 로직 상으로는 0으로 고정)
        if (maxAvailableAmount == 0) newAmount = 0;

        if (newAmount != currentApplyAmount)
        {
            currentApplyAmount = newAmount;
            UpdateApplyAmountUI();
        }
    }
    // 입력 개수 유효성 검사 (보유 개수 초과 방지)
    private void ValidateInput(string input, int maxCount)
    {
        if (int.TryParse(input, out int amount))
        {
            if (amount < 1) applyAmountInput.text = "1";
            if (amount > maxCount) applyAmountInput.text = maxCount.ToString();
        }
    }
    // UI 업데이트 함수
    private void UpdateApplyAmountUI()
    {
        applyAmountInput.text = currentApplyAmount.ToString();
        
        // [선택 사항] 버튼 활성화/비활성화 시각적 피드백
        decreaseButton.interactable = (currentApplyAmount > 1);
        increaseButton.interactable = (currentApplyAmount < maxAvailableAmount);
        
        // 비료가 아예 없을 경우, 두 버튼 모두 비활성화
        if (maxAvailableAmount == 0)
        {
            decreaseButton.interactable = false;
            increaseButton.interactable = false;
        }
    }

    private void OnConfirmClicked()
    {
        if (currentField == null || fertilizerData == null) return;

        int amountToApply = currentApplyAmount;

        // 1. 입력 값 가져오기
        if (amountToApply <= 0)
        {
            Debug.Log("적용할 비료가 없습니다.");
            return;
        }

        // 2. 인벤토리에서 제거 및 밭에 적용
        InventoryManager.Instance.RemoveItem(fertilizerData, amountToApply);
        currentField.ApplyFertilizer(amountToApply);

        // 3. 팝업 닫기
        UIManager.Instance.CloseAllPopups();
        Debug.Log($"{amountToApply}개의 비료가 {currentField.name}에 적용되었습니다. 성장 속도 배율: {currentField.GetGrowthMultiplier()}");
    }
    
    // TODO: InventoryManager 또는 ItemDatabase에서 비료 ItemData를 찾아오는 실제 로직으로 대체해야 합니다.
    private ItemData GetFertilizerItemData()
    {
        // 이 부분은 인벤토리 구조에 따라 달라집니다. 
        // 여기서는 임시로 인벤토리의 첫 번째 비료 아이템을 가져온다고 가정합니다.
        foreach (var kv in InventoryManager.Instance.items)
        {
            if (kv.Key.itemCategory == "Fertilizer")
            {
                return kv.Key;
            }
        }
        return null;
    }
}