using UnityEngine;

/// <summary>
/// 光明子弹控制器 — 蓄力型定向激光
/// 阶段1：蓄力3秒（玩家头上蓄力条，不减速）
/// 阶段2：朝鼠标方向射出激光，随后顺时针扫45度
/// 激光伤害：每3帧触发一次伤害1点，命中敌人施加光明标记
/// 光明标记：每层受到伤害增加0.5%，无上限，敌人身上显示xN层数
/// </summary>
public class LightBulletController : MonoBehaviour
{
    // ── 参数 ──
    private float _chargeDuration = 3f;
    private int _laserDamage = 1;
    private float _sweepAngle = 45f;
    private float _sweepDuration = 0.4f;
    private float _laserLength = 25f;
    private float _laserWidth = 1.5f;
    private float _markDuration = 15f;
    private int _markMaxStacks = 9999; // 无上限

    // ── 状态 ──
    private enum Phase { Charging, Sweeping, Done }
    private Phase _phase = Phase.Charging;
    private float _phaseTimer;

    // ── 激光状态 ──
    private float _startAngle;
    private float _sweepProgress;
    private int _damageFrameCounter; // 帧伤间隔计数器

    // ── 视觉 ──
    private GameObject _chargeBarObj;
    private GameObject _laserObj;

    // ── 静态引用（防止多个蓄力条残留）──
    private static LightBulletController _activeInstance;

    public void Setup(float chargeDuration, int laserDamage, float sweepAngle,
        float sweepDuration, float laserLength, float laserWidth,
        float markDuration, int markMaxStacks, float unused)
    {
        _chargeDuration = chargeDuration;
        _laserDamage = laserDamage;
        _sweepAngle = sweepAngle;
        _sweepDuration = sweepDuration;
        _laserLength = laserLength;
        _laserWidth = laserWidth;
        _markDuration = markDuration;
        _markMaxStacks = markMaxStacks;
    }

    public void BeginCharge()
    {
        if (_activeInstance != null && _activeInstance != this)
        {
            _activeInstance.CleanupVisuals();
            _activeInstance._phase = Phase.Done;
            DestroyImmediate(_activeInstance.gameObject);
        }
        _activeInstance = this;

        _phase = Phase.Charging;
        _phaseTimer = 0f;
        CreateChargeBarVisual();
    }

    private void Update()
    {
        if (_phase == Phase.Done) return;
        switch (_phase)
        {
            case Phase.Charging: UpdateCharging(); break;
            case Phase.Sweeping: UpdateSweeping(); break;
        }
    }

    // ═══ 阶段1：蓄力 ═══
    private void UpdateCharging()
    {
        _phaseTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_phaseTimer / _chargeDuration);
        UpdateChargeBarVisual(progress);

        if (_phaseTimer >= _chargeDuration)
        {
            CleanupChargeBar();
            _startAngle = GetMouseAngle();
            _sweepProgress = 0f;
            _damageFrameCounter = 0;
            CreateLaserVisual();
            _phase = Phase.Sweeping;
            _phaseTimer = 0f;
        }
    }

    // ═══ 阶段2：激光扫射 ═══
    private void UpdateSweeping()
    {
        _phaseTimer += Time.deltaTime;
        _sweepProgress = Mathf.Clamp01(_phaseTimer / _sweepDuration);

        float currentAngle = _startAngle - (_sweepAngle * _sweepProgress);
        Vector2 laserDir = AngleToDirection(currentAngle);
        UpdateLaserVisual(currentAngle);

        // 每3帧造成一次伤害
        _damageFrameCounter++;
        if (_damageFrameCounter % 3 == 0)
            HitEnemiesWithLaser(laserDir);

        if (_phaseTimer >= _sweepDuration + 0.1f)
        {
            CleanupLaser();
            _phase = Phase.Done;
            if (_activeInstance == this) _activeInstance = null;
            DestroyImmediate(gameObject);
        }
    }

    private void HitEnemiesWithLaser(Vector2 direction)
    {
        Vector2 origin = (Vector2)(GameReferences.Player?.transform.position ?? transform.position);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, _laserLength);
        foreach (var hit in hits)
        {
            if (!hit.collider.CompareTag("Enemy")) continue;
            var dmg = hit.collider.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;

            dmg.TakeDamage(_laserDamage, Color.white);

            DotBulletHelper.EnsureStatusEffectManager(hit.collider.gameObject);
            var lightMark = hit.collider.GetComponent<LightMarkEffect>();
            if (lightMark == null)
                lightMark = hit.collider.gameObject.AddComponent<LightMarkEffect>();
            lightMark.AddStack(_markDuration, _markMaxStacks);
        }
    }

    // ═══ 工具方法 ═══

    private float GetMouseAngle()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            var cam = GameReferences.MainCamera ?? Camera.main;
            if (cam != null)
            {
                Vector3 screenPos = mouse.position.ReadValue();
                screenPos.z = Mathf.Abs(cam.transform.position.z);
                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
                Vector2 playerPos = GameReferences.Player?.transform.position ?? transform.position;
                Vector2 dir = ((Vector2)worldPos - playerPos).normalized;
                return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
        }
        return 0f;
    }

    private Vector2 AngleToDirection(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    // ═══ 视觉效果 ═══

    private void CreateChargeBarVisual()
    {
        CleanupChargeBar();
        _chargeBarObj = new GameObject("LightChargeBar");
        var sr = _chargeBarObj.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(1f, 1f, 0.8f, 0f);
        sr.sortingOrder = 50;
        _chargeBarObj.transform.localScale = new Vector3(0f, 0.15f, 1f);
        UpdateChargeBarPosition();
    }

    private void UpdateChargeBarVisual(float progress)
    {
        if (_chargeBarObj == null) return;
        UpdateChargeBarPosition();
        _chargeBarObj.transform.localScale = new Vector3(progress * 1.5f, 0.15f, 1f);
        float pulse = 0.8f + Mathf.Sin(Time.time * 10f) * 0.2f;
        var sr = _chargeBarObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 0.8f, pulse);
    }

    private void UpdateChargeBarPosition()
    {
        if (_chargeBarObj == null) return;
        var player = GameReferences.Player;
        if (player != null)
            _chargeBarObj.transform.position = player.transform.position + new Vector3(0, 1.2f, 0);
        else
            _chargeBarObj.transform.position = transform.position + new Vector3(0, 1.2f, 0);
    }

    private void CleanupChargeBar()
    {
        if (_chargeBarObj != null) { DestroyImmediate(_chargeBarObj); _chargeBarObj = null; }
    }

    private void CreateLaserVisual()
    {
        _laserObj = new GameObject("LightBeam");
        var sr = _laserObj.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(1f, 1f, 1f, 0.85f);
        sr.sortingOrder = 60;
        UpdateLaserVisual(_startAngle);
    }

    private void UpdateLaserVisual(float angle)
    {
        if (_laserObj == null) return;
        Vector2 playerPos = GameReferences.Player?.transform.position ?? transform.position;
        Vector2 dir = AngleToDirection(angle);
        _laserObj.transform.position = (Vector3)(playerPos + dir * _laserLength * 0.5f);
        _laserObj.transform.rotation = Quaternion.Euler(0, 0, angle);
        _laserObj.transform.localScale = new Vector3(_laserLength, _laserWidth, 1f);
        float alpha = 0.7f - _sweepProgress * 0.3f;
        var sr = _laserObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 0.95f, alpha);
    }

    private void CleanupLaser()
    {
        if (_laserObj != null)
        {
            var fade = _laserObj.AddComponent<LaserFadeOut>();
            fade.Init(0.3f);
            _laserObj = null;
        }
    }

    private void CleanupVisuals()
    {
        CleanupChargeBar();
        if (_laserObj != null) { DestroyImmediate(_laserObj); _laserObj = null; }
    }

    private void OnDestroy()
    {
        if (_activeInstance == this) _activeInstance = null;
        CleanupVisuals();
    }

    public static LightBulletController Create(Vector2 pos)
    {
        var go = new GameObject("LightBulletController");
        go.transform.position = pos;
        var controller = go.AddComponent<LightBulletController>();
        return controller;
    }
}

/// <summary>
/// 光明标记效果 — 挂载到敌人身上
/// 每层受到伤害增加0.5%，无上限
/// 敌人身上显示层数文字（与其他标记一致的xN格式）
/// </summary>
public class LightMarkEffect : MonoBehaviour
{
    private float _duration = 15f;
    private int _stackCount = 0;
    private float _lastStackTime;
    private Damageable _damageable;
    private GameObject _stackTextObj;

    public int StackCount => _stackCount;

    public void AddStack(float duration, int maxStacks)
    {
        _duration = duration;
        _lastStackTime = Time.time;
        _stackCount++;
        _damageable = GetComponent<Damageable>();

        // 视觉：越叠越亮
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float brightness = Mathf.Min(0.3f, _stackCount * 0.02f);
            sr.color = Color.Lerp(sr.color, Color.white, brightness);
        }

        UpdateStackText();
    }

    /// <summary>
    /// 获取受伤倍率：每层+0.5%，无上限
    /// </summary>
    public float GetDamageMultiplier()
    {
        if (_stackCount <= 0) return 1f;
        return 1f + _stackCount * 0.005f;
    }

    private TextMesh _cachedTextMesh; // 缓存 TextMesh 组件
    private int _lastDisplayStacks = -1; // 只在层数变化时更新文字

    private void UpdateStackText()
    {
        if (_stackTextObj == null)
        {
            _stackTextObj = new GameObject("LightMarkText");
            _stackTextObj.transform.localScale = Vector3.one * 0.3f;

            _cachedTextMesh = _stackTextObj.AddComponent<TextMesh>();
            _cachedTextMesh.characterSize = 0.2f;
            _cachedTextMesh.anchor = TextAnchor.MiddleCenter;
            _cachedTextMesh.alignment = TextAlignment.Center;
            _cachedTextMesh.fontSize = 40;
            _cachedTextMesh.color = new Color(1f, 1f, 0.8f);
            _cachedTextMesh.fontStyle = FontStyle.Bold;
            _lastDisplayStacks = -1; // 强制首次更新
        }

        // 只在层数变化时更新文字内容
        if (_stackCount != _lastDisplayStacks)
        {
            _lastDisplayStacks = _stackCount;
            if (_cachedTextMesh == null) _cachedTextMesh = _stackTextObj.GetComponent<TextMesh>();
            if (_cachedTextMesh != null)
            {
                _cachedTextMesh.text = $"x{_stackCount}";
                float t = Mathf.Clamp01(_stackCount / 50f);
                _cachedTextMesh.color = Color.Lerp(new Color(1f, 1f, 0.7f), new Color(1f, 0.9f, 0.3f), t);
            }
        }
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _lastStackTime = Time.time;
    }

    private void LateUpdate()
    {
        // 持续更新文字位置跟随敌人
        if (_stackTextObj != null)
        {
            _stackTextObj.transform.position = transform.position + new Vector3(0, 0.6f, 0);
            _stackTextObj.transform.rotation = Quaternion.identity;
        }

        if (_stackCount > 0 && Time.time - _lastStackTime > _duration)
        {
            _stackCount = 0;
            if (_stackTextObj != null) { Destroy(_stackTextObj); _stackTextObj = null; }
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
            Destroy(this);
            return;
        }
        if (_damageable != null && _damageable.CurrentHp <= 0)
        {
            if (_stackTextObj != null) Destroy(_stackTextObj);
            Destroy(this);
        }
    }

    private void OnDisable()
    {
        if (_stackTextObj != null) { Destroy(_stackTextObj); _stackTextObj = null; }
        _stackCount = 0;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    private void OnDestroy()
    {
        if (_stackTextObj != null) Destroy(_stackTextObj);
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }
}

/// <summary>
/// 激光渐隐效果
/// </summary>
public class LaserFadeOut : MonoBehaviour
{
    private float _duration;
    private float _startTime;
    private SpriteRenderer _sr;

    public void Init(float duration)
    {
        _duration = duration;
        _startTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_sr == null) return;
        float elapsed = Time.time - _startTime;
        float alpha = 1f - (elapsed / _duration);
        if (alpha <= 0f) { Destroy(gameObject); return; }
        _sr.color = new Color(1f, 1f, 1f, alpha * 0.7f);
    }
}