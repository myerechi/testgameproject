using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class SurvivorGame : MonoBehaviour
{
    private struct SidePlatform
    {
        public float Left;
        public float Right;
        public float Top;
    }

    private struct SideBlocker
    {
        public float Left;
        public float Right;
        public float Bottom;
        public float Top;
    }

    public static SurvivorGame Instance { get; private set; }

    public PlayerActor Player { get; private set; }
    public IReadOnlyList<EnemyActor> Enemies => enemies;
    public bool IsGameOver { get; private set; }
    public bool IsSideScrollMode { get; private set; }
    public float SurvivalTime => survivalTime;

    private readonly List<EnemyActor> enemies = new();
    private readonly List<Transform> groundTiles = new();
    private readonly List<GameObject> hiddenTopDownObjects = new();
    private readonly List<SidePlatform> sidePlatforms = new();
    private readonly List<SideBlocker> sideBlockers = new();
    private EnemySpawner spawner;
    private Text hudText;
    private Camera mainCamera;
    private Transform groundRoot;
    private float groundTileSize = 24f;
    private int groundGridRadius = 2; // 2 => 5x5, 3 => 7x7
    private GameObject sideScrollRoot;
    private Transform sideStartPoint;
    private Transform sideGoalPoint;
    private Transform sideEnemyRoot;
    private GameObject sidePortal;
    private Vector3 returnPosition;
    private float portalCooldown;
    private GameObject levelUpPanel;
    private Text levelUpTitle;
    private readonly List<Button> levelUpButtons = new();
    private GameObject sideJumpBarRoot;
    private Image sideJumpBarFill;
    private Text sideJumpBarLabel;
    private GameObject sideJumpDebugRoot;
    private Text sideJumpDebugText;
    private readonly List<WeaponType> offeredWeapons = new();
    private int pendingLevelUps;
    private bool isChoosingLevelUp;
    private static Sprite cachedRuntimeSprite;

    private int level = 1;
    private int killCount;
    private float currentExp;
    private float nextLevelExp = 5f;
    private float survivalTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildWorld();
    }

    private void Update()
    {
        if (IsGameOver)
        {
            return;
        }

        survivalTime += Time.deltaTime;
        portalCooldown = Mathf.Max(0f, portalCooldown - Time.unscaledDeltaTime);
        UpdateSideScrollState();
        UpdateHud();
    }

    private void LateUpdate()
    {
        if (Player == null)
        {
            return;
        }

        UpdateCameraPosition();

        if (!IsSideScrollMode)
        {
            UpdateInfiniteGround();
        }
    }

    public void RegisterEnemy(EnemyActor enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyActor enemy)
    {
        enemies.Remove(enemy);
    }

    public void SpawnEnemyAt(Vector3 position, float health, float speed, float dps, EnemyKind enemyKind)
    {
        PixelSpriteId enemySprite = enemyKind == EnemyKind.Goblin ? PixelSpriteId.Goblin : PixelSpriteId.Slime;
        Color enemyTint = enemyKind == EnemyKind.Goblin ? new Color(0.95f, 0.95f, 0.95f) : new Color(1f, 1f, 1f);
        var go = CreateSpriteEntity(
            enemyKind.ToString(),
            position,
            new Vector2(1.2f, 1.2f),
            enemyTint,
            20,
            PixelArtFactory.Get(enemySprite));

        var enemy = go.AddComponent<EnemyActor>();
        enemy.Initialize(this, Player, health, speed, dps, enemyKind);
        RegisterEnemy(enemy);
    }

    public void OnEnemyKilled(Vector3 position, float expValue)
    {
        killCount++;

        var gemObject = CreateSpriteEntity(
            "ExpGem",
            position,
            new Vector2(0.35f, 0.35f),
            Color.white,
            10,
            PixelArtFactory.Get(PixelSpriteId.ExpGem));

        var gem = gemObject.AddComponent<ExpGem>();
        gem.Initialize(this, Player, expValue);
    }

    public GameObject CreateSpriteEntity(
        string objectName,
        Vector3 position,
        Vector2 scale,
        Color color,
        int sortingOrder,
        Sprite sprite = null)
    {
        var go = new GameObject(objectName);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite ?? GetRuntimeSprite();
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    public void AddExperience(float amount)
    {
        if (IsGameOver)
        {
            return;
        }

        currentExp += amount;
        while (currentExp >= nextLevelExp)
        {
            currentExp -= nextLevelExp;
            LevelUp();
        }
    }

    public void TriggerGameOver()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        isChoosingLevelUp = false;
        IsSideScrollMode = false;
        pendingLevelUps = 0;
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
        Time.timeScale = 0f;
        hudText.text += "\nGAME OVER - Press Play again to retry";
    }

    public EnemyActor FindNearestEnemy(Vector3 from)
    {
        EnemyActor nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            float sqrDistance = (enemy.transform.position - from).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void BuildWorld()
    {
        Time.timeScale = 1f;
        BuildCamera();
        BuildGround();
        BuildPlayer();
        BuildSpawner();
        BuildHud();
        BuildSideScrollStage();
        BuildSidePortal();
    }

    private void BuildCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var cameraGo = new GameObject("Main Camera");
            mainCamera = cameraGo.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.06f, 0.08f, 0.14f);
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 13f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.transform.rotation = Quaternion.identity;
        UpdateCameraPosition();
    }

    private void BuildGround()
    {
        groundTiles.Clear();
        groundRoot = new GameObject("InfiniteGround").transform;

        for (int y = -groundGridRadius; y <= groundGridRadius; y++)
        {
            for (int x = -groundGridRadius; x <= groundGridRadius; x++)
            {
                Sprite groundSprite = GetGroundSpriteForCell(x, y);

                var tile = CreateSpriteEntity(
                    $"Ground_{x}_{y}",
                    new Vector3(x * groundTileSize, y * groundTileSize, 0f),
                    new Vector2(groundTileSize, groundTileSize),
                    Color.white,
                    0,
                    groundSprite);
                tile.transform.SetParent(groundRoot, true);
                groundTiles.Add(tile.transform);
            }
        }
    }

    private void UpdateInfiniteGround()
    {
        if (Player == null || groundTiles.Count == 0)
        {
            return;
        }

        Vector3 playerPos = Player.transform.position;
        int baseCellX = Mathf.FloorToInt(playerPos.x / groundTileSize);
        int baseCellY = Mathf.FloorToInt(playerPos.y / groundTileSize);

        int index = 0;
        for (int y = -groundGridRadius; y <= groundGridRadius; y++)
        {
            for (int x = -groundGridRadius; x <= groundGridRadius; x++)
            {
                if (index >= groundTiles.Count)
                {
                    return;
                }

                int cellX = baseCellX + x;
                int cellY = baseCellY + y;
                groundTiles[index].position = new Vector3(cellX * groundTileSize, cellY * groundTileSize, 0f);

                var renderer = groundTiles[index].GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = GetGroundSpriteForCell(cellX, cellY);
                }
                index++;
            }
        }
    }

    private static Sprite GetGroundSpriteForCell(int cellX, int cellY)
    {
        bool evenTile = ((cellX + cellY) & 1) == 0;
        return evenTile ? PixelArtFactory.Get(PixelSpriteId.GroundA) : PixelArtFactory.Get(PixelSpriteId.GroundB);
    }

    private void BuildPlayer()
    {
        var playerObject = CreateSpriteEntity(
            "Player",
            Vector3.zero,
            new Vector2(1.45f, 1.45f),
            Color.white,
            25,
            PixelArtFactory.Get(PixelSpriteId.HeroKnight));

        Player = playerObject.AddComponent<PlayerActor>();
        Player.Initialize(this);
    }

    private void BuildSpawner()
    {
        spawner = gameObject.AddComponent<EnemySpawner>();
        spawner.Initialize(this, Player);
    }

    private void BuildHud()
    {
        EnsureEventSystem();

        var canvasObject = new GameObject("HUD");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        var textObject = new GameObject("Stats");
        textObject.transform.SetParent(canvasObject.transform, false);
        hudText = textObject.AddComponent<Text>();
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudText.fontSize = 24;
        hudText.alignment = TextAnchor.UpperLeft;
        hudText.color = Color.white;

        var rect = hudText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -18f);
        rect.sizeDelta = new Vector2(720f, 240f);

        UpdateHud();
        BuildLevelUpPanel(canvasObject.transform);
        BuildSideJumpChargeBar(canvasObject.transform);
        BuildSideJumpDebugText(canvasObject.transform);
    }

    private void BuildSidePortal()
    {
        sidePortal = new GameObject("TunnelEntrance");
        sidePortal.transform.position = new Vector3(7.5f, -4f, 0f);

        var rim = CreateSpriteEntity(
            "TunnelRim",
            sidePortal.transform.position,
            new Vector2(3.2f, 3.2f),
            Color.white,
            18,
            PixelArtFactory.Get(PixelSpriteId.TunnelRim));
        rim.transform.SetParent(sidePortal.transform, true);

        var hole = CreateSpriteEntity(
            "TunnelHole",
            sidePortal.transform.position + new Vector3(0f, -0.08f, 0f),
            new Vector2(2.3f, 2.3f),
            Color.white,
            19,
            PixelArtFactory.Get(PixelSpriteId.TunnelHole));
        hole.transform.SetParent(sidePortal.transform, true);

        var arrow = CreateSpriteEntity(
            "TunnelArrow",
            sidePortal.transform.position + new Vector3(0f, 2f, 0f),
            new Vector2(0.4f, 1.4f),
            new Color(1f, 0.88f, 0.3f),
            20);
        arrow.transform.SetParent(sidePortal.transform, true);
    }

    private void BuildSideScrollStage()
    {
        sideScrollRoot = new GameObject("SideScrollStage");
        sideScrollRoot.SetActive(false);

        sidePlatforms.Clear();
        sideBlockers.Clear();

        CreateStageSprite("StageBG", new Vector3(29f, 2.5f, 0f), new Vector2(64f, 20f), Color.white, 1, PixelSpriteId.StageSky);
        CreateStageSprite("StageGround", new Vector3(29f, -1.8f, 0f), new Vector2(62f, 3.2f), Color.white, 3, PixelSpriteId.StageGround);
        AddSidePlatform(new Vector3(29f, -1.8f, 0f), new Vector2(62f, 3.2f), 0f);

        CreateStageSprite("Pipe_1", new Vector3(14f, -0.6f, 0f), new Vector2(2f, 2.4f), Color.white, 6, PixelSpriteId.Pipe);
        AddSidePlatform(new Vector3(14f, -0.6f, 0f), new Vector2(2f, 2.4f), 0.12f);
        AddSideBlocker(new Vector3(14f, -0.6f, 0f), new Vector2(2f, 2.4f), 0.06f);

        CreateStageSprite("Pipe_2", new Vector3(29f, -0.1f, 0f), new Vector2(2.6f, 3.4f), Color.white, 6, PixelSpriteId.Pipe);
        AddSidePlatform(new Vector3(29f, -0.1f, 0f), new Vector2(2.6f, 3.4f), 0.15f);
        AddSideBlocker(new Vector3(29f, -0.1f, 0f), new Vector2(2.6f, 3.4f), 0.08f);

        CreateStageSprite("Pipe_3", new Vector3(43f, -0.6f, 0f), new Vector2(2f, 2.4f), Color.white, 6, PixelSpriteId.Pipe);
        AddSidePlatform(new Vector3(43f, -0.6f, 0f), new Vector2(2f, 2.4f), 0.12f);
        AddSideBlocker(new Vector3(43f, -0.6f, 0f), new Vector2(2f, 2.4f), 0.06f);

        var startGo = new GameObject("SideStartPoint");
        startGo.transform.SetParent(sideScrollRoot.transform, false);
        // Spawn at "feet on ground" height for side-scroll controller.
        startGo.transform.position = new Vector3(2f, 0.52f, 0f);
        sideStartPoint = startGo.transform;

        var goal = CreateStageSprite("SideGoal", new Vector3(58f, 0f, 0f), new Vector2(2.5f, 5f), Color.white, 8, PixelSpriteId.Goal);
        sideGoalPoint = goal.transform;

        RebuildSideScrollEnemies();
    }

    private GameObject CreateStageSprite(
        string name,
        Vector3 position,
        Vector2 scale,
        Color color,
        int sortingOrder,
        PixelSpriteId spriteId)
    {
        var sprite = CreateSpriteEntity(name, position, scale, color, sortingOrder, PixelArtFactory.Get(spriteId));
        sprite.transform.SetParent(sideScrollRoot.transform, true);
        return sprite;
    }

    private void UpdateSideScrollState()
    {
        if (Player == null)
        {
            return;
        }

        if (!IsSideScrollMode)
        {
            if (portalCooldown > 0f || sidePortal == null)
            {
                return;
            }

            float triggerDistance = 2.15f;
            if ((Player.transform.position - sidePortal.transform.position).sqrMagnitude <= triggerDistance * triggerDistance)
            {
                EnterSideScrollMode();
            }
            return;
        }

        if (sideGoalPoint != null && Player.transform.position.x >= sideGoalPoint.position.x)
        {
            ExitSideScrollModeWithReward();
        }
    }

    private void EnterSideScrollMode()
    {
        if (IsSideScrollMode || Player == null || sideStartPoint == null)
        {
            return;
        }

        IsSideScrollMode = true;
        returnPosition = Player.transform.position;
        if (groundRoot != null)
        {
            groundRoot.gameObject.SetActive(false);
        }
        if (sidePortal != null)
        {
            sidePortal.SetActive(false);
        }
        if (sideScrollRoot != null)
        {
            sideScrollRoot.SetActive(true);
        }
        RebuildSideScrollEnemies();

        SetTopDownActorsVisible(false);
        Player.transform.position = sideStartPoint.position;
        Player.EnterSideScrollMode();

        if (mainCamera != null)
        {
            mainCamera.orthographicSize = 8.5f;
        }
        UpdateCameraPosition();
    }

    private void ExitSideScrollModeWithReward()
    {
        if (!IsSideScrollMode || Player == null)
        {
            return;
        }

        IsSideScrollMode = false;
        Player.ExitSideScrollMode();
        Player.transform.position = returnPosition + new Vector3(2.8f, 0f, 0f);

        if (sideScrollRoot != null)
        {
            sideScrollRoot.SetActive(false);
        }
        if (groundRoot != null)
        {
            groundRoot.gameObject.SetActive(true);
        }
        if (sidePortal != null)
        {
            sidePortal.SetActive(true);
        }
        SetTopDownActorsVisible(true);
        if (mainCamera != null)
        {
            mainCamera.orthographicSize = 13f;
        }
        UpdateCameraPosition();

        portalCooldown = 1.2f;
        GrantOneLevelWorthExperience();
    }

    private void UpdateCameraPosition()
    {
        if (mainCamera == null || Player == null)
        {
            return;
        }

        Vector3 playerPosition = Player.transform.position;
        if (!IsSideScrollMode)
        {
            mainCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10f);
            return;
        }

        float leftBound = sideStartPoint != null ? sideStartPoint.position.x - 2f : 0f;
        float rightBound = sideGoalPoint != null ? sideGoalPoint.position.x : 58f;
        float stageCenter = (leftBound + rightBound) * 0.5f;
        float stageHalfWidth = Mathf.Max(0.1f, (rightBound - leftBound) * 0.5f);
        float camHalfWidth = Mathf.Max(0.1f, mainCamera.orthographicSize * mainCamera.aspect);

        float cameraX;
        if (camHalfWidth >= stageHalfWidth)
        {
            cameraX = stageCenter;
        }
        else
        {
            cameraX = Mathf.Clamp(playerPosition.x, leftBound + camHalfWidth, rightBound - camHalfWidth);
        }

        mainCamera.transform.position = new Vector3(cameraX, 2.5f, -10f);
    }

    private void GrantOneLevelWorthExperience()
    {
        float neededExp = Mathf.Max(0.1f, nextLevelExp - currentExp);
        AddExperience(neededExp);
    }

    public float ClampSideScrollX(float x)
    {
        return Mathf.Clamp(x, 0f, 60f);
    }

    public bool IsSideScrollGrounded(float x, float bottomY, float halfWidth, float tolerance, out float groundY)
    {
        groundY = float.MinValue;
        bool found = false;

        for (int i = 0; i < sidePlatforms.Count; i++)
        {
            var p = sidePlatforms[i];
            if (x + halfWidth < p.Left || x - halfWidth > p.Right)
            {
                continue;
            }

            if (Mathf.Abs(bottomY - p.Top) <= tolerance)
            {
                if (!found || p.Top > groundY)
                {
                    groundY = p.Top;
                    found = true;
                }
            }
        }

        return found;
    }

    public bool TryResolveSideScrollGround(float x, float previousBottomY, float nextBottomY, float halfWidth, out float groundY)
    {
        groundY = float.MinValue;
        bool landed = false;
        const float coyote = 0.05f;

        for (int i = 0; i < sidePlatforms.Count; i++)
        {
            var p = sidePlatforms[i];
            if (x + halfWidth < p.Left || x - halfWidth > p.Right)
            {
                continue;
            }

            bool crossedFromAbove = previousBottomY >= p.Top - coyote && nextBottomY <= p.Top + coyote;
            if (!crossedFromAbove)
            {
                continue;
            }

            if (!landed || p.Top > groundY)
            {
                groundY = p.Top;
                landed = true;
            }
        }

        return landed;
    }

    public float ResolveSideScrollHorizontal(float previousX, float proposedX, float centerY, float halfWidth, float halfHeight)
    {
        float resolvedX = proposedX;
        float moveDir = Mathf.Sign(proposedX - previousX);

        for (int i = 0; i < sideBlockers.Count; i++)
        {
            var b = sideBlockers[i];
            float currentLeft = resolvedX - halfWidth;
            float currentRight = resolvedX + halfWidth;
            float currentBottom = centerY - halfHeight;
            float currentTop = centerY + halfHeight;
            bool overlapY = currentTop > b.Bottom && currentBottom < b.Top;
            if (!overlapY)
            {
                continue;
            }

            bool overlapX = currentRight > b.Left && currentLeft < b.Right;
            if (overlapX)
            {
                if (moveDir > 0f)
                {
                    resolvedX = Mathf.Min(resolvedX, b.Left - halfWidth);
                }
                else if (moveDir < 0f)
                {
                    resolvedX = Mathf.Max(resolvedX, b.Right + halfWidth);
                }
                else
                {
                    float leftPush = Mathf.Abs(currentRight - b.Left);
                    float rightPush = Mathf.Abs(b.Right - currentLeft);
                    resolvedX = leftPush < rightPush ? b.Left - halfWidth : b.Right + halfWidth;
                }
                continue;
            }

            if (proposedX > previousX)
            {
                float previousRight = previousX + halfWidth;
                if (previousRight <= b.Left && currentRight >= b.Left)
                {
                    resolvedX = b.Left - halfWidth;
                }
            }
            else if (proposedX < previousX)
            {
                float previousLeft = previousX - halfWidth;
                if (previousLeft >= b.Right && currentLeft <= b.Right)
                {
                    resolvedX = b.Right + halfWidth;
                }
            }
        }

        return resolvedX;
    }

    private void AddSidePlatform(Vector3 center, Vector2 size, float insetX)
    {
        float halfWidth = Mathf.Max(0.02f, size.x * 0.5f - insetX);
        sidePlatforms.Add(new SidePlatform
        {
            Left = center.x - halfWidth,
            Right = center.x + halfWidth,
            Top = center.y + size.y * 0.5f
        });
    }

    private void AddSideBlocker(Vector3 center, Vector2 size, float insetX)
    {
        float halfWidth = Mathf.Max(0.02f, size.x * 0.5f - insetX);
        float halfHeight = size.y * 0.5f;
        sideBlockers.Add(new SideBlocker
        {
            Left = center.x - halfWidth,
            Right = center.x + halfWidth,
            Bottom = center.y - halfHeight,
            Top = center.y + halfHeight
        });
    }

    private void RebuildSideScrollEnemies()
    {
        if (sideScrollRoot == null)
        {
            return;
        }

        if (sideEnemyRoot != null)
        {
            Destroy(sideEnemyRoot.gameObject);
        }

        var root = new GameObject("SideEnemies");
        root.transform.SetParent(sideScrollRoot.transform, false);
        sideEnemyRoot = root.transform;

        SpawnSideEnemy(EnemyKind.Slime, new Vector3(9f, -0.2f, 0f), 7f, 16f, 2.2f, 10f);
        SpawnSideEnemy(EnemyKind.Goblin, new Vector3(22f, -0.2f, 0f), 18f, 27f, 2.4f, 12f);
        SpawnSideEnemy(EnemyKind.Slime, new Vector3(29f, 1.6f, 0f), 27.8f, 30.2f, 1.8f, 12f);
        SpawnSideEnemy(EnemyKind.Goblin, new Vector3(43f, 0.6f, 0f), 41.8f, 44.2f, 2.1f, 13f);
        SpawnSideEnemy(EnemyKind.Slime, new Vector3(50f, -0.2f, 0f), 47f, 56f, 2.4f, 13f);
    }

    private void SpawnSideEnemy(EnemyKind kind, Vector3 position, float leftX, float rightX, float speed, float dps)
    {
        PixelSpriteId sprite = kind == EnemyKind.Goblin ? PixelSpriteId.Goblin : PixelSpriteId.Slime;
        var enemyObject = CreateSpriteEntity(
            $"Side_{kind}",
            position,
            new Vector2(1.15f, 1.15f),
            Color.white,
            22,
            PixelArtFactory.Get(sprite));
        enemyObject.transform.SetParent(sideEnemyRoot, true);

        var actor = enemyObject.AddComponent<SideScrollEnemyActor>();
        actor.Initialize(this, Player, leftX, rightX, position.y, speed, dps);
    }

    private void SetTopDownActorsVisible(bool visible)
    {
        if (!visible)
        {
            hiddenTopDownObjects.Clear();
            HideObjectsOfType<EnemyActor>();
            HideObjectsOfType<ExpGem>();
            HideObjectsOfType<ProjectileActor>();
            HideObjectsOfType<OrbitBladeActor>();
            HideObjectsOfType<EnemyProjectileActor>();
            return;
        }

        for (int i = 0; i < hiddenTopDownObjects.Count; i++)
        {
            var go = hiddenTopDownObjects[i];
            if (go != null)
            {
                go.SetActive(true);
            }
        }
        hiddenTopDownObjects.Clear();
    }

    private void HideObjectsOfType<T>() where T : Component
    {
        var objects = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < objects.Length; i++)
        {
            var go = objects[i].gameObject;
            if (go == null || go == Player.gameObject || !go.activeSelf)
            {
                continue;
            }

            hiddenTopDownObjects.Add(go);
            go.SetActive(false);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void LevelUp()
    {
        level++;
        nextLevelExp = 4f + (level * 2.25f);
        pendingLevelUps++;

        if (Player != null)
        {
            Player.ApplyLevelBonus();
        }

        TryOpenLevelUpChoice();
    }

    private void UpdateHud()
    {
        if (hudText == null || Player == null)
        {
            return;
        }

        hudText.text = $"HP {Player.CurrentHealth:0}/{Player.MaxHealth:0}\n" +
                       $"Level {level}   XP {currentExp:0.0}/{nextLevelExp:0.0}\n" +
                       $"Kills {killCount}   Time {survivalTime:0.0}s\n" +
                       $"Weapons {Player.GetWeaponHudText()}";
    }

    public void SetSideJumpChargeUI(bool visible, float normalized)
    {
        if (sideJumpBarRoot == null || sideJumpBarFill == null)
        {
            return;
        }

        bool shouldShow = visible && IsSideScrollMode;
        if (sideJumpBarRoot.activeSelf != shouldShow)
        {
            sideJumpBarRoot.SetActive(shouldShow);
        }

        sideJumpBarFill.fillAmount = Mathf.Clamp01(normalized);
        if (sideJumpBarLabel != null)
        {
            sideJumpBarLabel.text = $"JUMP {Mathf.RoundToInt(sideJumpBarFill.fillAmount * 100f)}%";
        }
    }

    public void SetSideJumpDebugUI(bool visible, string text)
    {
        if (sideJumpDebugRoot == null || sideJumpDebugText == null)
        {
            return;
        }

        bool shouldShow = visible && IsSideScrollMode;
        if (sideJumpDebugRoot.activeSelf != shouldShow)
        {
            sideJumpDebugRoot.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            sideJumpDebugText.text = text;
        }
    }

    private void BuildLevelUpPanel(Transform parent)
    {
        levelUpPanel = new GameObject("LevelUpPanel");
        levelUpPanel.transform.SetParent(parent, false);
        var panelImage = levelUpPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        var panelRect = panelImage.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var titleObject = new GameObject("Title");
        titleObject.transform.SetParent(levelUpPanel.transform, false);
        levelUpTitle = titleObject.AddComponent<Text>();
        levelUpTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelUpTitle.fontSize = 36;
        levelUpTitle.alignment = TextAnchor.MiddleCenter;
        levelUpTitle.color = Color.white;
        levelUpTitle.text = "LEVEL UP - Choose 1";

        var titleRect = levelUpTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(900f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, 150f);

        for (int i = 0; i < 3; i++)
        {
            int buttonIndex = i;
            var buttonObject = new GameObject($"Choice_{i + 1}");
            buttonObject.transform.SetParent(levelUpPanel.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnSelectWeapon(buttonIndex));
            levelUpButtons.Add(button);

            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 88f);
            rect.anchoredPosition = new Vector2(0f, 40f - i * 104f);

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "Option";

            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        levelUpPanel.SetActive(false);
    }

    private void BuildSideJumpChargeBar(Transform parent)
    {
        sideJumpBarRoot = new GameObject("SideJumpChargeBar");
        sideJumpBarRoot.transform.SetParent(parent, false);

        var bgImage = sideJumpBarRoot.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.62f);
        var bgRect = bgImage.rectTransform;
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0f, 24f);
        bgRect.sizeDelta = new Vector2(340f, 26f);

        var fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(sideJumpBarRoot.transform, false);
        sideJumpBarFill = fillObject.AddComponent<Image>();
        sideJumpBarFill.color = new Color(0.3f, 0.95f, 0.5f, 0.95f);
        sideJumpBarFill.type = Image.Type.Filled;
        sideJumpBarFill.fillMethod = Image.FillMethod.Horizontal;
        sideJumpBarFill.fillOrigin = 0;
        sideJumpBarFill.fillAmount = 0f;

        var fillRect = sideJumpBarFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(sideJumpBarRoot.transform, false);
        sideJumpBarLabel = labelObject.AddComponent<Text>();
        sideJumpBarLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sideJumpBarLabel.fontSize = 15;
        sideJumpBarLabel.alignment = TextAnchor.MiddleCenter;
        sideJumpBarLabel.color = Color.white;
        sideJumpBarLabel.text = "JUMP 0%";

        var labelRect = sideJumpBarLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        sideJumpBarRoot.SetActive(false);
    }

    private void BuildSideJumpDebugText(Transform parent)
    {
        sideJumpDebugRoot = new GameObject("SideJumpDebugText");
        sideJumpDebugRoot.transform.SetParent(parent, false);
        sideJumpDebugText = sideJumpDebugRoot.AddComponent<Text>();
        sideJumpDebugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sideJumpDebugText.fontSize = 16;
        sideJumpDebugText.alignment = TextAnchor.LowerLeft;
        sideJumpDebugText.color = new Color(1f, 0.95f, 0.5f);
        sideJumpDebugText.text = "Jump Debug";

        var rect = sideJumpDebugText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(16f, 64f);
        rect.sizeDelta = new Vector2(700f, 90f);

        sideJumpDebugRoot.SetActive(false);
    }

    private void TryOpenLevelUpChoice()
    {
        if (IsGameOver || isChoosingLevelUp || pendingLevelUps <= 0 || Player == null || levelUpPanel == null)
        {
            return;
        }

        offeredWeapons.Clear();
        offeredWeapons.AddRange(Player.RollWeaponChoices(3));
        if (offeredWeapons.Count == 0)
        {
            pendingLevelUps--;
            if (pendingLevelUps <= 0)
            {
                Time.timeScale = 1f;
            }
            return;
        }

        Time.timeScale = 0f;
        isChoosingLevelUp = true;
        levelUpPanel.SetActive(true);
        levelUpTitle.text = pendingLevelUps > 1
            ? $"LEVEL UP x{pendingLevelUps} - Choose 1"
            : "LEVEL UP - Choose 1";

        for (int i = 0; i < levelUpButtons.Count; i++)
        {
            bool hasChoice = i < offeredWeapons.Count;
            var button = levelUpButtons[i];
            button.gameObject.SetActive(hasChoice);
            if (!hasChoice)
            {
                continue;
            }

            WeaponType weaponType = offeredWeapons[i];
            int currentLevel = Player.GetWeaponLevel(weaponType);
            string action = currentLevel <= 0 ? "Unlock Lv.1" : $"Upgrade Lv.{currentLevel + 1}";
            string text = $"{GetWeaponName(weaponType)}  ({action}/5)";
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }
    }

    private void OnSelectWeapon(int index)
    {
        if (!isChoosingLevelUp || Player == null || index < 0 || index >= offeredWeapons.Count)
        {
            return;
        }

        Player.ApplyWeaponChoice(offeredWeapons[index]);
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        isChoosingLevelUp = false;
        levelUpPanel.SetActive(false);
        offeredWeapons.Clear();

        if (IsGameOver)
        {
            return;
        }

        if (pendingLevelUps > 0)
        {
            TryOpenLevelUpChoice();
            return;
        }

        Time.timeScale = 1f;
    }

    private static string GetWeaponName(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.PulseShot => "Pulse Shot",
            WeaponType.SpreadShot => "Spread Shot",
            WeaponType.OrbitBlades => "Orbit Blades",
            WeaponType.PierceLance => "Pierce Lance",
            WeaponType.NovaBurst => "Nova Burst",
            _ => weaponType.ToString()
        };
    }

    private static Sprite GetRuntimeSprite()
    {
        if (cachedRuntimeSprite != null)
        {
            return cachedRuntimeSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedRuntimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        cachedRuntimeSprite.name = "RuntimeSprite";
        return cachedRuntimeSprite;
    }
}
