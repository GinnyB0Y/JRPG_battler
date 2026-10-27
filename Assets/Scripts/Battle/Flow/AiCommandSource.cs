using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

public class AiCommandSource : ICommandSource
{
    private const float HealThreshold = 0.5f;

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
        return ChooseHeal(unit) ?? ChooseAttack(unit) ?? BattleAction.Skip(unit);
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
            let expectedDamage = rules.GetHitChance(unit, ability, target) * ability.Power
            orderby expectedDamage descending, target.Health
            select BattleAction.Use(unit, ability, target);

        return options.FirstOrDefault();
    }
}
