using System;
using System.Collections.Generic;

public class CombatRules
{
    private const float BaseHitChance = 0.75f;
    private const float HitChancePerPoint = 0.05f;
    private const float MinHitChance = 0.05f;
    private const float MaxHitChance = 0.95f;

    private readonly Battlefield field;
    private readonly Random random;

    public CombatRules(Battlefield field, Random random)
    {
        this.field = field;
        this.random = random;
    }

    public bool CanUse(BattleUnit user, AbilityData ability)
    {
        if (!user.IsAlive)
            return false;

        if (ability.RequiresFrontUser && !field.IsFront(user))
            return false;

        return GetTargets(user, ability).Count > 0;
    }

    public List<BattleUnit> GetTargets(BattleUnit user, AbilityData ability)
    {
        Team side;

        if (ability.TargetSide == TargetSide.Enemy)
            side = user.Team.Opposite();
        else
            side = user.Team;

        if (ability.RequiresFrontTarget)
            return field.GetFrontLine(side);
        else
            return field.GetAlive(side);
    }

    public bool IsValid(BattleAction action)
    {
        if (action.IsSkip)
            return action.User.IsAlive;

        return CanUse(action.User, action.Ability) && GetTargets(action.User, action.Ability).Contains(action.Target);
    }

    public float GetHitChance(BattleUnit user, AbilityData ability, BattleUnit target)
    {
        if (ability.Effect != AbilityEffect.Damage)
            return 1f;

        int attack;

        if (field.IsFront(user))
            attack = user.Attack - ability.FrontAttackPenalty;
        else
            attack = user.Attack;

        float chance = BaseHitChance + (attack - target.Defense) * HitChancePerPoint;
        return Math.Clamp(chance, MinHitChance, MaxHitChance);
    }

    public ActionOutcome Resolve(BattleAction action)
    {
        if (action.IsSkip)
            return ActionOutcome.Skipped(action);

        AbilityData ability = action.Ability;
        float hitChance = GetHitChance(action.User, ability, action.Target);
        bool isHit = random.NextDouble() < hitChance;
        int amount;

        if (isHit)
            amount = GetAmount(ability, action.Target);
        else
            amount = 0;

        return new ActionOutcome(action, isHit, amount, hitChance);
    }

    private static int GetAmount(AbilityData ability, BattleUnit target)
    {
        if (ability.Effect == AbilityEffect.Heal)
            return Math.Min(ability.Power, target.MissingHealth);
        else
            return ability.Power;
    }
}
