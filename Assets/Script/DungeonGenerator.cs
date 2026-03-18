using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{

    [Header("타일맵 설정")]
    public Tilemap tilemap;   // 에디터에서 연결할 타일맵
    public TileBase floorTile; // 바닥으로 쓸 타일 에셋
    public TileBase wallTile;  // 벽으로 쓸 타일 에셋 (선택사항)

    [Header("맵 설정")]
    public int totalWidth = 50;
    public int totalHeight = 50;
    public int minNodeSize = 10;
    public int minRoomWidth = 6;
    public int minRoomHeight = 6;
    [Range(0f, 0.5f)] public float roomMargin = 0.1f; // 방과 방 사이 여백 비율

    [Header("아이템 설정")]
    public GameObject itemPrefab;    // 아까 만든 ItemObject 프리팹
    public ItemData[] itemTable;     // 생성할 아이템 데이터들 (물약, 골드 등)
    [Range(0, 100)] public int itemSpawnChance = 20; // 각 방에 아이템이 나올 확률 (%)

    [Header("프리팹 설정")]
    public GameObject playerPrefab;

    [Header("몬스터 설정")]
    public GameObject enemyBasePrefab; // 껍데기 프리팹 (EnemyController가 붙은)
    public EnemyData[] enemyTypes;     // 소환 가능한 몬스터 종류들 (Rat, Slime...)
    public List<EnemyController> enemies = new List<EnemyController>();

    // 맵 데이터 (0: 벽, 1: 바닥)
    public int[,] mapGrid;

    private List<Node> allNodes = new List<Node>();
    private List<RectInt> allRooms = new List<RectInt>();
    private List<RectInt> corridors = new List<RectInt>();

    public static DungeonGenerator instance;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        // 1. 데이터 초기화
        mapGrid = new int[totalWidth, totalHeight];
        allNodes.Clear();
        allRooms.Clear();
        corridors.Clear();

        // 2. BSP 공간 분할
        Node rootNode = new Node(new RectInt(0, 0, totalWidth, totalHeight));
        allNodes.Add(rootNode);
        SplitNode(rootNode);

        // 3. 방 생성 및 복도 연결
        CreateRooms();
        GenerateCorridors(rootNode);

        // 4. [핵심] 맵 배열에 굽기 (Bake)
        BakeMap();

        DrawTilemap();

        SpawnPlayer();

        SpawnItems();

        SpawnEnemies();

        if (FogOfWar.instance != null)
        {
            FogOfWar.instance.SetupFog(totalWidth, totalHeight);

            // 시작 위치도 바로 밝혀줘야 함
            // 플레이어를 찾아서 위치 업데이트
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) FogOfWar.instance.UpdateFog(pc.GetPosition());
        }
    }

    private void SplitNode(Node node)
    {
        if (node.nodeRect.width < minNodeSize * 2 && node.nodeRect.height < minNodeSize * 2) return;

        bool splitHorizontal = Random.value > 0.5f;
        if (node.nodeRect.width < minNodeSize * 2) splitHorizontal = true;
        if (node.nodeRect.height < minNodeSize * 2) splitHorizontal = false;

        int splitPoint;
        Node node1 = null;
        Node node2 = null;

        if (splitHorizontal)
        {
            splitPoint = Random.Range(minNodeSize, node.nodeRect.height - minNodeSize);
            node1 = new Node(new RectInt(node.nodeRect.x, node.nodeRect.y, node.nodeRect.width, splitPoint));
            node2 = new Node(new RectInt(node.nodeRect.x, node.nodeRect.y + splitPoint, node.nodeRect.width, node.nodeRect.height - splitPoint));
        }
        else
        {
            splitPoint = Random.Range(minNodeSize, node.nodeRect.width - minNodeSize);
            node1 = new Node(new RectInt(node.nodeRect.x, node.nodeRect.y, splitPoint, node.nodeRect.height));
            node2 = new Node(new RectInt(node.nodeRect.x + splitPoint, node.nodeRect.y, node.nodeRect.width - splitPoint, node.nodeRect.height));
        }

        node.left = node1;
        node.right = node2;
        allNodes.Add(node1);
        allNodes.Add(node2);

        SplitNode(node1);
        SplitNode(node2);
    }

    private void CreateRooms()
    {
        foreach (Node node in allNodes)
        {
            if (node.left == null && node.right == null)
            {
                // 방 크기를 공간에 꽉 채우지 않고 약간의 여백을 둠
                int width = (int)(node.nodeRect.width * (1f - roomMargin));
                int height = (int)(node.nodeRect.height * (1f - roomMargin));

                // 최소 크기 보장
                if (width < minRoomWidth) width = minRoomWidth;
                if (height < minRoomHeight) height = minRoomHeight;

                // 중앙 정렬
                int x = node.nodeRect.x + (node.nodeRect.width - width) / 2;
                int y = node.nodeRect.y + (node.nodeRect.height - height) / 2;

                RectInt room = new RectInt(x, y, width, height);
                node.roomRect = room;
                allRooms.Add(room);
            }
        }
    }

    private void GenerateCorridors(Node node)
    {
        if (node.left == null || node.right == null) return;

        Vector2Int leftCenter = node.left.GetCenter();
        Vector2Int rightCenter = node.right.GetCenter();

        // 코너 부분 연결을 확실하게 하기 위해 너비를 살짝 여유있게 잡을 수도 있음
        CreateCorridor(leftCenter, rightCenter);

        GenerateCorridors(node.left);
        GenerateCorridors(node.right);
    }

    private void CreateCorridor(Vector2Int pos1, Vector2Int pos2)
    {
        int x = Mathf.Min(pos1.x, pos2.x);
        int y = Mathf.Min(pos1.y, pos2.y);
        int w = Mathf.Abs(pos1.x - pos2.x);
        int h = Mathf.Abs(pos1.y - pos2.y);

        // ㄱ자 모양으로 연결 (랜덤 방향)
        if (Random.value > 0.5f)
        {
            // 가로 -> 세로
            corridors.Add(new RectInt(x, pos1.y, w + 1, 1));
            corridors.Add(new RectInt(pos2.x, y, 1, h + 1));
        }
        else
        {
            // 세로 -> 가로
            corridors.Add(new RectInt(pos1.x, y, 1, h + 1));
            corridors.Add(new RectInt(x, pos2.y, w + 1, 1));
        }
    }

    // [중요] 모든 방과 복도를 하나의 맵 배열에 합치는 함수
    private void BakeMap()
    {
        // 1. 방 뚫기 (1로 채움)
        foreach (RectInt room in allRooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    if (IsInRange(x, y)) mapGrid[x, y] = 1;
                }
            }
        }

        // 2. 복도 뚫기 (1로 채움 - 이미 방인 곳은 덮어씀)
        foreach (RectInt corridor in corridors)
        {
            for (int x = corridor.x; x < corridor.x + corridor.width; x++)
            {
                for (int y = corridor.y; y < corridor.y + corridor.height; y++)
                {
                    if (IsInRange(x, y)) mapGrid[x, y] = 1;
                }
            }
        }
    }

    private bool IsInRange(int x, int y)
    {
        return x >= 0 && x < totalWidth && y >= 0 && y < totalHeight;
    }

    // Node 클래스용 GetCenter (이전과 동일)
    // ... Node 클래스는 파일 하단에 그대로 두시면 됩니다.
    private void DrawTilemap()
    {
        // 기존에 그려진 타일이 있다면 싹 지웁니다.
        tilemap.ClearAllTiles();

        for (int x = 0; x < totalWidth; x++)
        {
            for (int y = 0; y < totalHeight; y++)
            {
                if (mapGrid[x, y] == 1)
                {
                    // 바닥(1)이면 해당 좌표에 바닥 타일을 깐다
                    tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
                else
                {
                    // 벽(0)이면 벽 타일을 깐다 (벽 타일이 있다면)
                    if (wallTile != null)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                    }
                }
            }
        }
    }

    private void SpawnPlayer()
    {
        if (allRooms.Count == 0) return;

        RectInt startRoom = allRooms[0];
        Vector2Int spawnPos = new Vector2Int(
            startRoom.x + startRoom.width / 2,
            startRoom.y + startRoom.height / 2
        );


        // 플레이어 생성
        GameObject player = Instantiate(playerPrefab, new Vector3(spawnPos.x + 0.5f, spawnPos.y + 0.5f, -1), Quaternion.identity);

        // [추가됨] PlayerController를 가져와서 초기 위치를 알려줌
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.Setup(spawnPos);
        }

        // 카메라 설정
        Camera.main.transform.position = new Vector3(spawnPos.x, spawnPos.y, -10);
        Camera.main.transform.parent = player.transform;
    }

    private void SpawnItems()
    {
        // 기존에 떨어진 아이템이 있다면 싹 지우기 (재시작 시 필요)
        //ItemPickup[] oldItems = FindAnyObjectByType<ItemPickup>();
        //foreach (var item in oldItems) Destroy(item.gameObject);

        // 첫 번째 방(시작 방)은 제외하고 루프 시작 (1부터 시작)
        for (int i = 1; i < allRooms.Count; i++)
        {
            // 확률 체크
            if (Random.Range(0, 100) < itemSpawnChance)
            {
                RectInt room = allRooms[i];

                // 방 안의 랜덤 좌표 구하기
                int x = Random.Range(room.x, room.x + room.width);
                int y = Random.Range(room.y, room.y + room.height);

                // 랜덤 아이템 데이터 하나 뽑기
                ItemData randomItem = itemTable[Random.Range(0, itemTable.Length)];

                // 아이템 생성 (프리팹 복제)
                GameObject newItem = Instantiate(itemPrefab);

                // 데이터 주입 및 위치 설정 (타일 중앙 0.5f)
                newItem.GetComponent<ItemPickup>().Setup(randomItem, new Vector3(x + 0.5f, y + 0.5f, -1));
            }
        }

    }
    private void SpawnEnemies()
    {
        // 기존 삭제 로직 동일
        foreach (var enemy in enemies) { if (enemy != null) Destroy(enemy.gameObject); }
        enemies.Clear();

        for (int i = 1; i < allRooms.Count; i++)
        {
            if (Random.value > 0.5f)
            {
                RectInt room = allRooms[i];
                int x = Random.Range(room.x, room.x + room.width);
                int y = Random.Range(room.y, room.y + room.height);

                // [핵심] 랜덤한 몬스터 데이터 하나 뽑기
                EnemyData randomData = enemyTypes[Random.Range(0, enemyTypes.Length)];

                // 껍데기 소환
                GameObject newEnemy = Instantiate(enemyBasePrefab);

                // 데이터 주입하면서 초기화
                EnemyController ec = newEnemy.GetComponent<EnemyController>();
                ec.Setup(new Vector2Int(x, y), randomData);

                enemies.Add(ec);
            }
        }
    }
    public void ProcessEnemyTurns(Vector2Int playerPos)
    {
        foreach (EnemyController enemy in enemies)
        {
            enemy.TakeTurn(playerPos);
        }
    }
}
public class Node
{
    public RectInt nodeRect;
    public RectInt roomRect;
    public Node left;
    public Node right;

    public Node(RectInt rect)
    {
        this.nodeRect = rect;
    }

    // [추가된 함수] 이 노드의 중심점(방의 중심)을 찾는 재귀 함수
    public Vector2Int GetCenter()
    {
        // 내가 말단 노드(Leaf)라면 내 방의 중심 반환
        if (right == null && left == null)
            return new Vector2Int(roomRect.x + roomRect.width / 2, roomRect.y + roomRect.height / 2);

        // 내가 부모라면 자식 중 하나의 중심을 내 중심인 척 반환
        // (오른쪽 자식의 중심을 따라가면 결국 방을 만나게 됨)
        return right.GetCenter();
    }
}