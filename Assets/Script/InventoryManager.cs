using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    public int capacity = 20; // 가방 칸 수
    public List<ItemData> items = new List<ItemData>(); // 획득한 아이템 리스트

    // UI 갱신을 알리기 위한 델리게이트 (이벤트)
    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    private void Awake()
    {
        instance = this;
    }

    // 아이템 추가
    public bool Add(ItemData item)
    {
        if (items.Count >= capacity)
        {
            Debug.Log("가방이 꽉 찼습니다!");
            return false;
        }

        items.Add(item);

        // UI에게 "내용 바뀌었으니 다시 그려!"라고 알림
        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();

        return true;
    }

    // 아이템 제거 (사용하거나 버릴 때)
    public void Remove(ItemData item)
    {
        items.Remove(item);

        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
    }
}