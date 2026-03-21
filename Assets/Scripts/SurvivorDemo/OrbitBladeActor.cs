using UnityEngine;

public class OrbitBladeActor : MonoBehaviour
{
    private SurvivorGame game;
    private PlayerActor player;
    private int index;
    private int totalCount;
    private float damage;
    private float orbitRadius;
    private float rotateSpeed;
    private float contactTimer;

    public void Initialize(
        SurvivorGame owner,
        PlayerActor ownerPlayer,
        int bladeIndex,
        int bladesTotal,
        float bladeDamage,
        float radius,
        float degreesPerSecond)
    {
        game = owner;
        player = ownerPlayer;
        index = bladeIndex;
        totalCount = Mathf.Max(1, bladesTotal);
        damage = bladeDamage;
        orbitRadius = radius;
        rotateSpeed = degreesPerSecond;
    }

    private void Update()
    {
        if (game == null || player == null || game.IsGameOver)
        {
            Destroy(gameObject);
            return;
        }

        if (game.IsSideScrollMode)
        {
            return;
        }

        float baseAngle = (360f / totalCount) * index;
        float angle = baseAngle + Time.time * rotateSpeed;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 center = player.transform.position;
        transform.position = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;

        contactTimer -= Time.deltaTime;
        if (contactTimer > 0f)
        {
            return;
        }

        var enemies = game.Enemies;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            float hitRange = enemy.Radius + 0.35f;
            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= hitRange * hitRange)
            {
                enemy.TakeDamage(damage);
                contactTimer = 0.1f;
                break;
            }
        }
    }
}
