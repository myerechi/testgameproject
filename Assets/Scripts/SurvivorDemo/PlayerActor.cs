using UnityEngine;
using System;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerActor : MonoBehaviour
{
    public float MaxHealth { get; private set; } = 100f;
    public float CurrentHealth { get; private set; } = 100f;

    private SurvivorGame game;
    private float moveSpeed = 8f;
    private float pulseTimer;
    private float spreadTimer;
    private float pierceTimer;
    private float novaTimer;

    private readonly Dictionary<WeaponType, int> weaponLevels = new();
    private readonly List<OrbitBladeActor> orbitBlades = new();
    private float sideVerticalVelocity;

    public void Initialize(SurvivorGame owner)
    {
        game = owner;
        CurrentHealth = MaxHealth;
        weaponLevels[WeaponType.PulseShot] = 1;
    }

    private void Update()
    {
        if (game == null || game.IsGameOver)
        {
            return;
        }

        if (game.IsSideScrollMode)
        {
            MoveSideScroll();
            return;
        }

        MoveTopDown();
        TickWeapons();
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
        moveSpeed += 0.2f;
    }

    public List<WeaponType> RollWeaponChoices(int count)
    {
        var candidates = new List<WeaponType>();
        foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
        {
            if (GetWeaponLevel(type) < 5)
            {
                candidates.Add(type);
            }
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        int resultCount = Mathf.Min(count, candidates.Count);
        if (resultCount <= 0)
        {
            return new List<WeaponType>();
        }

        return candidates.GetRange(0, resultCount);
    }

    public void ApplyWeaponChoice(WeaponType weaponType)
    {
        int nextLevel = Mathf.Clamp(GetWeaponLevel(weaponType) + 1, 1, 5);
        weaponLevels[weaponType] = nextLevel;

        if (weaponType == WeaponType.OrbitBlades)
        {
            RebuildOrbitBlades();
        }
    }

    public int GetWeaponLevel(WeaponType weaponType)
    {
        return weaponLevels.TryGetValue(weaponType, out int level) ? level : 0;
    }

    public string GetWeaponHudText()
    {
        var lines = new List<string>();
        foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
        {
            int level = GetWeaponLevel(type);
            if (level <= 0)
            {
                continue;
            }

            lines.Add($"{GetWeaponDisplayName(type)} Lv.{level}");
        }

        return lines.Count > 0 ? string.Join(", ", lines) : "-";
    }

    public void EnterSideScrollMode()
    {
        sideVerticalVelocity = 0f;
    }

    public void ExitSideScrollMode()
    {
        sideVerticalVelocity = 0f;
    }

    private void MoveTopDown()
    {
        Vector2 input = ReadInput();
        Vector3 movement = new Vector3(input.x, input.y, 0f);

        transform.position += movement * (moveSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    private void MoveSideScroll()
    {
        float groundY = -0.2f;
        Vector2 input = ReadInput();
        float horizontal = input.x;
        float horizontalSpeed = 7.5f;
        float gravity = -24f;
        float jumpSpeed = 11f;

        bool grounded = transform.position.y <= groundY + 0.001f;
        if (grounded)
        {
            transform.position = new Vector3(transform.position.x, groundY, 0f);
            sideVerticalVelocity = Mathf.Max(0f, sideVerticalVelocity);
        }

        if (grounded && ReadJumpPressed())
        {
            sideVerticalVelocity = jumpSpeed;
        }

        sideVerticalVelocity += gravity * Time.deltaTime;

        Vector3 position = transform.position;
        position.x += horizontal * horizontalSpeed * Time.deltaTime;
        position.y += sideVerticalVelocity * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, 0f, 60f);

        if (position.y < groundY)
        {
            position.y = groundY;
            sideVerticalVelocity = 0f;
        }

        transform.position = new Vector3(position.x, position.y, 0f);
    }

    private void TickWeapons()
    {
        pulseTimer -= Time.deltaTime;
        spreadTimer -= Time.deltaTime;
        pierceTimer -= Time.deltaTime;
        novaTimer -= Time.deltaTime;

        TickPulseShot();
        TickSpreadShot();
        TickPierceLance();
        TickNovaBurst();
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

    private bool ReadJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }
        return Keyboard.current.spaceKey.wasPressedThisFrame ||
               Keyboard.current.wKey.wasPressedThisFrame ||
               Keyboard.current.upArrowKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
    }

    private void TickPulseShot()
    {
        int level = GetWeaponLevel(WeaponType.PulseShot);
        if (level <= 0 || pulseTimer > 0f)
        {
            return;
        }

        var target = game.FindNearestEnemy(transform.position);
        if (target == null)
        {
            return;
        }

        float cooldown = Mathf.Max(0.5f - (level - 1) * 0.06f, 0.18f);
        float damage = 14f + level * 5f;
        float speed = 14f + level * 1.5f;
        int projectileCount = 1 + (level >= 3 ? 1 : 0) + (level >= 5 ? 1 : 0);
        pulseTimer = cooldown;

        Vector2 baseDirection = ((Vector2)(target.transform.position - transform.position)).normalized;
        float totalAngle = (projectileCount - 1) * 8f;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = projectileCount == 1 ? 0f : -totalAngle * 0.5f + (i * 8f);
            Vector2 direction = Rotate(baseDirection, angle);
            FireProjectile(direction, speed, damage, 0, 2.1f, new Color(1f, 0.88f, 0.2f), 0.35f);
        }
    }

    private void TickSpreadShot()
    {
        int level = GetWeaponLevel(WeaponType.SpreadShot);
        if (level <= 0 || spreadTimer > 0f)
        {
            return;
        }

        var target = game.FindNearestEnemy(transform.position);
        if (target == null)
        {
            return;
        }

        float cooldown = Mathf.Max(1.15f - (level - 1) * 0.12f, 0.45f);
        float damage = 8f + level * 3f;
        float speed = 11f + level;
        int projectileCount = 3 + level;
        float spanAngle = 34f + level * 6f;
        spreadTimer = cooldown;

        Vector2 baseDirection = ((Vector2)(target.transform.position - transform.position)).normalized;
        for (int i = 0; i < projectileCount; i++)
        {
            float t = projectileCount == 1 ? 0f : (float)i / (projectileCount - 1);
            float angle = Mathf.Lerp(-spanAngle * 0.5f, spanAngle * 0.5f, t);
            Vector2 direction = Rotate(baseDirection, angle);
            FireProjectile(direction, speed, damage, 0, 1.8f, new Color(1f, 0.56f, 0.2f), 0.28f);
        }
    }

    private void TickPierceLance()
    {
        int level = GetWeaponLevel(WeaponType.PierceLance);
        if (level <= 0 || pierceTimer > 0f)
        {
            return;
        }

        var target = game.FindNearestEnemy(transform.position);
        if (target == null)
        {
            return;
        }

        float cooldown = Mathf.Max(1.6f - (level - 1) * 0.16f, 0.8f);
        float damage = 18f + level * 8f;
        float speed = 17f + level * 2f;
        int pierceCount = 1 + level;
        int projectileCount = level >= 4 ? 2 : 1;
        pierceTimer = cooldown;

        Vector2 baseDirection = ((Vector2)(target.transform.position - transform.position)).normalized;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = projectileCount == 1 ? 0f : (i == 0 ? -6f : 6f);
            Vector2 direction = Rotate(baseDirection, angle);
            FireProjectile(direction, speed, damage, pierceCount, 2.6f, new Color(0.35f, 0.95f, 1f), 0.26f);
        }
    }

    private void TickNovaBurst()
    {
        int level = GetWeaponLevel(WeaponType.NovaBurst);
        if (level <= 0 || novaTimer > 0f)
        {
            return;
        }

        float cooldown = Mathf.Max(3.1f - (level - 1) * 0.35f, 1.4f);
        float damage = 7f + level * 3f;
        float speed = 8f + level;
        int shots = 8 + level * 2;
        novaTimer = cooldown;

        for (int i = 0; i < shots; i++)
        {
            float angle = i * (360f / shots);
            Vector2 direction = Rotate(Vector2.right, angle);
            FireProjectile(direction, speed, damage, 0, 1.6f, new Color(1f, 0.25f, 0.85f), 0.24f);
        }
    }

    private void RebuildOrbitBlades()
    {
        for (int i = 0; i < orbitBlades.Count; i++)
        {
            if (orbitBlades[i] != null)
            {
                Destroy(orbitBlades[i].gameObject);
            }
        }
        orbitBlades.Clear();

        int level = GetWeaponLevel(WeaponType.OrbitBlades);
        if (level <= 0 || game == null)
        {
            return;
        }

        int bladeCount = 1 + ((level - 1) / 2);
        float damage = 10f + level * 4f;
        float radius = 1.4f + level * 0.15f;
        float spinSpeed = 115f + level * 22f;

        for (int i = 0; i < bladeCount; i++)
        {
            var bladeObject = game.CreateSpriteEntity(
                "OrbitBlade",
                transform.position,
                new Vector2(0.45f, 0.45f),
                new Color(0.6f, 0.95f, 1f),
                28);
            var blade = bladeObject.AddComponent<OrbitBladeActor>();
            blade.Initialize(game, this, i, bladeCount, damage, radius, spinSpeed);
            orbitBlades.Add(blade);
        }
    }

    private void FireProjectile(
        Vector2 direction,
        float speed,
        float damage,
        int pierceCount,
        float lifeTime,
        Color color,
        float size)
    {
        if (direction.sqrMagnitude < 0.0001f || game == null)
        {
            return;
        }

        var projectileObject = game.CreateSpriteEntity(
            "Projectile",
            transform.position + (Vector3)(direction.normalized * 1.1f),
            new Vector2(size, size),
            color,
            30);

        var projectile = projectileObject.AddComponent<ProjectileActor>();
        projectile.Initialize(game, direction, speed, damage, pierceCount, lifeTime);
    }

    private static Vector2 Rotate(Vector2 direction, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        ).normalized;
    }

    private static string GetWeaponDisplayName(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.PulseShot => "Pulse",
            WeaponType.SpreadShot => "Spread",
            WeaponType.OrbitBlades => "Orbit",
            WeaponType.PierceLance => "Pierce",
            WeaponType.NovaBurst => "Nova",
            _ => weaponType.ToString()
        };
    }
}
