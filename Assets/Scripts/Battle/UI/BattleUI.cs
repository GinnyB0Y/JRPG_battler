using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
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
    [SerializeField] private Button restartButton;
    [SerializeField] private Color victoryColor = new Color(0.45f, 1f, 0.5f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.4f, 0.35f);

    private readonly Queue<string> logLines = new Queue<string>();
    private readonly List<UnitStatusView> boundStatusViews = new List<UnitStatusView>();

    private TMP_Text[] choiceLabels;
    private UniTaskCompletionSource<int> pendingChoice;
    private BattleLoop loop;
    private CombatRules rules;
    private UnitSelector selector;

    public event Action ActionChoiceStarted;
    public event Action ActionChoiceEnded;
    public event Action ExitRequested;
    public event Action RestartRequested;

    private void Awake()
    {
        choiceLabels = new TMP_Text[choiceButtons.Length];

        for (int i = 0; i < choiceButtons.Length; i++)
            choiceLabels[i] = choiceButtons[i].GetComponentInChildren<TMP_Text>(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].onClick.AddListener(() => OnChoiceClicked(index));
        }

        menuButton.onClick.AddListener(OnMenuButtonClicked);
        restartButton.onClick.AddListener(OnRestartButtonClicked);

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
        restartButton.onClick.RemoveListener(OnRestartButtonClicked);

        if (loop == null)
            return;

        loop.RoundStarted -= OnRoundStarted;
        loop.TurnStarted -= OnTurnStarted;
        loop.ActionResolved -= OnActionResolved;
        loop.BattleEnded -= OnBattleEnded;
    }

    public void Bind(BattleLoop battleLoop, CombatRules combatRules, Battlefield field, UnitSelector unitSelector)
    {
        loop = battleLoop;
        rules = combatRules;
        selector = unitSelector;

        loop.RoundStarted += OnRoundStarted;
        loop.TurnStarted += OnTurnStarted;
        loop.ActionResolved += OnActionResolved;
        loop.BattleEnded += OnBattleEnded;

        BindStatusViews(field.GetTeam(Team.Player), playerStatusViews);
        BindStatusViews(field.GetTeam(Team.Enemy), enemyStatusViews);
    }

    public async UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        ActionChoiceStarted?.Invoke();

        try
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
        finally
        {
            ActionChoiceEnded?.Invoke();
        }
    }

    private async UniTask<AbilityData> ChooseAbilityAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        IReadOnlyList<AbilityData> abilities = unit.Abilities;
        List<ChoiceOption> options = new List<ChoiceOption>();

        foreach (AbilityData ability in abilities)
            options.Add(new ChoiceOption(ability.DisplayName, rules.CanUse(unit, ability)));

        options.Add(new ChoiceOption("Skip", true));

        int index = await ChooseAsync($"{unit.Name}: выбирает действие", options, cancellationToken);

        if (index < abilities.Count)
            return abilities[index];
        else
            return null;
    }

    private async UniTask<BattleUnit> ChooseTargetAsync(BattleUnit unit, AbilityData ability, CancellationToken cancellationToken)
    {
        ChoiceOption[] options = { new ChoiceOption("Back", true) };

        using (CancellationTokenSource phase = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            (int winner, BattleUnit target, int _) = await UniTask.WhenAny(
                selector.SelectTargetAsync(rules.GetTargets(unit, ability), phase.Token),
                ChooseAsync($"{ability.DisplayName}: выбирает цель", options, phase.Token));

            phase.Cancel();

            if (winner == 0)
                return target;
            else
                return null;
        }
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
            if (pendingChoice == completion)
                pendingChoice = null;

            if (choicePanel != null)
                choicePanel.SetActive(false);
        }
    }

    private void OnChoiceClicked(int index)
    {
        pendingChoice?.TrySetResult(index);
    }

    private void BindStatusViews(List<BattleUnit> units, UnitStatusView[] views)
    {
        units.Sort(CompareSlot);

        int count = Mathf.Min(units.Count, views.Length);

        for (int i = 0; i < count; i++)
        {
            views[i].Bind(units[i]);
            boundStatusViews.Add(views[i]);
        }

        ShowSlots(views, count);
    }

    private static int CompareSlot(BattleUnit a, BattleUnit b)
    {
        return a.Slot.CompareTo(b.Slot);
    }

    private void OnRoundStarted(int round)
    {
        AddLog($"<b>Раунд {round}</b>");
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
            AddLog($"{Colored(outcome.Target)} умэр");

        if (outcome.StunnedTarget)
            AddLog($"{Colored(outcome.Target)} оглушён и пропустит следующий ход");

        RefreshQueue();
    }

    private void OnBattleEnded(Team winner)
    {
        bool isVictory = winner == Team.Player;

        if (isVictory)
        {
            AddLog("<b>Victory</b>");
            resultText.text = "ПОБЕДА";
            resultText.color = victoryColor;
            restartButton.gameObject.SetActive(false);
        }
        else
        {
            AddLog("<b>Defeat</b>");
            resultText.text = "ПОРАЖЕНИЕ";
            resultText.color = defeatColor;
            restartButton.gameObject.SetActive(true);
        }

        HighlightStatus(null);
        ShowSlots(queueSlots, 0);
        resultPanel.SetActive(true);
    }

    private void HighlightStatus(BattleUnit activeUnit)
    {
        foreach (UnitStatusView view in boundStatusViews)
            view.SetHighlighted(view.Unit == activeUnit);
    }

    private void RefreshQueue()
    {
        List<string> entries = new List<string>();

        foreach (BattleUnit unit in loop.GetUpcomingUnits())
            entries.Add(Colored(unit));

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

        if (outcome.IsStunSkip)
            return $"{user} оглушён и пропускает ход";

        if (outcome.IsSkip)
            return $"{user} пропускает ход";

        string target = Colored(outcome.Target);
        string ability = outcome.Ability.DisplayName;

        if (outcome.IsHeal)
            return $"{user} использует {ability} на {target}: +{outcome.Amount} HP";

        int chance = Mathf.RoundToInt(outcome.HitChance * 100f);

        if (outcome.IsHit)
            return $"{user} попадает по {target} с {ability}: -{outcome.Amount} HP ({chance}%)";
        else
            return $"{user} промахивается по {target} с {ability} ({chance}%)";
    }

    private static string Colored(BattleUnit unit)
    {
        string color;

        if (unit.Team == Team.Player)
            color = PlayerColor;
        else
            color = EnemyColor;

        return $"<color={color}>{unit.Name}</color>";
    }

    private static void ShowSlots<T>(IReadOnlyList<T> slots, int visibleCount) where T : Component
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].gameObject.SetActive(i < visibleCount);
    }

    private void OnMenuButtonClicked()
    {
        DisableResultButtons();
        ExitRequested?.Invoke();
    }

    private void OnRestartButtonClicked()
    {
        DisableResultButtons();
        RestartRequested?.Invoke();
    }

    private void DisableResultButtons()
    {
        menuButton.interactable = false;
        restartButton.interactable = false;
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
