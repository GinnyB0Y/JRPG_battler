using UnityEngine;

public static class BattleSettings
{
    public const int MinTeamSize = 1;
    public const int MaxTeamSize = 4;

    private static int playerTeamSize = MinTeamSize;
    private static int enemyTeamSize = MinTeamSize;

    public static int PlayerTeamSize
    {
        get => playerTeamSize;
        set => playerTeamSize = ClampTeamSize(value);
    }

    public static int EnemyTeamSize
    {
        get => enemyTeamSize;
        set => enemyTeamSize = ClampTeamSize(value);
    }

    private static int ClampTeamSize(int value)
    {
        return Mathf.Clamp(value, MinTeamSize, MaxTeamSize);
    }
}
