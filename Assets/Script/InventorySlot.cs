using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;       // 아이템 아이콘 이미지 연결
    ItemData item;           // 이 슬롯에 들어있는 아이템 데이터

    public void AddItem(ItemData newItem)
    {
        item = newItem;
        icon.sprite = item.icon;
        icon.enabled = true; // 아이콘 보이게
    }

    public void ClearSlot()
    {
        item = null;
        icon.sprite = null;
        icon.enabled = false; // 빈 슬롯이면 아이콘 숨김
    }

    // 버튼을 눌렀을 때 실행될 함수 (인스펙터의 Button OnClick에 연결하거나 코드로 연결)
    public void UseItem()
    {
        if (item != null)
        {
            Debug.Log($"아이템 사용: {item.itemName}");
            // TODO: 실제 효과 구현 (체력 회복 등)

            // 사용했으니 가방에서 제거
            InventoryManager.instance.Remove(item);
        }
    }
}