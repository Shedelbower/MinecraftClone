using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SlotItem", order = 1)]
public class SlotItem : ScriptableObject
{
    public enum SlotItemType {
        EnderPearl,
        TNT,
        EndermanHead,
        BoneMeal,
        Stone,
        Dirt,
        Gravel,
        Sand,
        Bedrock,
        CopyBlock
    }
    public Sprite Image;
    public SlotItemType Type;

}
