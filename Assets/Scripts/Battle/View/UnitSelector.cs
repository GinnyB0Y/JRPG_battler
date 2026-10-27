using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UnitSelector : MonoBehaviour
{
    private const float MaxRayDistance = 500f;
    private const float MarkerBobHeight = 0.3f;
    private const float MarkerBobDuration = 0.6f;
    private const float MarkerSpinDuration = 2f;

    private readonly Dictionary<BattleUnit, UnitView> views = new Dictionary<BattleUnit, UnitView>();

    private Camera mainCamera;
    private Transform activeMarker;
    private UnitIndicator hoverIndicator;
    private UnitIndicator targetIndicator;
    private BattleLoop loop;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Init(IEnumerable<UnitView> unitViews, GameObject activeMarkerPrefab, Transform hoverMarker, Transform targetMarker)
    {
        foreach (UnitView view in unitViews)
            views.Add(view.Unit, view);

        hoverIndicator = new UnitIndicator(hoverMarker);
        targetIndicator = new UnitIndicator(targetMarker);

        if (activeMarkerPrefab != null)
            CreateActiveMarker(activeMarkerPrefab);
    }

    public void Bind(BattleLoop battleLoop)
    {
        loop = battleLoop;
        loop.ActorChoiceStarted += OnActorChoiceStarted;
        loop.TurnStarted += OnTurnStarted;
        loop.BattleEnded += OnBattleEnded;
    }

    private void OnDestroy()
    {
        if (loop == null)
            return;

        loop.ActorChoiceStarted -= OnActorChoiceStarted;
        loop.TurnStarted -= OnTurnStarted;
        loop.BattleEnded -= OnBattleEnded;
    }

    public async UniTask<BattleUnit> SelectActorAsync(IEnumerable<BattleUnit> candidates, CancellationToken cancellationToken)
    {
        List<BattleUnit> actors = candidates.ToList();
        BattleUnit actor = await PickAsync(actors, actors.Contains, hoverIndicator, cancellationToken);
        hoverIndicator.ReturnHome();
        return actor;
    }

    public UniTask<BattleUnit> SelectTargetAsync(IEnumerable<BattleUnit> targets, CancellationToken cancellationToken)
    {
        List<BattleUnit> candidates = targets.ToList();
        return PickAsync(candidates, candidates.Contains, targetIndicator, cancellationToken);
    }

    private async UniTask<BattleUnit> PickAsync(
        IReadOnlyCollection<BattleUnit> hoverable,
        Func<BattleUnit, bool> canPick,
        UnitIndicator indicator,
        CancellationToken cancellationToken)
    {
        BattleUnit hovered = null;

        try
        {
            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                BattleUnit pointed = GetPointedUnit();
                BattleUnit next = pointed != null && hoverable.Contains(pointed) ? pointed : null;

                if (next != hovered)
                {
                    hovered = next;

                    if (hovered != null)
                        indicator.MoveTo(views[hovered]);
                    else
                        indicator.ReturnHome();
                }

                if (hovered != null && canPick(hovered) && Mouse.current.leftButton.wasPressedThisFrame)
                    return hovered;
            }
        }
        catch (OperationCanceledException)
        {
            indicator.ReturnHome();
            throw;
        }
    }

    private BattleUnit GetPointedUnit()
    {
        if (Mouse.current == null || mainCamera == null)
            return null;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return null;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance))
            return null;

        UnitView view = hit.collider.GetComponentInParent<UnitView>();
        return view != null ? view.Unit : null;
    }

    private void CreateActiveMarker(GameObject prefab)
    {
        activeMarker = new GameObject("ActiveMarker").transform;
        activeMarker.SetParent(transform, false);
        activeMarker.gameObject.SetActive(false);

        Transform marker = Instantiate(prefab, activeMarker).transform;
        marker.localPosition = Vector3.zero;
        marker.localRotation = Quaternion.identity;

        marker
            .DOLocalMoveY(MarkerBobHeight, MarkerBobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(marker.gameObject);

        marker
            .DOLocalRotate(new Vector3(0f, 360f, 0f), MarkerSpinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(marker.gameObject);
    }

    private void ShowActiveMarker(BattleUnit unit)
    {
        if (activeMarker == null)
            return;

        UnitView view = views[unit];
        activeMarker.SetParent(view.transform, false);
        activeMarker.localPosition = Vector3.up * view.MarkerHeight;
        activeMarker.gameObject.SetActive(true);
    }

    private void HideActiveMarker()
    {
        if (activeMarker != null)
            activeMarker.gameObject.SetActive(false);
    }

    private void OnActorChoiceStarted(Team team)
    {
        hoverIndicator.ReturnHome();
        targetIndicator.ReturnHome();
        HideActiveMarker();
    }

    private void OnTurnStarted(BattleUnit unit)
    {
        ShowActiveMarker(unit);
    }

    private void OnBattleEnded(Team winner)
    {
        hoverIndicator.ReturnHome();
        targetIndicator.ReturnHome();
        HideActiveMarker();
    }
}
