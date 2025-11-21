using UnityEngine;

// 'Create' 메뉴에서 쉽게 만들 수 있도록 설정
[CreateAssetMenu(fileName = "NewEvolutionRecipe", menuName = "Data/Evolution Recipe")]
public class EvolutionRecipe : ScriptableObject
{
    [Header("진화 재료")]
    public ItemData material; // 예: 멜론 (베이스 작물)
    public ItemData potion;   // 예: 물약

    [Header("진화 결과")]
    public ItemData resultItem; // 예: 화채 (결과물)

    [Header("진화 설정")]
    public int evolutionCost = 100; // 진화 비용

}