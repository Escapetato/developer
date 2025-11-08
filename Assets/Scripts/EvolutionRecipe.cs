using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 'Create' 메뉴에서 쉽게 만들 수 있도록 메뉴 항목 추가
[CreateAssetMenu(fileName = "NewEvolutionRecipe", menuName = "Data/Evolution Recipe")]
public class EvolutionRecipe : ScriptableObject
{
    [Header("진화 재료")]
    public ItemData material; // (이 스크립트는 ItemData.cs가 필요합니다)
    public ItemData potion;   // (이것도 ItemData.cs가 필요합니다)

    [Header("진화 결과")]
    public ItemData resultItem; // 성공 시 결과물

    [Header("진화 비용 및 확률")]
    public int evolutionCost = 100; // 진화 비용

    [Range(0f, 1f)]
    public float successChance = 0.7f; // 성공 확률 (예: 0.7 = 70%)
}
