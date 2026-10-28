using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class AiCommandSource : ICommandSource
{
    private const float HealThreshold = 0.5f;
    private const float StunScoreBonus = 4f;

    private readonly CombatRules rules;
    private readonly TimeSpan thinkDelay;
    private readonly int randomActionChance;
    private readonly Random random;

    public AiCommandSource(CombatRules rules, TimeSpan thinkDelay, int randomActionChance, Random random)
    {
        this.rules = rules;
        this.thinkDelay = thinkDelay;
        this.randomActionChance = randomActionChance;
        this.random = random;
    }

    public async UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        await UniTask.Delay(thinkDelay, cancellationToken: cancellationToken);

        if (random.Next(100) < randomActionChance)
        {
            BattleAction randomAction = ChooseRandom(unit);

            if (randomAction != null)
                return randomAction;
        }

        BattleAction heal = ChooseHeal(unit);

        if (heal != null)
            return heal;

        BattleAction attack = ChooseAttack(unit);

        if (attack != null)
            return attack;
        else
            return BattleAction.Skip(unit);
    }

    private BattleAction ChooseRandom(BattleUnit unit)
    {
        List<AbilityData> abilities = new List<AbilityData>();

        foreach (AbilityData ability in unit.Abilities)
        {
            if (rules.CanUse(unit, ability) && GetSensibleTargets(unit, ability).Count > 0)
                abilities.Add(ability);
        }

        if (abilities.Count == 0)
            return null;

        AbilityData chosen = abilities[random.Next(abilities.Count)];
        List<BattleUnit> targets = GetSensibleTargets(unit, chosen);
        BattleUnit target = targets[random.Next(targets.Count)];

        return BattleAction.Use(unit, chosen, target);
    }

    private List<BattleUnit> GetSensibleTargets(BattleUnit unit, AbilityData ability)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        foreach (BattleUnit target in rules.GetTargets(unit, ability))
        {
            if (ability.Effect == AbilityEffect.Heal && target.MissingHealth == 0)
                continue;

            result.Add(target);
        }

        return result;
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
