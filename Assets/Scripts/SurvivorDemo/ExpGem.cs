using UnityEngine;

public class ExpGem : MonoBehaviour
{
    private SurvivorGame game;
    private PlayerActor player;
    private float expValue;

    public void Initialize(SurvivorGame owner, PlayerActor target, float value)
    {
        game = owner;
        player = target;
        expValue = value;
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

        Vector2 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance < 6f && distance > 0.0001f)
        {
            float pullSpeed = Mathf.Lerp(0f, 13f, 1f - (distance / 6f));
            Vector2 movement = toPlayer.normalized * (pullSpeed * Time.deltaTime);
            transform.position += new Vector3(movement.x, movement.y, 0f);
        }

        if (distance < 1f)
        {
            game.AddExperience(expValue);
            Destroy(gameObject);
        }
    }
}
