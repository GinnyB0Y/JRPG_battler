using System;
using System.Collections.Generic;
using System.Linq;
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

    public UniTask<BattleUnit> ChooseActorAsync(IReadOnlyList<BattleUnit> candidates, CancellationToken cancellationToken)
    {
        return UniTask.FromResult(candidates[0]);
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
        IEnumerable<BattleAction> options =
            from ability in unit.Abilities
            where ability.Effect == AbilityEffect.Heal && rules.CanUse(unit, ability)
            from target in rules.GetTargets(unit, ability)
            where target.HealthRatio < HealThreshold
            orderby target.HealthRatio, ability.Power descending
            select BattleAction.Use(unit, ability, target);

        return options.FirstOrDefault();
    }

    private BattleAction ChooseAttack(BattleUnit unit)
    {
        IEnumerable<BattleAction> options =
            from ability in unit.Abilities
            where ability.Effect == AbilityEffect.Damage && rules.CanUse(unit, ability)
            from target in rules.GetTargets(unit, ability)
            let score = rules.GetHitChance(unit, ability, target) * (ability.Power + GetStunBonus(ability, target))
            orderby score descending, target.Health
            select BattleAction.Use(unit, ability, target);

        return options.FirstOrDefault();
    }

    private static float GetStunBonus(AbilityData ability, BattleUnit target)
    {
        if (ability.Stuns && !target.IsStunned)
            return StunScoreBonus;
        else
            return 0f;
    }
}
