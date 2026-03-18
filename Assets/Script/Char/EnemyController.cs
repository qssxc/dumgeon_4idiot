using Unity.VisualScripting;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public EnemyData data;
    private Vector2Int currentPos;
    private int currentHp;

    // [추가] 내 모습을 끄고 켤 스프라이트 렌더러
    private SpriteRenderer sr;

    public void Setup(Vector2Int startPos, EnemyData initData)
    {
        data = initData;
        currentHp = data.maxHp;

        // SpriteRenderer 가져오기 및 이미지 적용
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = data.sprite; // (주의: data에 만들어둔 변수 이름이 sprite면 data.sprite로 쓰세요!)

        currentPos = startPos;
        transform.position = new Vector3(startPos.x + 0.5f, startPos.y + 0.5f, -1);
        gameObject.name = $"Enemy_{data.enemyName}";

        // [추가] 안개 갱신 방송 구독하기!
        if (FogOfWar.instance != null)
        {
            FogOfWar.instance.onFogUpdated += UpdateVisibility;
        }

        // 태어나자마자 한 번 체크
        UpdateVisibility();
    }

    // [추가] 몬스터가 죽으면 꼭 구독을 취소해야 에러가 안 납니다.
    private void OnDestroy()
    {
        if (FogOfWar.instance != null)
        {
            FogOfWar.instance.onFogUpdated -= UpdateVisibility;
        }
    }

    // [추가] 내가 현재 안개 속에 있는지 체크하고 모습을 숨기거나 드러냄
    private void UpdateVisibility()
    {
        if (FogOfWar.instance != null && sr != null)
        {
            sr.enabled = FogOfWar.instance.IsVisible(currentPos);
        }
    }

    public void TakeTurn(Vector2Int playerPos)
    {
        int dist = Mathf.Abs(playerPos.x - currentPos.x) + Mathf.Abs(playerPos.y - currentPos.y);

        if (dist <= data.viewRange)
        {
            MoveTowards(playerPos);
        }
        else
        {
            MoveRandomly();
        }
    }

    private void MoveTowards(Vector2Int target)
    {
        int dx = target.x - currentPos.x;
        int dy = target.y - currentPos.y;
        Vector2Int direction = Vector2Int.zero;
        if (Mathf.Abs(dx) > Mathf.Abs(dy)) direction = (dx > 0) ? Vector2Int.right : Vector2Int.left;
        else direction = (dy > 0) ? Vector2Int.up : Vector2Int.down;
        TryMove(direction);
    }

    private void MoveRandomly()
    {
        int rand = Random.Range(0, 4);
        Vector2Int dir = Vector2Int.zero;
        if (rand == 0) dir = Vector2Int.up;
        else if (rand == 1) dir = Vector2Int.down;
        else if (rand == 2) dir = Vector2Int.left;
        else if (rand == 3) dir = Vector2Int.right;
        TryMove(dir);
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetPos = currentPos + direction;
        int[,] map = DungeonGenerator.instance.mapGrid;
        if (targetPos.x < 0 || targetPos.x >= map.GetLength(0) || targetPos.y < 0 || targetPos.y >= map.GetLength(1)) return;
        if (map[targetPos.x, targetPos.y] == 0) return;

        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player.GetPosition() == targetPos) return;

        currentPos = targetPos;
        transform.position = new Vector3(currentPos.x + 0.5f, currentPos.y + 0.5f, -1);

        // [추가] 내가 움직였으니 시야 갱신
        UpdateVisibility();
    }
}