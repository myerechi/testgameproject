using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class SurvivorGame : MonoBehaviour
{
    public static SurvivorGame Instance { get; private set; }

    public PlayerActor Player { get; private set; }
    public IReadOnlyList<EnemyActor> Enemies => enemies;
    public bool IsGameOver { get; private set; }
    public bool IsSideScrollMode { get; private set; }
    public float SurvivalTime => survivalTime;

    private readonly List<EnemyActor> enemies = new();
    private readonly List<Transform> groundTiles = new();
    private EnemySpawner spawner;
    private Text hudText;
    private Camera mainCamera;
    private Transform groundRoot;
    private float groundTileSize = 24f;
    private int groundGridRadius = 2; // 2 => 5x5, 3 => 7x7
    private GameObject sideScrollRoot;
    private Transform sideStartPoint;
    private Transform sideGoalPoint;
    private GameObject sidePortal;
    private Vector3 returnPosition;
    private float portalCooldown;
    private GameObject levelUpPanel;
    private Text levelUpTitle;
    private readonly List<Button> levelUpButtons = new();
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

        if (mainCamera != null)
        {
            Vector3 playerPosition = Player.transform.position;
            if (IsSideScrollMode)
            {
                float leftBound = 0f;
                float rightBound = 58f;
                float camHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
                float cameraX = Mathf.Clamp(playerPosition.x, leftBound + camHalfWidth, rightBound - camHalfWidth);
                mainCamera.transform.position = new Vector3(cameraX, 2.5f, -10f);
            }
            else
            {
                mainCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10f);
            }
        }

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

    public void SpawnEnemyAt(Vector3 position, float health, float speed, float dps)
    {
        var go = CreateSpriteEntity(
            "Enemy",
            position,
            new Vector2(1f, 1f),
            new Color(0.85f, 0.2f, 0.2f),
            20);

        var enemy = go.AddComponent<EnemyActor>();
        enemy.Initialize(this, Player, health, speed, dps);
        RegisterEnemy(enemy);
    }

    public void OnEnemyKilled(Vector3 position, float expValue)
    {
        killCount++;

        var gemObject = CreateSpriteEntity(
            "ExpGem",
            position,
            new Vector2(0.35f, 0.35f),
            new Color(0.1f, 0.95f, 0.95f),
            10);

        var gem = gemObject.AddComponent<ExpGem>();
        gem.Initialize(this, Player, expValue);
    }

    public GameObject CreateSpriteEntity(string objectName, Vector3 position, Vector2 scale, Color color, int sortingOrder)
    {
        var go = new GameObject(objectName);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRuntimeSprite();
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
        mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 13f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.transform.rotation = Quaternion.identity;
    }

    private void BuildGround()
    {
        groundTiles.Clear();
        groundRoot = new GameObject("InfiniteGround").transform;

        for (int y = -groundGridRadius; y <= groundGridRadius; y++)
        {
            for (int x = -groundGridRadius; x <= groundGridRadius; x++)
            {
                Color tileColor = ((x + y) & 1) == 0
                    ? new Color(0.16f, 0.18f, 0.2f)
                    : new Color(0.2f, 0.22f, 0.24f);

                var tile = CreateSpriteEntity(
                    $"Ground_{x}_{y}",
                    new Vector3(x * groundTileSize, y * groundTileSize, 0f),
                    new Vector2(groundTileSize, groundTileSize),
                    tileColor,
                    0);
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
        float baseX = Mathf.Floor(playerPos.x / groundTileSize) * groundTileSize;
        float baseY = Mathf.Floor(playerPos.y / groundTileSize) * groundTileSize;

        int index = 0;
        for (int y = -groundGridRadius; y <= groundGridRadius; y++)
        {
            for (int x = -groundGridRadius; x <= groundGridRadius; x++)
            {
                if (index >= groundTiles.Count)
                {
                    return;
                }

                groundTiles[index].position = new Vector3(
                    baseX + x * groundTileSize,
                    baseY + y * groundTileSize,
                    0f);
                index++;
            }
        }
    }

    private void BuildPlayer()
    {
        var playerObject = CreateSpriteEntity(
            "Player",
            Vector3.zero,
            new Vector2(1f, 1f),
            new Color(0.25f, 0.75f, 0.35f),
            25);

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
    }

    private void BuildSidePortal()
    {
        sidePortal = CreateSpriteEntity(
            "SideScrollPortal",
            new Vector3(7.5f, 2.5f, 0f),
            new Vector2(3f, 3f),
            new Color(0.85f, 0.35f, 1f),
            18);
    }

    private void BuildSideScrollStage()
    {
        sideScrollRoot = new GameObject("SideScrollStage");
        sideScrollRoot.SetActive(false);

        CreateStageSprite("StageBG", new Vector3(29f, 2.5f, 0f), new Vector2(64f, 20f), new Color(0.1f, 0.16f, 0.22f), 1);
        CreateStageSprite("StageGround", new Vector3(29f, -1.8f, 0f), new Vector2(62f, 3.2f), new Color(0.2f, 0.28f, 0.16f), 3);
        CreateStageSprite("Pipe_1", new Vector3(14f, -0.6f, 0f), new Vector2(2f, 2.4f), new Color(0.16f, 0.7f, 0.2f), 6);
        CreateStageSprite("Pipe_2", new Vector3(29f, -0.1f, 0f), new Vector2(2.6f, 3.4f), new Color(0.16f, 0.7f, 0.2f), 6);
        CreateStageSprite("Pipe_3", new Vector3(43f, -0.6f, 0f), new Vector2(2f, 2.4f), new Color(0.16f, 0.7f, 0.2f), 6);

        var startGo = new GameObject("SideStartPoint");
        startGo.transform.SetParent(sideScrollRoot.transform, false);
        startGo.transform.position = new Vector3(2f, -0.2f, 0f);
        sideStartPoint = startGo.transform;

        var goal = CreateStageSprite("SideGoal", new Vector3(58f, 0f, 0f), new Vector2(2.5f, 5f), new Color(1f, 0.86f, 0.25f), 8);
        sideGoalPoint = goal.transform;
    }

    private GameObject CreateStageSprite(string name, Vector3 position, Vector2 scale, Color color, int sortingOrder)
    {
        var sprite = CreateSpriteEntity(name, position, scale, color, sortingOrder);
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

        Player.transform.position = sideStartPoint.position;
        Player.EnterSideScrollMode();

        if (mainCamera != null)
        {
            mainCamera.orthographicSize = 8.5f;
        }
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
        if (mainCamera != null)
        {
            mainCamera.orthographicSize = 13f;
        }

        portalCooldown = 1.2f;
        GrantOneLevelWorthExperience();
    }

    private void GrantOneLevelWorthExperience()
    {
        float neededExp = Mathf.Max(0.1f, nextLevelExp - currentExp);
        AddExperience(neededExp);
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
