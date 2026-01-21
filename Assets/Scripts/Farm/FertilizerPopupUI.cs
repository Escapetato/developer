using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FertilizerPopupUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI fertilizerCountText;
    public TextMeshProUGUI remainingTimeText;
    public Image appliedFieldImage;
    public TMP_InputField applyAmountInput;
    public Button confirmButton;
    public Button closeButton;

    [Header("Stepper Buttons")]
    public Button increaseButton;
    public Button decreaseButton;

    private Field currentField;
    private ItemData fertilizerData;

    private int maxAvailableAmount = 0;
    private int currentApplyAmount = 1;

    public void Show(Field field)
    {
        currentField = field;
        fertilizerData = GetFertilizerItemData();

        if (fertilizerData == null)
        {
            Debug.LogError("비료 아이템을 찾을 수 없습니다.");
            // 비료가 없으면 확인 버튼을 비활성화하거나 안내 메시지 출력
            confirmButton.interactable = false;
            maxAvailableAmount = 0;
        }
        else
        {
            maxAvailableAmount = InventoryManager.Instance.GetItemCount(fertilizerData);
            confirmButton.interactable = maxAvailableAmount > 0;
        }

        // [수정] .ToString() 추가 (숫자를 텍스트로 변환)
        fertilizerCountText.text = maxAvailableAmount.ToString();

        currentApplyAmount = maxAvailableAmount > 0 ? 1 : 0;
        UpdateApplyAmountUI();

        // 버튼 리스너 재설정 (확실하게 하기 위해)
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClicked);

        UpdateRemainingTime();

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            Debug.Log("닫기 버튼 클릭됨");
            gameObject.SetActive(false); // 직접 끄거나 UIManager 호출
            // UIManager.Instance.CloseAllPopups(); // 원래 사용하던 방식 유지
        });

        increaseButton.onClick.RemoveAllListeners();
        increaseButton.onClick.AddListener(() => ChangeApplyAmount(1));

        decreaseButton.onClick.RemoveAllListeners();
        decreaseButton.onClick.AddListener(() => ChangeApplyAmount(-1));

        // InputField 직접 입력 대응
        applyAmountInput.onEndEdit.RemoveAllListeners();
        applyAmountInput.onEndEdit.AddListener((val) => ValidateInput(val, maxAvailableAmount));

        if (appliedFieldImage != null)
        {
            Sprite fieldSprite = field.GetFieldSprite();

            if (fieldSprite != null)
            {
                appliedFieldImage.sprite = fieldSprite;
                appliedFieldImage.enabled = true; // 컴포넌트가 꺼져있는지 확인

                // UI Image의 경우 투명도(Alpha)가 0이면 안 보입니다.
                Color c = appliedFieldImage.color;
                c.a = 1f;
                appliedFieldImage.color = c;

                Debug.Log($"팝업에 이미지 적용 성공: {fieldSprite.name}");
            }
            else
            {
                Debug.LogWarning("밭에서 가져올 이미지가 없습니다!");
                appliedFieldImage.enabled = false;
            }
        }
    }

    private void ChangeApplyAmount(int delta)
    {
        if (maxAvailableAmount == 0) return;
        currentApplyAmount = Mathf.Clamp(currentApplyAmount + delta, 1, maxAvailableAmount);
        UpdateApplyAmountUI();
    }

    private void ValidateInput(string input, int maxCount)
    {
        if (int.TryParse(input, out int amount))
        {
            currentApplyAmount = Mathf.Clamp(amount, 1, maxCount);
        }
        else
        {
            currentApplyAmount = 1;
        }
        UpdateApplyAmountUI();
    }

    private void UpdateApplyAmountUI()
    {
        applyAmountInput.text = currentApplyAmount.ToString();
        decreaseButton.interactable = (currentApplyAmount > 1);
        increaseButton.interactable = (currentApplyAmount < maxAvailableAmount);
    }

    private void OnConfirmClicked()
    {
        if (currentField == null || fertilizerData == null || currentApplyAmount <= 0) return;

        // 적용 로직
        InventoryManager.Instance.RemoveItem(fertilizerData, currentApplyAmount);
        currentField.ApplyFertilizer(currentApplyAmount);

        Debug.Log($"{currentApplyAmount}개 비료 적용 완료");
        gameObject.SetActive(false); // 팝업 닫기
    }

    private ItemData GetFertilizerItemData()
    {
        foreach (var kv in InventoryManager.Instance.items)
        {
            if (kv.Key.itemCategory == "Fertilizer") return kv.Key;
        }
        return null;
    }
    private void Update()
    {
        if (currentField != null && gameObject.activeSelf)
        {
            UpdateRemainingTime();
        }
    }
    private void UpdateRemainingTime()
    {
        if (currentField != null && remainingTimeText != null)
        {
            remainingTimeText.text = currentField.GetRemainingTimeText();
        }
    }
}