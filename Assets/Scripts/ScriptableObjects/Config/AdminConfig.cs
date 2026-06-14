using UnityEngine;

[CreateAssetMenu(fileName = "AdminConfig", menuName = "Configs/AdminConfig")]
public class AdminConfig : ScriptableObject
{
    [Tooltip("0 = 普通模式（只显示 Start Game）\n1 = 管理员模式（显示全部按钮和快捷键）")]
    [Range(0, 1)]
    public int admin = 0;

    private static AdminConfig _instance;
    public static AdminConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<AdminConfig>("Configs/AdminConfig");
                if (_instance == null)
                {
                    _instance = CreateInstance<AdminConfig>();
                    _instance.admin = 0;
                }
            }
            return _instance;
        }
    }

    public bool IsAdmin => admin == 1;
}
