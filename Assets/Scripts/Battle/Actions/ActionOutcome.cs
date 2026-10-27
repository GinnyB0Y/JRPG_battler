public class ActionOutcome
{
    public ActionOutcome(BattleAction action, bool isHit, int amount, float hitChance)
    {
        Action = action;
        IsHit = isHit;
        Amount = amount;
        HitChance = hitChance;
    }

    public BattleAction Action { get; }
    public bool IsHit { get; }
    public int Amount { get; }
    public float HitChance { get; }

    public BattleUnit User => Action.User;
    public BattleUnit Target => Action.Target;
    public AbilityData Ability => Action.Ability;
    public bool IsSkip => Action.IsSkip;
    public bool IsStunSkip => Action.IsStunned;
    public bool IsHeal => !IsSkip && Ability.Effect == AbilityEffect.Heal;
    public bool KilledTarget => !IsSkip && IsHit && !Target.IsAlive;
    public bool StunnedTarget => !IsSkip && IsHit && Ability.Stuns && Target.IsAlive;

    public static ActionOutcome Skipped(BattleAction action)
    {
        return new ActionOutcome(action, false, 0, 0f);
    }

    public void Apply()
    {
        if (IsSkip || !IsHit)
            return;

        if (IsHeal)
            Target.Heal(Amount);
        else
            Target.TakeDamage(Amount);

        if (StunnedTarget)
            Target.Stun(Ability.StunTurns);
    }
}
