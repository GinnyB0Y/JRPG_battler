public enum Team
{
    Player,
    Enemy
}

public static class TeamExtensions
{
    public static Team Opposite(this Team team)
    {
        if (team == Team.Player)
            return Team.Enemy;
        else
            return Team.Player;
    }
}
