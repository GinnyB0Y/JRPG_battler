using System;
using System.Collections.Generic;
using System.Linq;
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
    private const int ReselectActorChoice = -1;

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
    private UnitSelector selector;

    public event Action ActionChoiceStarted;
    public event Action ActionChoiceEnded;
    public event Action ExitRequested;

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
        loop.ActorChoiceStarted -= OnActorChoiceStarted;
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
        loop.ActorChoiceStarted += OnActorChoiceStarted;
        loop.TurnStarted += OnTurnStarted;
        loop.ActionResolved += OnActionResolved;
        loop.BattleEnded += OnBattleEnded;

        BindStatusViews(field.GetTeam(Team.Player), playerStatusViews);
        BindStatusViews(field.GetTeam(Team.Enemy), enemyStatusViews);
    }

    public async UniTask<BattleUnit> ChooseActorAsync(IReadOnlyList<BattleUnit> candidates, CancellationToken cancellationToken)
    {
        choiceTitleText.text = "Выберите персонажа для хода";
        ShowSlots(choiceButtons, 0);
        choicePanel.SetActive(true);

        try
        {
            return await selector.SelectActorAsync(candidates, cancellationToken);
        }
        finally
        {
            if (choicePanel != null)
                choicePanel.SetActive(false);
        }
    }

    public async UniTask<BattleAction> ChooseActionAsync(BattleUnit unit, CancellationToken cancellationToken)
    {
        ActionChoiceStarted?.Invoke();

        try
        {
            List<AbilityData> abilities = unit.Abilities.ToList();

            while (true)
            {
                int choice = await ChooseAbilityAsync(unit, abilities, cancellationToken);

                if (choice == ReselectActorChoice)
                    return null;

                if (choice >= abilities.Count)
                    return BattleAction.Skip(unit);

                AbilityData ability = abilities[choice];
                (bool reselectActor, BattleUnit target) = await ChooseTargetAsync(unit, ability, cancellationToken);

                if (reselectActor)
                    return null;

                if (target != null)
                    return BattleAction.Use(unit, ability, target);
            }
        }
        finally
        {
            ActionChoiceEnded?.Invoke();
        }
    }

    private async UniTask<int> ChooseAbilityAsync(BattleUnit unit, List<AbilityData> abilities, CancellationToken cancellationToken)
    {
        List<ChoiceOption> options = abilities
            .Select(ability => new ChoiceOption(ability.DisplayName, rules.CanUse(unit, ability)))
            .ToList();
        options.Add(new ChoiceOption("Skip", true));

        using (CancellationTokenSource phase = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            (int winner, int index, BattleUnit _) = await UniTask.WhenAny(
                ChooseAsync($"{unit.Name}: выбирает действие", options, phase.Token),
                selector.WaitForClickAsync(unit, phase.Token));

            phase.Cancel();

            if (winner == 0)
                return index;
            else
                return ReselectActorChoice;
        }
    }

    private async UniTask<(bool reselectActor, BattleUnit target)> ChooseTargetAsync(BattleUnit unit, AbilityData ability, CancellationToken cancellationToken)
    {
        ChoiceOption[] options = { new ChoiceOption("Back", true) };
        List<BattleUnit> targets = rules.GetTargets(unit, ability).ToList();

        using (CancellationTokenSource phase = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            UniTask<BattleUnit> actorClick;

            if (targets.Contains(unit))
                actorClick = UniTask.Never<BattleUnit>(phase.Token);
            else
                actorClick = selector.WaitForClickAsync(unit, phase.Token);

            (int winner, BattleUnit target, int _, BattleUnit _) = await UniTask.WhenAny(
                selector.SelectTargetAsync(targets, phase.Token),
                ChooseAsync($"{ability.DisplayName}: выбирает цель", options, phase.Token),
                actorClick);

            phase.Cancel();

            if (winner == 0)
                return (false, target);
            else if (winner == 2)
                return (true, null);
            else
                return (false, null);
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
        AddLog($"<b>Раунд {round}</b>");
        RefreshQueue();
    }

    private void OnActorChoiceStarted(Team team)
    {
        RefreshQueue();
        HighlightStatus(null);
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
        }
        else
        {
            AddLog("<b>Defeat</b>");
            resultText.text = "ПОРАЖЕНИЕ";
            resultText.color = defeatColor;
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
        menuButton.interactable = false;
        ExitRequested?.Invoke();
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
