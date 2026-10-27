using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BattleLoop
{
    private readonly Battlefield field;
    private readonly CombatRules rules;
    private readonly ICommandSource playerCommands;
    private readonly ICommandSource enemyCommands;
    private readonly IBattlePresenter presenter;
    private readonly InitiativeQueue queue = new InitiativeQueue();

    public BattleLoop(
        Battlefield field,
        CombatRules rules,
        ICommandSource playerCommands,
        ICommandSource enemyCommands,
        IBattlePresenter presenter)
    {
        this.field = field;
        this.rules = rules;
        this.playerCommands = playerCommands;
        this.enemyCommands = enemyCommands;
        this.presenter = presenter;
    }

    public event Action<int> RoundStarted;
    public event Action<Team> ActorChoiceStarted;
    public event Action<BattleUnit> TurnStarted;
    public event Action<ActionOutcome> ActionResolved;
    public event Action<Team> BattleEnded;

    public int Round => queue.Round;
    public BattleUnit ActiveUnit => queue.Current;
    public IEnumerable<BattleUnit> UpcomingUnits => queue.Upcoming;

    public async UniTask<Team> RunAsync(CancellationToken cancellationToken)
    {
        Team winner;

        while (!field.TryGetWinner(out winner))
            await PlayRoundAsync(cancellationToken);

        BattleEnded?.Invoke(winner);
        return winner;
    }

    private async UniTask PlayRoundAsync(CancellationToken cancellationToken)
    {
        queue.StartRound(field.Units);
        RoundStarted?.Invoke(queue.Round);

        while (!field.TryGetWinner(out _) && queue.TryPeekNext(out BattleUnit next))
            await PlayTurnAsync(next, cancellationToken);
    }

    private async UniTask PlayTurnAsync(BattleUnit next, CancellationToken cancellationToken)
    {
        ICommandSource commands;

        if (next.Team == Team.Player)
            commands = playerCommands;
        else
            commands = enemyCommands;

        ActorChoiceStarted?.Invoke(next.Team);

        BattleUnit unit;

        if (next.IsStunned)
            unit = next;
        else
            unit = await ChooseActorAsync(commands, next, cancellationToken);

        queue.Begin(unit);
        TurnStarted?.Invoke(unit);

        BattleAction action;

        if (unit.IsStunned)
            action = BattleAction.Stunned(unit);
        else
            action = await ChooseActionAsync(commands, unit, cancellationToken);

        if (action.IsStunned)
            unit.SpendStunnedTurn();

        ActionOutcome outcome = rules.Resolve(action);

        await presenter.PlayActionStartAsync(outcome, cancellationToken);
        outcome.Apply();
        ActionResolved?.Invoke(outcome);
        await presenter.PlayActionImpactAsync(outcome, cancellationToken);
        await presenter.PlayActionEndAsync(outcome, cancellationToken);
    }

    private async UniTask<BattleUnit> ChooseActorAsync(ICommandSource commands, BattleUnit next, CancellationToken cancellationToken)
    {
        IReadOnlyList<BattleUnit> candidates = queue.GetCandidates(next.Team)
            .Where(candidate => !candidate.IsStunned)
            .ToList();

        BattleUnit unit = await commands.ChooseActorAsync(candidates, cancellationToken);

        if (candidates.Contains(unit))
            return unit;

        Debug.LogWarning($"{unit?.Name} cannot act now, {next.Name} takes the turn");
        return next;
    }

    private async UniTask<BattleAction> ChooseActionAsync(ICommandSource commands, BattleUnit unit, CancellationToken cancellationToken)
    {
        BattleAction action = await commands.ChooseActionAsync(unit, cancellationToken);

        if (rules.IsValid(action))
            return action;

        Debug.LogWarning($"{unit.Name} выбрал чёт не то");
        return BattleAction.Skip(unit);
    }
}
