using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 14f;
    [SerializeField] private Ease ease = Ease.Linear;

    public UniTask FlyAsync(Vector3 target, CancellationToken cancellationToken)
    {
        Vector3 direction = target - transform.position;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        float duration = direction.magnitude / speed;

        return transform
            .DOMove(target, duration)
            .SetEase(ease)
            .SetLink(gameObject)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
    }
}
