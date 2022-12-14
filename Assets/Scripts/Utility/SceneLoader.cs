using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public Animator transition;

    public float transitionTime = 1f;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(Load(sceneName));
    }

    private IEnumerator Load(string sceneName)
    {
        transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}