using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarController : MonoBehaviour
{
    public Transform slotParent;
    public SlotItem SeletedItem {
        get { return this.items[_selectedItemIndex]; }
    }

    private SlotItem[] items;
    private int _selectedItemIndex;
    private Slot[] _slots;

    public void SelectItem(int index) {
        DeselectAll();
        _selectedItemIndex = index;
        _slots[_selectedItemIndex].Select();
    }

    public void DeselectAll() {
        foreach (var slot in _slots) {
            slot.Deselect();
        }
    }

    private void Start() {
        Initialize();
    }

    public void Initialize() {
        _slots = slotParent.GetComponentsInChildren<Slot>();
        var slotItems = new List<SlotItem>();
        for (int i = 0; i < _slots.Length; i++) {
            var slot = _slots[i];
            slot.Initialize();
            slotItems.Add(slot.item);
        }
        SelectItem(_selectedItemIndex);
        this.items = slotItems.ToArray();
    }

}
