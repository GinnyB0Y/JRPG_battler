using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public interface ICommandSource
{
    UniTask<BattleUnit> ChooseActorAsync(IReadOnlyList<BattleUnit> candidates, CancellationToken cancellationToken);
    UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken);
}
