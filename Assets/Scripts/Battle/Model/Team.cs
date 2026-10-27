public enum Team
{
    Player,
    Enemy
}

public static class TeamExtensions
{
    public static Team Opposite(this Team team)
    {
        return team == Team.Player ? Team.Enemy : Team.Player;
    }
}
