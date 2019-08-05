using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Block Type", menuName = "Block Type")]
public class BlockType : ScriptableObject
{

    /*------------------------ MEMBER ------------------------*/

    public int index;
    public new string name;
    // Position of each of the 6 faces in the block atlas.
    // Top, Bottom, Front, Back, Left, Right
    public Vector2Int[] atlasPositions;
    public bool isTransparent;
    public bool isBillboard = false;
    public bool affectedByGravity = false;
    public bool isSourceBlock = false;
    public bool mustBeOnGrassBlock = false;
    public AudioClip digClip = null;
    public AudioClip[] stepClips;

    /*------------------------ STATIC ------------------------*/

    // Maps the names of block types to their corresponding object.
    private static Dictionary<string, BlockType> nameToBlockType;
    public static Dictionary<string, BlockType> NameToBlockType
    {
        get
        {
            if (nameToBlockType == null)
            {
                LoadBlockTypes();
            }
            return nameToBlockType;
        }
    }

    private static Dictionary<int, BlockType> indexToBlockType;
    public static Dictionary<int, BlockType> IndexToBlockType
    {
        get
        {
            if (indexToBlockType == null)
            {
                LoadBlockTypes();
            }
            return indexToBlockType;
        }
    }

    private static void LoadBlockTypes()
    {
        // Load all the BlockType assets from the Resources folder.
        BlockType[] typeArray = Resources.LoadAll<BlockType>("Block Types");

        nameToBlockType = new Dictionary<string, BlockType>();
        indexToBlockType = new Dictionary<int, BlockType>();
        foreach (BlockType type in typeArray)
        {
            //if (type.name == "")
            //{
            //    string assetPath = AssetDatabase.GetAssetPath(type.GetInstanceID());
            //    string filename = Path.GetFileNameWithoutExtension(assetPath);
            //    type.name = filename;
            //}

            while (indexToBlockType.ContainsKey(type.index))
            {
                Debug.LogWarning("The block type \"" + type.name + "\" is using the same index as an existing block type. Creating new index...");
                type.index += 1;
            }

            Debug.Log(type.name);
            nameToBlockType.Add(type.name, type);
            indexToBlockType.Add(type.index, type);
        }

    }

    public static BlockType GetBlockType(string name)
    {
        return NameToBlockType[name];
    }

    public static BlockType GetBlockType(int index)
    {
        return IndexToBlockType[index];
    }
}
