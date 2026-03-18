using UnityEngine;
using UnityEngine.InputSystem; // [필수] 뉴 인풋 시스템 네임스페이스 추가

public class PlayerController : MonoBehaviour
{
    private Vector2Int currentPos;

    public void Setup(Vector2Int startPos)
    {
        currentPos = startPos;
        // 타일 중앙(0.5)에 맞춰서 위치 잡기
        transform.position = new Vector3(startPos.x + 0.5f, startPos.y + 0.5f, -1);
    }

    private void Update()
    {
        // 키보드가 연결되어 있지 않으면 실행 안 함 (안전장치)
        if (Keyboard.current == null) return;

        // 뉴 인풋 시스템 방식 (wasPressedThisFrame == GetKeyDown)
        // 화살표(Arrow) 또는 WASD 키 둘 다 먹히게 설정

        // 위쪽 (Up / W)
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.up);
        }
        // 아래쪽 (Down / S)
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.down);
        }
        // 왼쪽 (Left / A)
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.left);
        }
        // 오른쪽 (Right / D)
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.right);
        }
    }
    public Vector2Int GetPosition()
    {
        return currentPos;
    }
    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetPos = currentPos + direction;

        // 싱글톤 인스턴스 접근
        if (DungeonGenerator.instance == null) return;

        int[,] map = DungeonGenerator.instance.mapGrid;

        // 맵 범위 체크
        if (targetPos.x < 0 || targetPos.x >= map.GetLength(0) ||
            targetPos.y < 0 || targetPos.y >= map.GetLength(1))
            return;

        // 바닥(1)인지 체크
        if (map[targetPos.x, targetPos.y] == 1)
        {
            currentPos = targetPos;
            // 이동할 때도 0.5f를 더해서 중앙 맞춤 유지
            transform.position = new Vector3(currentPos.x + 0.5f, currentPos.y + 0.5f, -1);

            // [추가] 이동 성공 시 안개 걷어내기
            if (FogOfWar.instance != null)
            {
                FogOfWar.instance.UpdateFog(currentPos);
            }
        }
        
        CheckItemAtFeet();

        if (DungeonGenerator.instance != null)
        {
            DungeonGenerator.instance.ProcessEnemyTurns(currentPos);
        }
    }
    private void CheckItemAtFeet()
    {
        Collider2D hit = Physics2D.OverlapPoint(transform.position);

        if (hit != null)
        {
            ItemPickup pickup = hit.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                // [수정됨] 인벤토리에 추가 시도
                bool wasPickedUp = InventoryManager.instance.Add(pickup.data);

                if (wasPickedUp)
                {
                    Debug.Log($"획득: {pickup.data.itemName}");
                    Destroy(pickup.gameObject); // 가방에 넣었으니 필드에서는 삭제
                }
            }
        }
    }
}
