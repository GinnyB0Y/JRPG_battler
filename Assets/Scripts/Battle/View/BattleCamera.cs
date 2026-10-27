using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleCamera : MonoBehaviour
{
    [Header("Framing")]
    [SerializeField, Min(1f)] private float distance;
    [SerializeField, Min(0.01f)] private float smoothTime;
    [SerializeField, Min(0f)] private float introDistance;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1f, 0f);

    [Header("Follow")]
    [SerializeField, Range(0f, 1f)] private float followStrength;
    [SerializeField] private Transform enemyTurnPoint;

    [Header("Floating")]
    [SerializeField, Min(0f)] private float floatAmplitude;
    [SerializeField, Min(0f)] private float floatFrequency;
    [SerializeField, Min(0f)] private float swayAngle;

    private readonly List<UnitView> views = new List<UnitView>();

    private BattleLoop loop;
    private UnitView followed;
    private UnitView watched;
    private Vector3 forward;
    private Vector3 basePosition;
    private Vector3 lookPoint;
    private Vector3 positionVelocity;
    private Vector3 lookVelocity;
    private Vector3 focus;
    private float noiseSeed;

    private void Awake()
    {
        forward = transform.forward;
        noiseSeed = Random.Range(0f, 100f);
    }

    public void Bind(BattleLoop battleLoop, IEnumerable<UnitView> unitViews)
    {
        loop = battleLoop;
        loop.TurnStarted += OnTurnStarted;
        loop.BattleEnded += OnBattleEnded;

        views.AddRange(unitViews);
        lookPoint = GetTeamFocus();
        basePosition = lookPoint - forward * introDistance;
        transform.position = basePosition;
    }

    private void OnDestroy()
    {
        if (loop == null)
            return;

        loop.TurnStarted -= OnTurnStarted;
        loop.BattleEnded -= OnBattleEnded;
    }

    private void LateUpdate()
    {
        if (loop == null)
            return;

        Vector3 lookTarget;
        Vector3 positionTarget;

        if (watched != null && enemyTurnPoint != null)
        {
            lookTarget = watched.transform.position + focusOffset;
            positionTarget = enemyTurnPoint.position;
        }
        else
        {
            lookTarget = GetTeamFocus() + GetFollowOffset();
            positionTarget = lookTarget - forward * distance;
        }

        basePosition = Vector3.SmoothDamp(basePosition, positionTarget, ref positionVelocity, smoothTime);
        lookPoint = Vector3.SmoothDamp(lookPoint, lookTarget, ref lookVelocity, smoothTime);

        Vector3 direction = lookPoint - basePosition;
        Quaternion rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : transform.rotation;

        float time = Time.time * floatFrequency;
        Vector3 drift = rotation * GetNoise(time, 0f) * floatAmplitude;
        Quaternion sway = Quaternion.Euler(GetNoise(time, 50f) * swayAngle);

        transform.SetPositionAndRotation(basePosition + drift, rotation * sway);
    }

    private void OnTurnStarted(BattleUnit unit)
    {
        UnitView view = views.FirstOrDefault(candidate => candidate.Unit == unit);

        followed = unit.Team == Team.Player ? view : null;
        watched = unit.Team == Team.Enemy ? view : null;
    }

    private void OnBattleEnded(Team winner)
    {
        followed = null;
        watched = null;
    }

    private Vector3 GetTeamFocus()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (UnitView view in views)
        {
            if (view.Unit.Team != Team.Player || !view.Unit.IsAlive)
                continue;

            sum += view.HomePosition;
            count++;
        }

        if (count > 0)
            focus = sum / count + focusOffset;

        return focus;
    }

    private Vector3 GetFollowOffset()
    {
        if (followed == null)
            return Vector3.zero;

        Vector3 offset = followed.transform.position - followed.HomePosition;
        offset.y = 0f;
        return offset * followStrength;
    }

    private Vector3 GetNoise(float time, float shift)
    {
        return new Vector3(
            Mathf.PerlinNoise(noiseSeed + shift, time) * 2f - 1f,
            Mathf.PerlinNoise(noiseSeed + shift + 10f, time) * 2f - 1f,
            Mathf.PerlinNoise(noiseSeed + shift + 20f, time) * 2f - 1f);
    }
}
