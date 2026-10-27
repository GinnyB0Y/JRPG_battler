using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    private const float BodyHeight = 1f;
    private const float MarkerOffset = 2.6f;

    public BattleUnit Unit { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public Vector3 BodyPosition => transform.position + Vector3.up * BodyHeight;
    public float MarkerHeight => MarkerOffset;

    public void Bind(BattleUnit unit)
    {
        Unit = unit;
        HomePosition = transform.position;
    }

    public UniTask MoveToAsync(Vector3 position, float duration, CancellationToken cancellationToken)
    {
        return Await(transform.DOMove(position, duration).SetEase(Ease.InOutQuad), cancellationToken);
    }

    public UniTask HopAsync(CancellationToken cancellationToken)
    {
        return Await(transform.DOJump(HomePosition, 0.5f, 1, 0.35f), cancellationToken);
    }

    public UniTask PlayHitAsync(CancellationToken cancellationToken)
    {
        return Await(transform.DOShakePosition(0.3f, new Vector3(0.3f, 0f, 0.3f), 25), cancellationToken);
    }

    public UniTask PlayDodgeAsync(CancellationToken cancellationToken)
    {
        Vector3 dodgePosition = HomePosition + transform.right * 0.7f;
        return Await(transform.DOMove(dodgePosition, 0.15f).SetLoops(2, LoopType.Yoyo), cancellationToken);
    }

    public UniTask PlayHealAsync(CancellationToken cancellationToken)
    {
        return Await(transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 6), cancellationToken);
    }

    public UniTask PlayDeathAsync(CancellationToken cancellationToken)
    {
        Quaternion fallen = transform.rotation * Quaternion.Euler(-90f, 0f, 0f);
        return Await(transform.DORotateQuaternion(fallen, 0.5f).SetEase(Ease.InQuad), cancellationToken);
    }

    private UniTask Await(Tween tween, CancellationToken cancellationToken)
    {
        return tween.SetLink(gameObject).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
    }
}
