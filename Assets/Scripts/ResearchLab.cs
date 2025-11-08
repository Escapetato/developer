using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;

// ResearchLab.cs
public class ResearchLab : MonoBehaviour
{
    // 1. 레시피(진화 공식) 데이터 (ScriptableObject)
    public EvolutionRecipe currentRecipe; // (테스트용 레시피를 Inspector에서 연결)

    // 2. UI 슬롯 연결
    public ItemSlot materialSlot; // (재료1 슬롯 UI)
    public ItemSlot potionSlot;   // (재료2 슬롯 UI)
    public ItemSlot resultSlot;   // 결과물이 보일 UI 슬롯

    // '진화' 버튼이 OnClick() 이벤트로 호출할 함수
    public void OnEvolutionButtonClick()
    {
        // --- 1. 사전 조건 검사 (재료가 있는가?) ---
        if (materialSlot.item == null || potionSlot.item == null)
        {
            Debug.LogWarning("재료가 부족합니다"); // 콘솔에 경고 출력
            return;
        }

        // 슬롯의 아이템이 'currentRecipe'의 재료와 일치하는지 검사
        // (ScriptableObject는 == 로 비교 가능)
        bool isRecipeCorrect = (materialSlot.item == currentRecipe.material) &&
                               (potionSlot.item == currentRecipe.potion);

        // a. 작물 소멸 (UI에서만 제거)
        materialSlot.ClearSlot();
        // b. 에너지(포션) 소멸 (UI에서만 제거)
        potionSlot.ClearSlot();
        // c. 골드 소멸 (테스트용 로그)
        if (currentRecipe != null) // (Null 에러 방지)
        {
            Debug.Log(currentRecipe.evolutionCost + " 골드 차감 시도!");
        }

        if (isRecipeCorrect)
        {
            // --- [성공] 시 구현 ---
            ItemData newItem = currentRecipe.resultItem;

            // 새로운 아이템을 '결과 슬롯' UI에 표시
            resultSlot.SetItem(newItem);

            // 도감에 추가 (테스트용 로그)
            Debug.Log("도감에 " + newItem.itemName + " 추가");

            // 성공 팝업 (테스트용 로그)
            Debug.Log("진화 성공: " + newItem.itemName);
        }
        else
        {
            // --- [실패] 시 구현 ---
            // (레시피가 일치하지 않음)

            // 실패 팝업 (테스트용 로그)
            Debug.Log("진화 실패... (재료/골드 모두 소멸됨)");

            // 결과 슬롯 비우기
            resultSlot.ClearSlot();
        }
    }
}