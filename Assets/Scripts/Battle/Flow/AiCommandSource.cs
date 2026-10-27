using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class AiCommandSource : ICommandSource
{
    private const float HealThreshold = 0.5f;
    private const float StunScoreBonus = 4f;

    private readonly CombatRules rules;
    private readonly TimeSpan thinkDelay;

    public AiCommandSource(CombatRules rules, TimeSpan thinkDelay)
    {
        this.rules = rules;
        this.thinkDelay = thinkDelay;
    }

    public async UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        await UniTask.Delay(thinkDelay, cancellationToken: cancellationToken);

        BattleAction heal = ChooseHeal(unit);

        if (heal != null)
            return heal;

        BattleAction attack = ChooseAttack(unit);

        if (attack != null)
            return attack;
        else
            return BattleAction.Skip(unit);
    }

    private BattleAction ChooseHeal(BattleUnit unit)
    {
        BattleAction best = null;
        float bestRatio = HealThreshold;
        int bestPower = 0;

        foreach (AbilityData ability in unit.Abilities)
        {
            if (ability.Effect != AbilityEffect.Heal || !rules.CanUse(unit, ability))
                continue;

            foreach (BattleUnit target in rules.GetTargets(unit, ability))
            {
                bool isBetter = target.HealthRatio < bestRatio
                    || (target.HealthRatio == bestRatio && ability.Power > bestPower);

                if (isBetter)
                {
                    best = BattleAction.Use(unit, ability, target);
                    bestRatio = target.HealthRatio;
                    bestPower = ability.Power;
                }
            }
        }

        return best;
    }

    private BattleAction ChooseAttack(BattleUnit unit)
    {
        BattleAction best = null;
        float bestScore = 0f;
        int bestHealth = 0;

        foreach (AbilityData ability in unit.Abilities)
        {
            if (ability.Effect != AbilityEffect.Damage || !rules.CanUse(unit, ability))
                continue;

            foreach (BattleUnit target in rules.GetTargets(unit, ability))
            {
                float score = rules.GetHitChance(unit, ability, target) * (ability.Power + GetStunBonus(ability, target));
                bool isBetter = best == null
                    || score > bestScore
                    || (score == bestScore && target.Health < bestHealth);

                if (isBetter)
                {
                    best = BattleAction.Use(unit, ability, target);
                    bestScore = score;
                    bestHealth = target.Health;
                }
            }
        }

        return best;
    }

    private static float GetStunBonus(AbilityData ability, BattleUnit target)
    {
        if (ability.Stuns && !target.IsStunned)
            return StunScoreBonus;
        else
            return 0f;
    }
}
