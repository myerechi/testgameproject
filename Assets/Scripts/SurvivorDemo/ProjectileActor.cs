using UnityEngine;
using System.Collections.Generic;

public class ProjectileActor : MonoBehaviour
{
    private SurvivorGame game;
    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifeTime = 2f;
    private int remainingPierce;
    private readonly HashSet<int> hitEnemyIds = new();

    public void Initialize(
        SurvivorGame owner,
        Vector2 moveDirection,
        float moveSpeed,
        float hitDamage,
        int pierceCount = 0,
        float maxLifeTime = 2f)
    {
        game = owner;
        direction = moveDirection.normalized;
        speed = moveSpeed;
        damage = hitDamage;
        remainingPierce = Mathf.Max(0, pierceCount);
        lifeTime = Mathf.Max(0.1f, maxLifeTime);
    }

    private void Update()
    {
        if (game == null || game.IsGameOver)
        {
            Destroy(gameObject);
            return;
        }

        if (game.IsSideScrollMode)
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

        Vector2 movement = direction * (speed * Time.deltaTime);
        transform.position += new Vector3(movement.x, movement.y, 0f);

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
                int enemyId = enemy.GetInstanceID();
                if (hitEnemyIds.Contains(enemyId))
                {
                    continue;
                }

                enemy.TakeDamage(damage);
                hitEnemyIds.Add(enemyId);

                if (remainingPierce <= 0)
                {
                    Destroy(gameObject);
                    return;
                }

                remainingPierce--;
                break;
            }
        }
    }
}
