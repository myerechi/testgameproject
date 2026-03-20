using UnityEngine;

public class EnemyActor : MonoBehaviour
{
    public float Radius => 0.45f;

    private SurvivorGame game;
    private PlayerActor player;
    private float health;
    private float speed;
    private float dps;

    public void Initialize(SurvivorGame owner, PlayerActor target, float hp, float moveSpeed, float damagePerSecond)
    {
        game = owner;
        player = target;
        health = hp;
        speed = moveSpeed;
        dps = damagePerSecond;
    }

    private void Update()
    {
        if (game == null || player == null || game.IsGameOver)
        {
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > 0.001f)
        {
            transform.position += toPlayer.normalized * (speed * Time.deltaTime);
        }

        if (distance <= 1.05f)
        {
            player.ReceiveDamage(dps * Time.deltaTime);
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
}
