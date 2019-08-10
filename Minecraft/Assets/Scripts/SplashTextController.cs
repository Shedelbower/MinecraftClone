using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SplashTextController : MonoBehaviour
{
    public string[] textPool;

    [Range(0f,3f)] public float maxScale = 1.2f;
    [Range(0f, 3f)] public float minScale = 1.0f;
    public float animationInterval = 1.0f;

    private float _timer;
    private Text _text;

    private void Start()
    {
        _text = GetComponent<Text>();
        RandomlySetText();
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        float t = (Mathf.Sin(_timer * 2f*Mathf.PI / animationInterval) + 1f) / 2f;

        float scale = Mathf.Lerp(minScale, maxScale, t);

        this.transform.localScale = new Vector3(scale,scale,1.0f);


        if (Input.GetKeyDown(KeyCode.Space))
        {
            RandomlySetText();
        }
    }

    private void RandomlySetText()
    {
        _text.text = textPool[Random.Range(0, textPool.Length)];
    }

}
