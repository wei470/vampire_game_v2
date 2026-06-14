using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [System.Serializable]
    public struct CharacterStats
    {
        public string characterId;
        public float moveSpeed;
    }

    [Header("角色属性")]
    public CharacterStats[] characters = new CharacterStats[]
    {
        new CharacterStats { characterId = "mage", moveSpeed = 10f }
    };

    [Header("全局默认")]
    public float defaultMoveSpeed = 10f;

    public float GetMoveSpeed(string characterId)
    {
        if (characters == null) return defaultMoveSpeed;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].characterId == characterId)
                return characters[i].moveSpeed;
        }
        return defaultMoveSpeed;
    }
}
