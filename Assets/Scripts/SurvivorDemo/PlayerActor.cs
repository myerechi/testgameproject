using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerActor : MonoBehaviour
{
    public float MaxHealth { get; private set; } = 100f;
    public float CurrentHealth { get; private set; } = 100f;

    private SurvivorGame game;
    private float moveSpeed = 8f;
    private float fireInterval = 0.42f;
    private float projectileSpeed = 16f;
    private float projectileDamage = 24f;
    private float fireTimer;

    public void Initialize(SurvivorGame owner)
    {
        game = owner;
        CurrentHealth = MaxHealth;
    }

    private void Update()
    {
        if (game == null || game.IsGameOver)
        {
            return;
        }

        Move();
        AutoFire();
    }

    public void ReceiveDamage(float amount)
    {
        if (game == null || game.IsGameOver)
        {
            return;
        }

        CurrentHealth -= amount;
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            game.TriggerGameOver();
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }

    public void ApplyLevelBonus()
    {
        MaxHealth += 5f;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + 8f);
        projectileDamage += 4f;
        fireInterval = Mathf.Max(0.18f, fireInterval - 0.03f);
        moveSpeed += 0.2f;
    }

    private void Move()
    {
        Vector2 input = ReadInput();
        Vector3 movement = new Vector3(input.x, 0f, input.y);
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        transform.position += movement * (moveSpeed * Time.deltaTime);
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -45f, 45f),
            transform.position.y,
            Mathf.Clamp(transform.position.z, -45f, 45f));
    }

    private void AutoFire()
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
        {
            return;
        }

        var target = game.FindNearestEnemy(transform.position);
        if (target == null)
        {
            return;
        }

        fireTimer = fireInterval;
        Vector3 direction = (target.transform.position - transform.position).normalized;

        var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "Projectile";
        projectileObject.transform.localScale = Vector3.one * 0.35f;
        projectileObject.transform.position = transform.position + direction * 1.2f + Vector3.up * 0.2f;

        var renderer = projectileObject.GetComponent<Renderer>();
        renderer.material.color = new Color(1f, 0.88f, 0.2f);

        var projectile = projectileObject.AddComponent<ProjectileActor>();
        projectile.Initialize(game, direction, projectileSpeed, projectileDamage);
    }

    private Vector2 ReadInput()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 movement = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) movement.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) movement.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) movement.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) movement.x += 1f;
        }

        if (Gamepad.current != null)
        {
            movement += Gamepad.current.leftStick.ReadValue();
        }

        return Vector2.ClampMagnitude(movement, 1f);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }
}
