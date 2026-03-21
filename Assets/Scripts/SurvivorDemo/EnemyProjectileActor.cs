using UnityEngine;

public class EnemyProjectileActor : MonoBehaviour
{
    private SurvivorGame game;
    private PlayerActor player;
    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifeTime;

    public void Initialize(
        SurvivorGame owner,
        PlayerActor target,
        Vector2 moveDirection,
        float moveSpeed,
        float hitDamage,
        float maxLifeTime)
    {
        game = owner;
        player = target;
        direction = moveDirection.normalized;
        speed = moveSpeed;
        damage = hitDamage;
        lifeTime = Mathf.Max(0.2f, maxLifeTime);
    }

    private void Update()
    {
        if (game == null || player == null || game.IsGameOver || game.IsSideScrollMode)
        {
            Destroy(gameObject);
            return;
        }

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        float hitRange = 0.62f;
        if ((player.transform.position - transform.position).sqrMagnitude <= hitRange * hitRange)
        {
            player.ReceiveDamage(damage);
            Destroy(gameObject);
        }
    }
}
