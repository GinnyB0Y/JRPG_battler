public class BattleAction
{
    private BattleAction(BattleUnit user, AbilityData ability, BattleUnit target, bool isStunned)
    {
        User = user;
        Ability = ability;
        Target = target;
        IsStunned = isStunned;
    }

    public BattleUnit User { get; }
    public AbilityData Ability { get; }
    public BattleUnit Target { get; }
    public bool IsStunned { get; }

    public bool IsSkip => Ability == null;

    public static BattleAction Skip(BattleUnit user)
    {
        return new BattleAction(user, null, null, false);
    }

    public static BattleAction Stunned(BattleUnit user)
    {
        return new BattleAction(user, null, null, true);
    }

    public static BattleAction Use(BattleUnit user, AbilityData ability, BattleUnit target)
    {
        return new BattleAction(user, ability, target, false);
    }
}
