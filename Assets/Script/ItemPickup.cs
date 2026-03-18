using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData data; // 이 아이템이 무슨 아이템인지 담을 변수

    // 데이터를 받아서 스프라이트를 바꿔주는 초기화 함수
    public void Setup(ItemData initData, Vector3 position)
    {
        data = initData;

        // 스프라이트 변경
        GetComponent<SpriteRenderer>().sprite = data.icon;

        // 위치 설정 (타일 중앙에 오게)
        transform.position = position;
    }
}