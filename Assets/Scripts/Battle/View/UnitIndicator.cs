using DG.Tweening;
using UnityEngine;

public class UnitIndicator
{
    private const float MoveDuration = 0.2f;

    private readonly Transform transform;
    private readonly Vector3 homePosition;
    private Tween tween;

    public UnitIndicator(Transform transform)
    {
        this.transform = transform;

        if (transform != null)
            homePosition = transform.position;
    }

    public void MoveTo(UnitView view)
    {
        Move(view.transform.position + Vector3.up * view.MarkerHeight);
    }

    public void ReturnHome()
    {
        Move(homePosition);
    }

    private void Move(Vector3 position)
    {
        if (transform == null)
            return;

        tween?.Kill();
        tween = transform
            .DOMove(position, MoveDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(transform.gameObject);
    }
}
