using UnityEngine;

public class EnemyActor : MonoBehaviour
{
    public float Radius => 0.45f;

    private SurvivorGame game;
    private PlayerActor player;
    private float health;
    private float speed;
    private float dps;
    private EnemyKind enemyKind;
    private float specialTimer;
    private float rangedTimer;

    public void Initialize(SurvivorGame owner, PlayerActor target, float hp, float moveSpeed, float damagePerSecond, EnemyKind kind)
    {
        game = owner;
        player = target;
        health = hp;
        speed = moveSpeed;
        dps = damagePerSecond;
        enemyKind = kind;
        specialTimer = Random.Range(0.1f, 0.5f);
        rangedTimer = Random.Range(0.4f, 0.9f);
    }

    private void Update()
    {
        if (game == null || player == null || game.IsGameOver)
        {
            return;
        }

        if (game.IsSideScrollMode)
        {
            return;
        }

        switch (enemyKind)
        {
            case EnemyKind.Goblin:
                UpdateGoblin();
                break;
            default:
                UpdateSlime();
                break;
        }
    }

    public void TakeDamage(float amount)
    {
        if (health <= 0f)
        {
            return;
        }

        health -= amount;
        if (health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (game != null)
        {
            game.UnregisterEnemy(this);
            game.OnEnemyKilled(transform.position, 1f);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (game != null)
        {
            game.UnregisterEnemy(this);
        }
    }

    private void UpdateSlime()
    {
        Vector2 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > 0.001f)
        {
            Vector2 movement = toPlayer.normalized * (speed * Time.deltaTime);
            transform.position += new Vector3(movement.x, movement.y, 0f);
        }

        specialTimer -= Time.deltaTime;
        if (specialTimer <= 0f && distance > 0.9f && distance < 6f)
        {
            Vector2 hop = toPlayer.normalized * 1.2f;
            transform.position += new Vector3(hop.x, hop.y, 0f);
            specialTimer = Random.Range(1.2f, 1.8f);
        }

        if (distance <= 0.98f)
        {
            player.ReceiveDamage(dps * Time.deltaTime);
        }
    }

    private void UpdateGoblin()
    {
        Vector2 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > 0.001f)
        {
            Vector2 forward = toPlayer.normalized;
            Vector2 side = new Vector2(-forward.y, forward.x) * Mathf.Sin(Time.time * 2.8f);

            Vector2 desiredMove = side * 0.9f;
            if (distance > 6.8f)
            {
                desiredMove += forward;
            }
            else if (distance < 4.2f)
            {
                desiredMove -= forward * 1.4f;
            }

            if (desiredMove.sqrMagnitude > 0.001f)
            {
                desiredMove = desiredMove.normalized * speed;
                transform.position += new Vector3(desiredMove.x, desiredMove.y, 0f) * Time.deltaTime;
            }
        }

        if (distance <= 0.8f)
        {
            player.ReceiveDamage(dps * 1.15f * Time.deltaTime);
        }

        rangedTimer -= Time.deltaTime;
        if (rangedTimer <= 0f && distance <= 10f && distance >= 2f)
        {
            FireDart(toPlayer.normalized);
            rangedTimer = Random.Range(1f, 1.35f);
        }
    }

    private void FireDart(Vector2 direction)
    {
        if (game == null || direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        var dartObject = game.CreateSpriteEntity(
            "GoblinDart",
            transform.position + (Vector3)(direction * 0.9f),
            new Vector2(0.55f, 0.55f),
            Color.white,
            22,
            PixelArtFactory.Get(PixelSpriteId.EnemyDart));

        var dart = dartObject.AddComponent<EnemyProjectileActor>();
        dart.Initialize(game, player, direction, 9.5f, Mathf.Max(4f, dps * 0.85f), 2.4f);
    }
}
