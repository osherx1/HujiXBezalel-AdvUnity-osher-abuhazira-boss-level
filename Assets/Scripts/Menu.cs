using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public GameObject main;
    public UnityEngine.EventSystems.EventSystem eventSystem;

    private bool isLoading = false;
    public void Play()
    {
        if (isLoading)
        {
            return;
        }
        isLoading = true;
        eventSystem.SetSelectedGameObject(null);
        StartCoroutine(LoadGame());
    }
    IEnumerator LoadGame()
    {
        BlackScreen.instance.done += ScreenIsBlack;
        BlackScreen.instance.FadeToBlack();
        yield return new WaitUntil(() => isScreenBlack);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainScene");
    }
    private bool isScreenBlack = false;
    private void ScreenIsBlack()
    {
        isScreenBlack = true;
        BlackScreen.instance.done -= ScreenIsBlack;
    }

    private GameObject lastSelected;

    public void OpenMenu()
    {
        main.SetActive(true);
        eventSystem.SetSelectedGameObject(lastSelected);
    }
}