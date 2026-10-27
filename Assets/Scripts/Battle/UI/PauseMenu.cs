using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private BattleLoop loop;
    private bool isBattleOver;
    private bool isLeaving;

    public event Action RestartRequested;
    public event Action ExitRequested;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        pausePanel.SetActive(false);

        continueButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(OnRestartClicked);
        menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDestroy()
    {
        continueButton.onClick.RemoveListener(Resume);
        restartButton.onClick.RemoveListener(OnRestartClicked);
        menuButton.onClick.RemoveListener(OnMenuClicked);

        if (IsPaused)
            Time.timeScale = 1f;

        if (loop != null)
            loop.BattleEnded -= OnBattleEnded;
    }

    public void Bind(BattleLoop battleLoop)
    {
        loop = battleLoop;
        loop.BattleEnded += OnBattleEnded;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (IsPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        if (isBattleOver || isLeaving)
            return;

        IsPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    private void Resume()
    {
        if (!IsPaused || isLeaving)
            return;

        IsPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    private void OnRestartClicked()
    {
        Leave(RestartRequested);
    }

    private void OnMenuClicked()
    {
        Leave(ExitRequested);
    }

    private void Leave(Action request)
    {
        if (isLeaving)
            return;

        isLeaving = true;
        continueButton.interactable = false;
        restartButton.interactable = false;
        menuButton.interactable = false;
        request?.Invoke();
    }

    private void OnBattleEnded(Team winner)
    {
        isBattleOver = true;
    }
}
