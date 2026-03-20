using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SurvivorGame : MonoBehaviour
{
    public static SurvivorGame Instance { get; private set; }

    public PlayerActor Player { get; private set; }
    public IReadOnlyList<EnemyActor> Enemies => enemies;
    public bool IsGameOver { get; private set; }
    public float SurvivalTime => survivalTime;

    private readonly List<EnemyActor> enemies = new();
    private EnemySpawner spawner;
    private Text hudText;

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
        UpdateHud();
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
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Enemy";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);

        var renderer = go.GetComponent<Renderer>();
        renderer.material.color = new Color(0.85f, 0.2f, 0.2f);

        var enemy = go.AddComponent<EnemyActor>();
        enemy.Initialize(this, Player, health, speed, dps);
        RegisterEnemy(enemy);
    }

    public void OnEnemyKilled(Vector3 position, float expValue)
    {
        killCount++;

        var gemObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gemObject.name = "ExpGem";
        gemObject.transform.position = position + Vector3.up * 0.35f;
        gemObject.transform.localScale = Vector3.one * 0.3f;

        var renderer = gemObject.GetComponent<Renderer>();
        renderer.material.color = new Color(0.1f, 0.95f, 0.95f);

        var gem = gemObject.AddComponent<ExpGem>();
        gem.Initialize(this, Player, expValue);
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
        BuildLighting();
        BuildGround();
        BuildPlayer();
        BuildSpawner();
        BuildHud();
    }

    private void BuildCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraGo = new GameObject("Main Camera");
            camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        camera.orthographic = true;
        camera.orthographicSize = 11f;
        camera.transform.position = new Vector3(0f, 20f, 0f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void BuildLighting()
    {
        if (FindFirstObjectByType<Light>() != null)
        {
            return;
        }

        var lightObject = new GameObject("Directional Light");
        var lightComponent = lightObject.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        lightComponent.color = Color.white;
        lightComponent.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
    }

    private void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(6f, 1f, 6f);

        var renderer = ground.GetComponent<Renderer>();
        renderer.material.color = new Color(0.18f, 0.2f, 0.22f);
    }

    private void BuildPlayer()
    {
        var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObject.name = "Player";
        playerObject.transform.position = new Vector3(0f, 0.6f, 0f);

        var renderer = playerObject.GetComponent<Renderer>();
        renderer.material.color = new Color(0.25f, 0.75f, 0.35f);

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
    }

    private void LevelUp()
    {
        level++;
        nextLevelExp = 4f + (level * 2.25f);

        if (Player != null)
        {
            Player.ApplyLevelBonus();
        }
    }

    private void UpdateHud()
    {
        if (hudText == null || Player == null)
        {
            return;
        }

        hudText.text = $"HP {Player.CurrentHealth:0}/{Player.MaxHealth:0}\n" +
                       $"Level {level}   XP {currentExp:0.0}/{nextLevelExp:0.0}\n" +
                       $"Kills {killCount}   Time {survivalTime:0.0}s";
    }
}
