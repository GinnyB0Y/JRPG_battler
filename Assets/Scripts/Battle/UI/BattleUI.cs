using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour, ICommandSource
{
    private const string PlayerColor = "#6FB7FF";
    private const string EnemyColor = "#FF7A6F";
    private const string ActiveColor = "#FFD84D";

    [Header("Turn Queue")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text[] queueSlots;

    [Header("Battle Log")]
    [SerializeField] private TMP_Text logText;
    [SerializeField, Min(1)] private int maxLogLines = 12;

    [Header("Unit Status")]
    [SerializeField] private UnitStatusView[] playerStatusViews;
    [SerializeField] private UnitStatusView[] enemyStatusViews;

    [Header("Action Choice")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TMP_Text choiceTitleText;
    [SerializeField] private Button[] choiceButtons;

    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button menuButton;
    [SerializeField] private Color victoryColor = new Color(0.45f, 1f, 0.5f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.4f, 0.35f);

    private readonly Queue<string> logLines = new Queue<string>();
    private readonly List<UnitStatusView> boundStatusViews = new List<UnitStatusView>();

    private TMP_Text[] choiceLabels;
    private UniTaskCompletionSource<int> pendingChoice;
    private BattleLoop loop;
    private CombatRules rules;
    private string menuSceneName;

    private void Awake()
    {
        choiceLabels = choiceButtons.Select(button => button.GetComponentInChildren<TMP_Text>(true)).ToArray();

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].onClick.AddListener(() => OnChoiceClicked(index));
        }

        menuButton.onClick.AddListener(OnMenuButtonClicked);

        choicePanel.SetActive(false);
        resultPanel.SetActive(false);
        logText.text = string.Empty;
        roundText.text = string.Empty;
        ShowSlots(queueSlots, 0);
        ShowSlots(playerStatusViews, 0);
        ShowSlots(enemyStatusViews, 0);
    }

    private void OnDestroy()
    {
        foreach (Button button in choiceButtons)
            button.onClick.RemoveAllListeners();

        menuButton.onClick.RemoveListener(OnMenuButtonClicked);

        if (loop == null)
            return;

        loop.RoundStarted -= OnRoundStarted;
        loop.TurnStarted -= OnTurnStarted;
        loop.ActionResolved -= OnActionResolved;
        loop.BattleEnded -= OnBattleEnded;
    }

    public void Bind(BattleLoop battleLoop, CombatRules combatRules, Battlefield field, string menuScene)
    {
        loop = battleLoop;
        rules = combatRules;
        menuSceneName = menuScene;

        loop.RoundStarted += OnRoundStarted;
        loop.TurnStarted += OnTurnStarted;
        loop.ActionResolved += OnActionResolved;
        loop.BattleEnded += OnBattleEnded;

        BindStatusViews(field.GetTeam(Team.Player), playerStatusViews);
        BindStatusViews(field.GetTeam(Team.Enemy), enemyStatusViews);
    }

    public async UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        while (true)
        {
            AbilityData ability = await ChooseAbilityAsync(unit, cancellationToken);

            if (ability == null)
                return BattleAction.Skip(unit);

            BattleUnit target = await ChooseTargetAsync(unit, ability, cancellationToken);

            if (target != null)
                return BattleAction.Use(unit, ability, target);
        }
    }

    private async UniTask<AbilityData> ChooseAbilityAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        List<AbilityData> abilities = unit.Abilities.ToList();
        List<ChoiceOption> options = abilities
            .Select(ability => new ChoiceOption(ability.DisplayName, rules.CanUse(unit, ability)))
            .ToList();
        options.Add(new ChoiceOption("Skip", true));

        int index = await ChooseAsync($"{unit.Name}: choose an action", options, cancellationToken);
        return index < abilities.Count ? abilities[index] : null;
    }

    private async UniTask<BattleUnit> ChooseTargetAsync(BattleUnit unit, AbilityData ability, CancellationToken cancellationToken)
    {
        List<BattleUnit> targets = rules.GetTargets(unit, ability).ToList();
        List<ChoiceOption> options = targets
            .Select(target => new ChoiceOption(FormatTarget(unit, ability, target), true))
            .ToList();
        options.Add(new ChoiceOption("Back", true));

        int index = await ChooseAsync($"{ability.DisplayName}: choose a target", options, cancellationToken);
        return index < targets.Count ? targets[index] : null;
    }

    private async UniTask<int> ChooseAsync(string title, IReadOnlyList<ChoiceOption> options, CancellationToken cancellationToken)
    {
        if (options.Count > choiceButtons.Length)
            throw new InvalidOperationException($"{options.Count} options do not fit into {choiceButtons.Length} choice buttons");

        UniTaskCompletionSource<int> completion = new UniTaskCompletionSource<int>();
        pendingChoice = completion;
        choiceTitleText.text = title;

        for (int i = 0; i < options.Count; i++)
        {
            choiceLabels[i].text = options[i].Label;
            choiceButtons[i].interactable = options[i].IsAvailable;
        }

        ShowSlots(choiceButtons, options.Count);
        choicePanel.SetActive(true);

        try
        {
            using (cancellationToken.Register(() => completion.TrySetCanceled()))
                return await completion.Task;
        }
        finally
        {
            pendingChoice = null;

            if (choicePanel != null)
                choicePanel.SetActive(false);
        }
    }

    private void OnChoiceClicked(int index)
    {
        pendingChoice?.TrySetResult(index);
    }

    private void BindStatusViews(IEnumerable<BattleUnit> team, UnitStatusView[] views)
    {
        List<BattleUnit> units = team.OrderBy(unit => unit.Slot).Take(views.Length).ToList();

        for (int i = 0; i < units.Count; i++)
        {
            views[i].Bind(units[i]);
            boundStatusViews.Add(views[i]);
        }

        ShowSlots(views, units.Count);
    }

    private void OnRoundStarted(int round)
    {
        AddLog($"<b>Round {round}</b>");
        RefreshQueue();
    }

    private void OnTurnStarted(BattleUnit unit)
    {
        RefreshQueue();
        HighlightStatus(unit);
    }

    private void OnActionResolved(ActionOutcome outcome)
    {
        AddLog(Describe(outcome));

        if (outcome.KilledTarget)
            AddLog($"{Colored(outcome.Target)} dies");

        RefreshQueue();
    }

    private void OnBattleEnded(Team winner)
    {
        bool isVictory = winner == Team.Player;

        AddLog(isVictory ? "<b>Victory</b>" : "<b>Defeat</b>");
        HighlightStatus(null);
        ShowSlots(queueSlots, 0);
        resultText.text = isVictory ? "Victory" : "Defeat";
        resultText.color = isVictory ? victoryColor : defeatColor;
        resultPanel.SetActive(true);
    }

    private void HighlightStatus(BattleUnit activeUnit)
    {
        foreach (UnitStatusView view in boundStatusViews)
            view.SetHighlighted(view.Unit == activeUnit);
    }

    private void RefreshQueue()
    {
        List<string> entries = loop.UpcomingUnits.Select(Colored).ToList();

        if (loop.ActiveUnit != null)
            entries.Insert(0, $"<color={ActiveColor}><b>[{loop.ActiveUnit.Name}]</b></color>");

        int count = Mathf.Min(entries.Count, queueSlots.Length);

        for (int i = 0; i < count; i++)
            queueSlots[i].text = entries[i];

        ShowSlots(queueSlots, count);
        roundText.text = loop.Round.ToString();
    }

    private void AddLog(string line)
    {
        logLines.Enqueue(line);

        while (logLines.Count > maxLogLines)
            logLines.Dequeue();

        logText.text = string.Join("\n", logLines);
    }

    private string Describe(ActionOutcome outcome)
    {
        string user = Colored(outcome.User);

        if (outcome.IsSkip)
            return $"{user} skips the turn";

        string target = Colored(outcome.Target);
        string ability = outcome.Ability.DisplayName;

        if (outcome.IsHeal)
            return $"{user} uses {ability} on {target}: +{outcome.Amount} HP";

        int chance = Mathf.RoundToInt(outcome.HitChance * 100f);

        return outcome.IsHit
            ? $"{user} hits {target} with {ability}: -{outcome.Amount} HP ({chance}%)"
            : $"{user} misses {target} with {ability} ({chance}%)";
    }

    private string FormatTarget(BattleUnit user, AbilityData ability, BattleUnit target)
    {
        string text = $"{target.Name}\n{target.Health}/{target.MaxHealth} HP";

        if (ability.Effect != AbilityEffect.Damage)
            return text;

        int chance = Mathf.RoundToInt(rules.GetHitChance(user, ability, target) * 100f);
        return $"{text}  {chance}%";
    }

    private static string Colored(BattleUnit unit)
    {
        string color = unit.Team == Team.Player ? PlayerColor : EnemyColor;
        return $"<color={color}>{unit.Name}</color>";
    }

    private static void ShowSlots<T>(IReadOnlyList<T> slots, int visibleCount) where T : Component
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].gameObject.SetActive(i < visibleCount);
    }

    private void OnMenuButtonClicked()
    {
        LoadMenuAsync().Forget();
    }

    private async UniTaskVoid LoadMenuAsync()
    {
        menuButton.interactable = false;
        await SceneManager.LoadSceneAsync(menuSceneName);
    }

    private readonly struct ChoiceOption
    {
        public ChoiceOption(string label, bool isAvailable)
        {
            Label = label;
            IsAvailable = isAvailable;
        }

        public string Label { get; }
        public bool IsAvailable { get; }
    }
}
