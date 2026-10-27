public class BattleAction
{
    private BattleAction(BattleUnit user, AbilityData ability, BattleUnit target)
    {
        User = user;
        Ability = ability;
        Target = target;
    }

    public BattleUnit User { get; }
    public AbilityData Ability { get; }
    public BattleUnit Target { get; }

    public bool IsSkip => Ability == null;

    public static BattleAction Skip(BattleUnit user)
    {
        return new BattleAction(user, null, null);
    }

    public static BattleAction Use(BattleUnit user, AbilityData ability, BattleUnit target)
    {
        return new BattleAction(user, ability, target);
    }
}
