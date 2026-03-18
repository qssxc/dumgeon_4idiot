using UnityEngine;

// [CreateAssetMenu]를 쓰면 에디터 우클릭 메뉴에서 아이템을 찍어낼 수 있습니다.
[CreateAssetMenu(fileName = "New Item", menuName = "Roguelike/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;      // 아이템 이름
    public Sprite icon;          // 아이템 그림
    public ItemType itemType;    // 아이템 종류

    // 나중에 효과 같은 것도 여기에 추가하면 됩니다 (예: 회복량)
}

public enum ItemType
{
    Potion, // 물약
    Gold,   // 돈
    Weapon  // 무기
}