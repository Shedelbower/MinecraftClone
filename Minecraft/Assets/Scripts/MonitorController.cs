using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonitorController : MonoBehaviour
{
    public Text fpsText;
    public Text chunkCountText;
    public Text placedCountText;
    public Text destroyedCountText;
    public Text explodedCountText;
    public Text traveledCountText;
    public Text memoryUsedText;
    public Text diamondText;

    public ChunkManager chunkManager;

    private int _blocksPlacedCount = 0;
    private int _blocksDestroyedCount = 0;
    private int _blocksTraveledCount = 0;
    private int _blocksExplodedCount = 0;
    private int _diamondCount = 0;

    private bool _isVisible = true;

    private float _deltaTime = 0.0f;


    private void Update()
    {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

        UpdateAllText();
    }

    public void OnBlockPlaced()
    {
        _blocksPlacedCount++;
    }

    public void OnBlockTraveled()
    {
        _blocksTraveledCount++;
    }

    public void OnBlockDestroyed()
    {
        _blocksDestroyedCount ++;
    }

    public void OnBlocksExploded(int n)
    {
        _blocksExplodedCount += n;
    }

    public void OnDiamondMined()
    {
        _diamondCount++;
    }

    public void UpdateAllText()
    {
        if (_isVisible)
        {
            
            fpsText.text = Mathf.RoundToInt(1.0f / _deltaTime).ToString();

            placedCountText.text = _blocksPlacedCount.ToString();
            destroyedCountText.text = _blocksDestroyedCount.ToString();
            traveledCountText.text = _blocksTraveledCount.ToString();
            explodedCountText.text = _blocksExplodedCount.ToString();

            chunkCountText.text = chunkManager.LoadedChunkCount.ToString();

            memoryUsedText.text = System.String.Format("{0:n0}", System.GC.GetTotalMemory(false) / 1000000.0f) + " MB";
            diamondText.text = _diamondCount.ToString();
        }
    }



}
