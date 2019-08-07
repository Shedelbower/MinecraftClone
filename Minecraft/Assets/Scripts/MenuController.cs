using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject loadingScreen;
    private string sceneToLoad;
    private bool isTransitioning = false;
    private float transitionTimer = 0.0f;
    private static readonly float TRANSITION_DURATION = 0.5f;
    

    public void TransitionToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void BeginTransitionToScene(string sceneName)
    {
        //Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        loadingScreen.SetActive(true);
        sceneToLoad = sceneName;
        isTransitioning = true;
    }

    private void Update()
    {
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            if (transitionTimer >= TRANSITION_DURATION)
            {
                TransitionToScene(sceneToLoad);
            }

        }
    }
}
