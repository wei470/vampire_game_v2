using UnityEngine;

/// <summary>
/// 下雪天系统 — 解锁后挂载到玩家身上。
/// 功能：
/// 1. 场地略微变蓝（修改 Camera backgroundColor）
/// 2. 每 1 秒在随机敌人位置落下一颗冰雹
/// </summary>
public class SnowyDaySystem : MonoBehaviour
{
    private float _hailAccumulator;
    private const float HAIL_INTERVAL = 1f;
    private const float HAIL_SPEED = 20f;
    private const float HAIL_LIFETIME = 1.5f;
    private const int HAIL_FROST_STACKS = 2;

    private Camera _cam;
    private Color _originalBg;
    private bool _bgApplied;

    private void OnEnable()
    {
        _cam = Camera.main;
        if (_cam != null) _originalBg = _cam.backgroundColor;
        _hailAccumulator = 0f;
        _bgApplied = false;
    }

    private void OnDisable()
    {
        if (_cam != null && _bgApplied) _cam.backgroundColor = _originalBg;
    }

    private void Update()
    {
        var mage = GameReferences.DotCharacterPassive as MagePassive;
        if (mage == null || !mage.SnowyDay) return;

        // 场地变蓝
        if (!_bgApplied && _cam != null)
        {
            _cam.backgroundColor = Color.Lerp(_originalBg, new Color(0.15f, 0.18f, 0.3f), 0.4f);
            _bgApplied = true;
        }

        // 每 1 秒落冰雹
        _hailAccumulator += Time.deltaTime;
        if (_hailAccumulator >= HAIL_INTERVAL)
        {
            _hailAccumulator -= HAIL_INTERVAL;
            DropHail();
        }
    }

    private void DropHail()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // 从屏幕顶部随机位置落下
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        Vector3 cp = cam.transform.position;
        float spawnX = cp.x + Random.Range(-w, w);
        float spawnY = cp.y + h + 1f;
        Vector2 spawnPos = new Vector2(spawnX, spawnY);
        Vector2 dir = Vector2.down;

        HailBullet.Create(spawnPos, dir, HAIL_SPEED, HAIL_LIFETIME, HAIL_FROST_STACKS);
    }
}
