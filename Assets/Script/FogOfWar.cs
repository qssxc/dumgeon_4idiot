using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class FogOfWar : MonoBehaviour
{
    public static FogOfWar instance;
    public event Action onFogUpdated;

    [Header("타일맵 연결")]
    public Tilemap unexploredMap; // FogMap_Black (완전 어둠)
    public Tilemap shadowMap;     // FogMap_Shadow (회색 그림자)

    [Header("설정")]
    public TileBase fogTile;
    public TileBase shadowTile;   // 회색 반투명 타일 (없으면 fogTile 사용)
    public int viewRadius = 5;

    // ── SPD에서 이식: 4단계 상태 ──────────────────────────────
    private const int VISIBLE = 0;
    private const int VISITED = 1;
    private const int MAPPED = 2;
    private const int INVISIBLE = 3;

    // 셀 단위 상태 배열 (SPD의 visible[], visited[], mapped[] 통합)
    private int[] cellState;      // 현재 상태
    private int[] prevCellState;  // 이전 프레임 상태 (Dirty 체크용)

    private int mapWidth, mapHeight;

    // ── 업데이트 요청 큐 (SPD의 Dirty Rect 방식) ──────────────
    private bool fullUpdateRequested = false;
    private HashSet<int> dirtySet = new HashSet<int>(); // 갱신 필요한 셀 인덱스

    private void Awake()
    {
        instance = this;
    }

    // ── 초기화 ────────────────────────────────────────────────
    public void SetupFog(int width, int height)
    {
        this.mapWidth = width;
        this.mapHeight = height;

        int len = width * height;
        cellState = new int[len];
        prevCellState = new int[len];

        // 전부 INVISIBLE로 초기화
        for (int i = 0; i < len; i++)
        {
            cellState[i] = INVISIBLE;
            prevCellState[i] = INVISIBLE;
        }

        unexploredMap.ClearAllTiles();
        shadowMap.ClearAllTiles();

        // 맵 전체 + 여백을 안개로 덮음
        int margin = viewRadius + 2;
        for (int x = -margin; x < width + margin; x++)
            for (int y = -margin; y < height + margin; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                unexploredMap.SetTile(pos, fogTile);
                shadowMap.SetTile(pos, fogTile);
            }
    }

    // ── 외부 호출: 플레이어 이동 시 ───────────────────────────
    public void UpdateFog(Vector2Int playerPos)
    {
        if (cellState == null) return;

        // 1. 이전 VISIBLE → VISITED로 강등
        for (int i = 0; i < cellState.Length; i++)
        {
            if (cellState[i] == VISIBLE)
            {
                cellState[i] = VISITED;
            }
        }

        // 2. 브레즌햄 광선으로 현재 시야 VISIBLE 처리
        RevealByRaycasting(playerPos);

        // 3. SPD 방식: 상태가 바뀐 셀만 SetTile
        ApplyFogToTilemaps();

        onFogUpdated?.Invoke();
    }

    // ── 광선 시야 계산 (기존 로직 유지) ──────────────────────
    private void RevealByRaycasting(Vector2Int playerPos)
    {
        // 플레이어 자신은 무조건 VISIBLE
        SetCellState(playerPos.x, playerPos.y, VISIBLE);

        // 테두리를 향해 광선
        for (int x = -viewRadius; x <= viewRadius; x++)
            for (int y = -viewRadius; y <= viewRadius; y++)
            {
                if (Mathf.Abs(x) == viewRadius || Mathf.Abs(y) == viewRadius)
                {
                    Vector2Int target = playerPos + new Vector2Int(x, y);
                    CastRayAndReveal(playerPos, target);
                }
            }
    }

    private void CastRayAndReveal(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = GetBresenhamLine(start, end);

        foreach (Vector2Int point in line)
        {
            if (!InBounds(point)) break;
            if (Vector2Int.Distance(start, point) > viewRadius) break;

            SetCellState(point.x, point.y, VISIBLE);

            // ── SPD 이식: 벽이면 광선 차단 ──────────────────
            if (IsWall(point.x, point.y)) break;
        }
    }

    // ── SPD 핵심: 상태 변경 + Dirty 마킹 ────────────────────
    private void SetCellState(int x, int y, int state)
    {
        int idx = y * mapWidth + x;
        // 이미 같거나 더 높은 우선순위 상태면 덮어쓰지 않음
        // (VISIBLE > VISITED > MAPPED > INVISIBLE, 숫자가 작을수록 높은 우선순위)
        if (state < cellState[idx])
        {
            cellState[idx] = state;
        }
    }

    // ── SPD 방식: Dirty 체크 후 SetTile ──────────────────────
    private void ApplyFogToTilemaps()
    {
        for (int idx = 0; idx < cellState.Length; idx++)
        {
            // 상태가 변하지 않았으면 SetTile 호출 생략 (핵심 최적화)
            if (cellState[idx] == prevCellState[idx]) continue;

            int x = idx % mapWidth;
            int y = idx / mapWidth;
            Vector3Int tilePos = new Vector3Int(x, y, 0);

            int state = cellState[idx];

            // ── SPD 이식: 벽 타일 인접 셀 참조 처리 ──────────
            if (IsWall(x, y))
            {
                state = GetWallFogState(x, y);
            }

            ApplyStateToTile(tilePos, state);

            prevCellState[idx] = cellState[idx];
        }
    }

    // ── SPD의 벽 좌우 분할 로직을 Unity 단일 타일에 맞게 이식 ──
    // 벽 타일은 자신 + 인접 셀 중 가장 어두운 값을 사용
    private int GetWallFogState(int x, int y)
    {
        int self = cellState[y * mapWidth + x];

        // 아래 셀 참조 (카메라 방향 벽)
        if (InBounds(x, y - 1))
        {
            int below = cellState[(y - 1) * mapWidth + x];
            self = Mathf.Max(self, below); // 숫자 클수록 어두움
        }

        // 좌우 인접 셀 참조
        if (InBounds(x - 1, y))
        {
            int left = cellState[y * mapWidth + (x - 1)];
            // 왼쪽도 벽이면 대각선 아래까지 확인
            if (IsWall(x - 1, y) && InBounds(x - 1, y - 1))
            {
                int belowLeft = cellState[(y - 1) * mapWidth + (x - 1)];
                self = Mathf.Max(self, Mathf.Max(left, belowLeft));
            }
            else
            {
                self = Mathf.Max(self, left);
            }
        }

        if (InBounds(x + 1, y))
        {
            int right = cellState[y * mapWidth + (x + 1)];
            if (IsWall(x + 1, y) && InBounds(x + 1, y - 1))
            {
                int belowRight = cellState[(y - 1) * mapWidth + (x + 1)];
                self = Mathf.Max(self, Mathf.Max(right, belowRight));
            }
            else
            {
                self = Mathf.Max(self, right);
            }
        }

        return self;
    }

    // ── 타일맵에 상태 반영 ────────────────────────────────────
    private void ApplyStateToTile(Vector3Int pos, int state)
    {
        switch (state)
        {
            case VISIBLE:
                unexploredMap.SetTile(pos, null); // 완전 탐험됨 → 영구 제거
                shadowMap.SetTile(pos, null);     // 현재 시야 → 제거
                break;

            case VISITED:
                // unexploredMap은 이미 제거된 상태 유지
                shadowMap.SetTile(pos, shadowTile != null ? shadowTile : fogTile);
                break;

            case MAPPED:
                // MAPPED는 게임에 따라 커스텀 타일 사용 가능
                shadowMap.SetTile(pos, shadowTile != null ? shadowTile : fogTile);
                break;

            case INVISIBLE:
                unexploredMap.SetTile(pos, fogTile);
                shadowMap.SetTile(pos, fogTile);
                break;
        }
    }

    // ── 유틸 ──────────────────────────────────────────────────
    private bool IsWall(int x, int y)
    {
        return DungeonGenerator.instance.mapGrid[x, y] == 0;
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
    }

    private bool InBounds(Vector2Int p) => InBounds(p.x, p.y);

    public bool IsVisible(Vector2Int pos)
    {
        if (!InBounds(pos.x, pos.y)) return false;
        return cellState[pos.y * mapWidth + pos.x] == VISIBLE;
    }

    public bool IsExplored(Vector2Int pos)
    {
        if (!InBounds(pos.x, pos.y)) return false;
        return cellState[pos.y * mapWidth + pos.x] <= VISITED;
    }

    // ── 브레즌햄 (기존 동일) ──────────────────────────────────
    private List<Vector2Int> GetBresenhamLine(Vector2Int start, Vector2Int end)
    {
        var points = new List<Vector2Int>();
        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;

        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            points.Add(new Vector2Int(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
        return points;
    }
}