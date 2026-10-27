using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class CreatureDropdown : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    public event Action<CreatureDropdown> RoleChanged;

    public UnitRole Role
    {
        get => (UnitRole)Dropdown.value;
        set => Dropdown.SetValueWithoutNotify((int)value);
    }

    private TMP_Dropdown Dropdown
    {
        get
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();

            return dropdown;
        }
    }

    private void Awake()
    {
        Dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        Dropdown.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(int _)
    {
        RoleChanged?.Invoke(this);
    }
}
