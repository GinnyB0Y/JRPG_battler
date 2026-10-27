using System.Threading;
using Cysharp.Threading.Tasks;

public interface IBattlePresenter
{
    UniTask PlayActionStartAsync(ActionOutcome outcome, CancellationToken cancellationToken);
    UniTask PlayActionImpactAsync(ActionOutcome outcome, CancellationToken cancellationToken);
    UniTask PlayActionEndAsync(ActionOutcome outcome, CancellationToken cancellationToken);
}
