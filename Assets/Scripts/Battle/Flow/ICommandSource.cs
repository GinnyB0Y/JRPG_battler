using System.Threading;
using Cysharp.Threading.Tasks;

public interface ICommandSource
{
    UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken);
}
