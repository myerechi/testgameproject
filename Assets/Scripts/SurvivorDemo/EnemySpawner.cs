using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private SurvivorGame game;
    private PlayerActor player;
    private float spawnTimer;

    public void Initialize(SurvivorGame owner, PlayerActor target)
    {
        game = owner;
        player = target;
    }

    private void Update()
    {
        if (game == null || player == null || game.IsGameOver)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        float t = game.SurvivalTime;
        float interval = Mathf.Max(0.2f, 0.9f - t * 0.01f);
        int count = Mathf.Clamp(1 + Mathf.FloorToInt(t / 22f), 1, 8);
        float hp = 28f + t * 0.3f;
        float speed = 2.1f + t * 0.012f;
        float dps = 6f + t * 0.06f;

        spawnTimer = interval;
        for (int i = 0; i < count; i++)
        {
            Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(14f, 20f);
            Vector3 spawnPosition = player.transform.position + new Vector3(ring.x, ring.y, 0f);
            game.SpawnEnemyAt(spawnPosition, hp, speed, dps);
        }
    }
}
