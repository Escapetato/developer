using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI(Image)를 다루기 위해 꼭 필요

public class ItemSlot : MonoBehaviour
{
    public ItemData item;

    private Image itemIcon;

    void Awake()
    {
        // 슬롯의 Image 컴포넌트를 가져옴
        itemIcon = GetComponent<Image>();
    }
    void Start()
    {
        // 게임이 시작될 때, Inspector에 'item'이 할당되어 있으면
        // 그 아이템을 슬롯에 바로 표시
        if (item != null)
        {
            SetItem(item);
        }
        else
        {
            ClearSlot(); // 할당된 게 없으면 확실히 비움
        }
    }

    // 슬롯에 아이템을 세팅하는 함수 (아이콘과 색상 표시)
    public void SetItem(ItemData newItem)
    {
        item = newItem;

        if (item != null)
        {
            itemIcon.sprite = item.itemIcon;
            itemIcon.color = Color.white;
        }
        else
        {
            ClearSlot();
        }
    }

    // 슬롯을 비우는 함수 (아이콘 숨김)
    public void ClearSlot()
    {
        item = null;
        itemIcon.sprite = null;
        itemIcon.color = new Color(1, 1, 1, 0); // 투명하게
    }
}