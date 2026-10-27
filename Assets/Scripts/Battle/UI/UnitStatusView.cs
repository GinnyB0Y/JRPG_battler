using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitStatusView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Image healthFill;
    [SerializeField] private GameObject activeHighlight;
    [SerializeField] private GameObject stunIcon;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float deadAlpha = 0.4f;

    public BattleUnit Unit { get; private set; }

    public void Bind(BattleUnit unit)
    {
        Unit = unit;
        Unit.HealthChanged += OnHealthChanged;
        Unit.StunChanged += OnStunChanged;

        nameText.text = unit.Name;

        if (statsText != null)
            statsText.text = $"ATK {unit.Attack}   DEF {unit.Defense}   SPD {unit.Speed}";

        SetHighlighted(false);
        Refresh();
    }

    private void OnDestroy()
    {
        if (Unit != null)
        {
            Unit.HealthChanged -= OnHealthChanged;
            Unit.StunChanged -= OnStunChanged;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (activeHighlight != null)
            activeHighlight.SetActive(highlighted);
    }

    private void OnHealthChanged(BattleUnit _)
    {
        Refresh();
    }

    private void OnStunChanged(BattleUnit _)
    {
        Refresh();
    }

    private void Refresh()
    {
        healthText.text = Unit.IsAlive ? $"{Unit.Health}/{Unit.MaxHealth}" : "DEAD";

        if (healthFill != null)
            healthFill.fillAmount = Unit.HealthRatio;

        if (canvasGroup != null)
            canvasGroup.alpha = Unit.IsAlive ? 1f : deadAlpha;

        if (stunIcon != null)
            stunIcon.SetActive(Unit.IsStunned);
    }
}
