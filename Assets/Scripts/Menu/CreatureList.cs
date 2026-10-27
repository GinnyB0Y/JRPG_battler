using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreatureList : MonoBehaviour
{
    [SerializeField] private Team team;
    [SerializeField] private CreatureDropdown entryPrefab;

    [Header("Controls")]
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private TMP_Text countText;

    private readonly List<CreatureDropdown> entries = new List<CreatureDropdown>();

    private void Awake()
    {
        foreach (CreatureDropdown placeholder in GetComponentsInChildren<CreatureDropdown>(true))
            Destroy(placeholder.gameObject);

        foreach (UnitRole role in BattleSettings.GetRoles(team))
            CreateEntry(role);

        while (entries.Count < BattleSettings.GetMinCount(team))
            CreateEntry(GetRandomRole());

        if (addButton != null)
            addButton.onClick.AddListener(Add);

        if (removeButton != null)
            removeButton.onClick.AddListener(RemoveLast);

        Save();
    }

    private void OnDestroy()
    {
        if (addButton != null)
            addButton.onClick.RemoveListener(Add);

        if (removeButton != null)
            removeButton.onClick.RemoveListener(RemoveLast);

        foreach (CreatureDropdown entry in entries)
            if (entry != null)
                entry.RoleChanged -= OnRoleChanged;
    }

    public void Add()
    {
        if (entries.Count >= BattleSettings.GetMaxCount(team))
            return;

        CreateEntry(GetRandomRole());
        Save();
    }

    public void RemoveLast()
    {
        if (entries.Count <= BattleSettings.GetMinCount(team))
            return;

        CreatureDropdown entry = entries[entries.Count - 1];
        entries.RemoveAt(entries.Count - 1);
        entry.RoleChanged -= OnRoleChanged;
        Destroy(entry.gameObject);
        Save();
    }

    private void CreateEntry(UnitRole role)
    {
        CreatureDropdown entry = Instantiate(entryPrefab, transform);
        entry.Role = role;
        entry.RoleChanged += OnRoleChanged;
        entries.Add(entry);
    }

    private void OnRoleChanged(CreatureDropdown _)
    {
        Save();
    }

    private void Save()
    {
        List<UnitRole> roles = new List<UnitRole>();

        foreach (CreatureDropdown entry in entries)
            roles.Add(entry.Role);

        BattleSettings.SetRoles(team, roles);
        Refresh();
    }

    private void Refresh()
    {
        if (countText != null)
            countText.text = entries.Count.ToString();

        if (addButton != null)
            addButton.interactable = entries.Count < BattleSettings.GetMaxCount(team);

        if (removeButton != null)
            removeButton.interactable = entries.Count > BattleSettings.GetMinCount(team);
    }

    private static UnitRole GetRandomRole()
    {
        UnitRole[] roles = (UnitRole[])Enum.GetValues(typeof(UnitRole));
        return roles[UnityEngine.Random.Range(0, roles.Length)];
    }
}
