using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Block Type")]
public class BlockType : ScriptableObject
{
    public string displayName;
    // Position of each of the 6 faces in the block atlas.
    // Top, Bottom, Front, Back, Left, Right
    public Vector2Int[] atlasPositions;
    public bool isTransparent;
}
