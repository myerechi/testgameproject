using UnityEngine;

public class SideScrollEnemyActor : MonoBehaviour
{
    private SurvivorGame game;
    private PlayerActor player;
    private float leftX;
    private float rightX;
    private float groundY;
    private float speed;
    private float dps;
    private float direction = 1f;

    public void Initialize(
        SurvivorGame owner,
        PlayerActor target,
        float patrolLeft,
        float patrolRight,
        float platformY,
        float moveSpeed,
        float damagePerSecond)
    {
        game = owner;
        player = target;
        leftX = Mathf.Min(patrolLeft, patrolRight);
        rightX = Mathf.Max(patrolLeft, patrolRight);
        groundY = platformY;
        speed = Mathf.Max(0.2f, moveSpeed);
        dps = Mathf.Max(0.1f, damagePerSecond);
    }

    private void Update()
    {
        if (game == null || player == null || game.IsGameOver)
        {
            Destroy(gameObject);
            return;
        }

        if (!game.IsSideScrollMode)
        {
            return;
        }

        const float halfWidth = 0.32f;
        const float halfHeight = 0.58f;
        Vector3 pos = transform.position;
        float previousX = pos.x;
        pos.x += direction * speed * Time.deltaTime;
        pos.x = game.ResolveSideScrollHorizontal(previousX, pos.x, pos.y, halfWidth, halfHeight);

        if (Mathf.Abs(pos.x - previousX) <= 0.0005f)
        {
            direction *= -1f;
        }

        if (pos.x <= leftX)
        {
            pos.x = leftX;
            direction = 1f;
        }
        else if (pos.x >= rightX)
        {
            pos.x = rightX;
            direction = -1f;
        }

        pos.y = groundY;
        transform.position = pos;

        float hitRange = 0.95f;
        if ((player.transform.position - pos).sqrMagnitude <= hitRange * hitRange)
        {
            player.ReceiveDamage(dps * Time.deltaTime);
        }
    }
}
