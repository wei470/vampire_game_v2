using UnityEngine;

/// <summary>
/// 敌人移速显示器 — 在敌人脚下显示当前移速百分比（如 "95%"）。
/// 读取 EnemyBase.FrostSlowMultiplier 和 PoisonSwampMultiplier。
/// 减速公式为加法叠加：每层 -5%，100% → 95% → 90% → ...
/// </summary>
public class EnemySpeedBar : MonoBehaviour
{
    private EnemyBase _enemyBase;
    private TextMesh _textMesh;
    private GameObject _textObj;
    private float _lastDisplayed = -1f;

    private void OnEnable()
    {
        _enemyBase = GetComponent<EnemyBase>();
        EnsureTextCreated();
    }

    private void OnDisable()
    {
        if (_textObj != null) _textObj.SetActive(false);
    }

    private void EnsureTextCreated()
    {
        if (_textObj != null) return;

        _textObj = new GameObject("SpeedText");
        _textObj.transform.SetParent(transform);
        _textObj.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        _textObj.transform.localScale = Vector3.one * 0.15f;

        _textMesh = _textObj.AddComponent<TextMesh>();
        _textMesh.characterSize = 0.5f;
        _textMesh.anchor = TextAnchor.UpperCenter;
        _textMesh.alignment = TextAlignment.Center;
        _textMesh.fontSize = 60;
        _textMesh.fontStyle = FontStyle.Bold;
        _textMesh.color = Color.white;
    }

    private void LateUpdate()
    {
        if (_enemyBase == null) return;
        EnsureTextCreated();

        float speedPct = GetCurrentSpeedPercent();

        if (Mathf.Approximately(speedPct, _lastDisplayed)) return;
        _lastDisplayed = speedPct;

        int pct = Mathf.RoundToInt(speedPct * 100f);
        _textMesh.text = $"{pct}%";

        if (pct >= 99) _textMesh.color = Color.clear;
        else if (pct >= 70) _textMesh.color = Color.white;
        else if (pct >= 50) _textMesh.color = Color.yellow;
        else _textMesh.color = new Color(1f, 0.3f, 0.3f);

        _textObj.SetActive(pct < 99);
    }

    private float GetCurrentSpeedPercent()
    {
        float frostMult = _enemyBase.FrostSlowMultiplier;
        float swampMult = _enemyBase.PoisonSwampMultiplier;
        return frostMult * swampMult;
    }
}
