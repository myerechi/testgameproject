using UnityEngine;

public class ProjectileActor : MonoBehaviour
{
    private SurvivorGame game;
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifeTime = 2f;

    public void Initialize(SurvivorGame owner, Vector3 moveDirection, float moveSpeed, float hitDamage)
    {
        game = owner;
        direction = moveDirection.normalized;
        speed = moveSpeed;
        damage = hitDamage;
    }

    private void Update()
    {
        if (game == null || game.IsGameOver)
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

        transform.position += direction * (speed * Time.deltaTime);

        var enemies = game.Enemies;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            float hitRange = enemy.Radius + 0.25f;
            if (sqrDistance <= hitRange * hitRange)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }
    }
}
