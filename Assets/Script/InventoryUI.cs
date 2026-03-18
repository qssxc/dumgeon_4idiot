using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public Transform itemsParent;   // SlotContainer 연결
    public GameObject slotPrefab;   // Slot 프리팹 연결
    public GameObject inventoryPanelObject;

    InventoryManager inventory;
    InventorySlot[] slots;

    void Start()
    {
        inventory = InventoryManager.instance;
        // 매니저의 데이터가 변할 때마다 UpdateUI 함수가 실행되도록 구독
        inventory.onItemChangedCallback += UpdateUI;

        // 처음에 슬롯 미리 생성 (20개)
        for (int i = 0; i < inventory.capacity; i++)
        {
            Instantiate(slotPrefab, itemsParent);
        }

        // 생성된 슬롯들을 배열로 가져오기
        slots = itemsParent.GetComponentsInChildren<InventorySlot>(true);

        UpdateUI();
    }
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
        {
            inventoryPanelObject.SetActive(!inventoryPanelObject.activeSelf);
        }
    }

    void UpdateUI()
    {
        // 현재 아이템 리스트를 순회하며 슬롯 채우기
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventory.items.Count)
            {
                slots[i].AddItem(inventory.items[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }
}