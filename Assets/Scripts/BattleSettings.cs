using System.Collections.Generic;
using System.Linq;

public static class BattleSettings
{
    public const int MaxTeamSize = 4;
    public const int MinNpcCount = 0;
    public const int MaxNpcCount = MaxTeamSize - 1;
    public const int MinEnemyCount = 1;
    public const int MaxEnemyCount = MaxTeamSize;

    private static readonly List<UnitRole> npcRoles = new List<UnitRole>();
    private static readonly List<UnitRole> enemyRoles = new List<UnitRole> { UnitRole.Melee };

    public static int Seed { get; private set; } = System.Environment.TickCount;

    public static IReadOnlyList<UnitRole> NpcRoles => npcRoles;
    public static IReadOnlyList<UnitRole> EnemyRoles => enemyRoles;

    public static IReadOnlyList<UnitRole> GetRoles(Team team)
    {
        if (team == Team.Player)
            return npcRoles;
        else
            return enemyRoles;
    }

    public static void SetRoles(Team team, IEnumerable<UnitRole> roles)
    {
        List<UnitRole> target;

        if (team == Team.Player)
            target = npcRoles;
        else
            target = enemyRoles;

        target.Clear();
        target.AddRange(roles.Take(GetMaxCount(team)));
    }

    public static int GetMinCount(Team team)
    {
        if (team == Team.Player)
            return MinNpcCount;
        else
            return MinEnemyCount;
    }

    public static int GetMaxCount(Team team)
    {
        if (team == Team.Player)
            return MaxNpcCount;
        else
            return MaxEnemyCount;
    }

    public static void RollSeed()
    {
        Seed = System.Environment.TickCount;
    }
}
