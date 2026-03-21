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
        if (game == null || player == null || game.IsGameOver || game.IsSideScrollMode)
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

        spawnTimer = interval;
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = t > 20f && Random.value < 0.42f ? EnemyKind.Goblin : EnemyKind.Slime;
            float hp = kind == EnemyKind.Goblin ? 20f + t * 0.22f : 30f + t * 0.35f;
            float speed = kind == EnemyKind.Goblin ? 2.6f + t * 0.013f : 2.05f + t * 0.01f;
            float dps = kind == EnemyKind.Goblin ? 9f + t * 0.08f : 6f + t * 0.06f;

            Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(14f, 20f);
            Vector3 spawnPosition = player.transform.position + new Vector3(ring.x, ring.y, 0f);
            game.SpawnEnemyAt(spawnPosition, hp, speed, dps, kind);
        }
    }
}
