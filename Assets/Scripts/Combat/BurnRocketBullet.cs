using UnityEngine;

/// <summary>
/// 焚天跟踪小火箭 — 自动追踪最近敌人，命中后叠加1层燃烧。
/// 攻速为燃烧子弹的2倍（cooldown = burnGun.cooldown / 2）。
/// </summary>
public class BurnRocketBullet : ProjectileBase
{
    private float _burnDps;
    private float _burnDuration;
    private float _turnSpeed = 360f;
    private float _homingRadius = 20f;
    private Transform _target;

    protected override void OnHitEnemy(GameObject enemy)
    {
        DotBulletHelper.EnsureStatusEffectManager(enemy);
        var burn = enemy.GetComponent<BurnStackEffect>();
        if (burn == null) burn = enemy.AddComponent<BurnStackEffect>();
        var mage = GameReferences.DotCharacterPassive as MagePassive;
        float dpsMult = mage != null ? mage.GetDotDamageMultiplier() : 1f;
        burn.AddStack(_burnDps * dpsMult, _burnDuration, false, 0f, 0f);
    }

    public void SetupRocket(float speed, float lifetime, float burnDps, float burnDuration)
    {
        SetupBullet(speed, lifetime, 0, 1f, false, 0f, 0f);
        _burnDps = burnDps;
        _burnDuration = burnDuration;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _target = null;
    }

    protected override void FixedUpdate()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
            _target = FindNearestEnemy();

        if (_target != null)
        {
            Vector2 toTarget = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            float maxAngle = _turnSpeed * Time.fixedDeltaTime;
            Vector2 currentDir = _direction;
            float angle = Vector2.SignedAngle(currentDir, toTarget);
            float clampedAngle = Mathf.Clamp(angle, -maxAngle, maxAngle);
            _direction = (Quaternion.Euler(0, 0, clampedAngle) * currentDir).normalized;
            RotateToDirection();
        }

        base.FixedUpdate();
    }

    private Transform FindNearestEnemy()
    {
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return null;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return null;

        float bestDist = _homingRadius * _homingRadius;
        Transform best = null;
        Vector2 pos = transform.position;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;
            var dmg = e.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            float d = ((Vector2)e.transform.position - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = e.transform; }
        }
        return best;
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.BURN_ROCKET);
    }

    private static GameObject BuildTemplate()
    {
        var go = DotBulletTemplate.BuildCircle("BurnRocket",
            DotSpriteCache.CircleSprite(), DotPalette.BurnOrange, 8, 0.6f,
            new DotBulletTemplate.CircleColliderParams { radius = 0.6f });
        go.AddComponent<BurnRocketBullet>();
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.5f, 0.1f);
        go.transform.localScale = Vector3.one * 0.2f;
        return go;
    }

    public static BurnRocketBullet Create(Vector2 pos, Vector2 dir, float speed,
        float lifetime, float burnDps, float burnDuration)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.BURN_ROCKET, BuildTemplate,
            BuildTemplate, pos);
        var r = go.GetComponent<BurnRocketBullet>();
        if (r == null) r = go.AddComponent<BurnRocketBullet>();
        r.SetupRocket(speed, lifetime, burnDps, burnDuration);
        r.SetDirection(dir);
        return r;
    }
}
