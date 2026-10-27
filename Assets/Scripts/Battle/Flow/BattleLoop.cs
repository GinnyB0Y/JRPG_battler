using System;
using System.Collections.Generic;
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

    public BattleLoop(Battlefield field, CombatRules rules, ICommandSource playerCommands, ICommandSource enemyCommands, IBattlePresenter presenter)
    {
        this.field = field;
        this.rules = rules;
        this.playerCommands = playerCommands;
        this.enemyCommands = enemyCommands;
        this.presenter = presenter;
    }

    public event Action<int> RoundStarted;
    public event Action<BattleUnit> TurnStarted;
    public event Action<ActionOutcome> ActionResolved;
    public event Action<Team> BattleEnded;

    public int Round => queue.Round;
    public BattleUnit ActiveUnit => queue.Current;

    public List<BattleUnit> GetUpcomingUnits()
    {
        return queue.GetUpcoming();
    }

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

        while (!field.TryGetWinner(out _) && queue.TryGetNext(out BattleUnit unit))
            await PlayTurnAsync(unit, cancellationToken);
    }

    private async UniTask PlayTurnAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        TurnStarted?.Invoke(unit);

        BattleAction action;

        if (unit.IsStunned)
        {
            action = BattleAction.Stunned(unit);
            unit.SpendStunnedTurn();
        }
        else
        {
            action = await ChooseActionAsync(unit, cancellationToken);
        }

        ActionOutcome outcome = rules.Resolve(action);

        await presenter.PlayActionStartAsync(outcome, cancellationToken);
        outcome.Apply();
        ActionResolved?.Invoke(outcome);
        await presenter.PlayActionImpactAsync(outcome, cancellationToken);
        await presenter.PlayActionEndAsync(outcome, cancellationToken);
    }

    private async UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        ICommandSource commands;

        if (unit.Team == Team.Player)
            commands = playerCommands;
        else
            commands = enemyCommands;

        BattleAction action = await commands.ChooseActionAsync(unit, cancellationToken);

        if (rules.IsValid(action))
            return action;

        Debug.LogWarning($"{unit.Name} выбрал чёт не то");
        return BattleAction.Skip(unit);
    }
}
