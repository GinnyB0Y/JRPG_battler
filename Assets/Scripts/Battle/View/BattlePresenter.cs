using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class BattlePresenter : MonoBehaviour, IBattlePresenter
{
    private const float StrikeDistance = 1.3f;
    private const float StrikeDuration = 0.25f;
    private const float ReturnDuration = 0.3f;
    private const float PopupDuration = 0.7f;
    private const float PopupRise = 1f;

    private static readonly Color DamageColor = new Color(1f, 0.35f, 0.3f);
    private static readonly Color HealColor = new Color(0.4f, 1f, 0.45f);
    private static readonly Color MissColor = new Color(0.85f, 0.85f, 0.85f);
    private static readonly Color StunColor = new Color(1f, 0.85f, 0.3f);

    private readonly Dictionary<BattleUnit, UnitView> views = new Dictionary<BattleUnit, UnitView>();
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Register(UnitView view)
    {
        views.Add(view.Unit, view);
    }

    public UniTask PlayActionStartAsync(ActionOutcome outcome, CancellationToken cancellationToken)
    {
        UnitView user = views[outcome.User];

        if (outcome.IsStunSkip)
            return UniTask.WhenAll(
                user.PlayHitAsync(cancellationToken),
                ShowPopupAsync(user.BodyPosition + Vector3.up, "Оглушён", StunColor, cancellationToken));

        if (outcome.IsSkip)
            return user.HopAsync(cancellationToken);

        UnitView target = views[outcome.Target];

        switch (outcome.Ability.Visual)
        {
            case AbilityVisual.Strike:
                return user.MoveToAsync(GetStrikePosition(user, target), StrikeDuration, cancellationToken);
            case AbilityVisual.Projectile:
                return LaunchProjectileAsync(outcome.Ability.ProjectilePrefab, user.BodyPosition, target.BodyPosition, cancellationToken);
            default:
                return user.HopAsync(cancellationToken);
        }
    }

    public async UniTask PlayActionImpactAsync(ActionOutcome outcome, CancellationToken cancellationToken)
    {
        if (outcome.IsSkip)
            return;

        UnitView target = views[outcome.Target];

        await UniTask.WhenAll(
            PlayTargetReactionAsync(outcome, target, cancellationToken),
            ShowPopupAsync(target.BodyPosition + Vector3.up, GetPopupText(outcome), GetPopupColor(outcome), cancellationToken));

        if (outcome.KilledTarget)
            await target.PlayDeathAsync(cancellationToken);
    }

    public UniTask PlayActionEndAsync(ActionOutcome outcome, CancellationToken cancellationToken)
    {
        if (outcome.IsSkip || outcome.Ability.Visual != AbilityVisual.Strike)
            return UniTask.CompletedTask;

        UnitView user = views[outcome.User];
        return user.MoveToAsync(user.HomePosition, ReturnDuration, cancellationToken);
    }

    private static UniTask PlayTargetReactionAsync(ActionOutcome outcome, UnitView target, CancellationToken cancellationToken)
    {
        if (!outcome.IsHit)
            return target.PlayDodgeAsync(cancellationToken);

        return outcome.IsHeal ? target.PlayHealAsync(cancellationToken) : target.PlayHitAsync(cancellationToken);
    }

    private static Vector3 GetStrikePosition(UnitView user, UnitView target)
    {
        Vector3 direction = (user.HomePosition - target.HomePosition).normalized;
        return target.HomePosition + direction * StrikeDistance;
    }

    private async UniTask LaunchProjectileAsync(Projectile prefab, Vector3 from, Vector3 to, CancellationToken cancellationToken)
    {
        if (prefab == null)
        {
            return;
        }

        Projectile projectile = Instantiate(prefab, from, Quaternion.identity, transform);

        try
        {
            await projectile.FlyAsync(to, cancellationToken);
        }
        finally
        {
            if (projectile != null)
                Destroy(projectile.gameObject);
        }
    }

    private async UniTask ShowPopupAsync(Vector3 position, string text, Color color, CancellationToken cancellationToken)
    {
        GameObject popupObject = new GameObject("Popup", typeof(RectTransform));
        popupObject.transform.SetParent(transform, true);
        popupObject.transform.position = position;

        if (mainCamera != null)
            popupObject.transform.rotation = mainCamera.transform.rotation;

        TextMeshPro popup = popupObject.AddComponent<TextMeshPro>();
        popup.text = text;
        popup.color = color;
        popup.fontSize = 7f;
        popup.fontStyle = FontStyles.Bold;
        popup.alignment = TextAlignmentOptions.Center;
        popup.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

        try
        {
            await DOTween.Sequence()
                .Append(popupObject.transform.DOMoveY(position.y + PopupRise, PopupDuration).SetEase(Ease.OutQuad))
                .Join(DOTween.To(() => popup.alpha, alpha => popup.alpha = alpha, 0f, PopupDuration).SetEase(Ease.InQuad))
                .SetLink(popupObject)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
        }
        finally
        {
            if (popupObject != null)
                Destroy(popupObject);
        }
    }

    private static string GetPopupText(ActionOutcome outcome)
    {
        if (!outcome.IsHit)
            return "Промах";

        if (outcome.StunnedTarget)
            return $"-{outcome.Amount}\nОглушён";

        return outcome.IsHeal ? $"+{outcome.Amount}" : $"-{outcome.Amount}";
    }

    private static Color GetPopupColor(ActionOutcome outcome)
    {
        if (!outcome.IsHit)
            return MissColor;

        return outcome.IsHeal ? HealColor : DamageColor;
    }
}
