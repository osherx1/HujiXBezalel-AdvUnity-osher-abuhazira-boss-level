using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager gm;
    public static event System.Action OnBossBeat;

    public GameObject player;
    public GameObject beatBossUI;

    private void Awake()
    {
        gm = this;
    }

    void Start()
    {
        if (PlayerData.hasCheckPoint)
        {
            player.transform.position = PlayerData.checkPoint;
            FindObjectOfType<FollowCamera>().SnapToTarget();
        }
        else
        {
            player.transform.position = PlayerData.checkPoint;
        }
        BlackScreen.instance.FadeFromBlack();
    }


    public void BeatBoss()
    {
        beatBossUI.SetActive(true);
        OnBossBeat?.Invoke();
        StartCoroutine(BossWinSequence());
    }

    private IEnumerator BossWinSequence()
    {
        yield return new WaitForSecondsRealtime(5f);

        // Set fade duration and connect callback
        BlackScreen.instance.fadeDuration = 1f;
        BlackScreen.instance.done = null; // Avoid duplicate callbacks
        BlackScreen.instance.done += OnFadeDoneAndLoadScene;
        BlackScreen.instance.FadeToBlack();
    }

    private void OnFadeDoneAndLoadScene()
    {
        // Unsubscribe and load scene after fade out
        BlackScreen.instance.done -= OnFadeDoneAndLoadScene;
        SceneManager.sceneLoaded += OnMainSceneLoaded;
        SceneManager.LoadScene("Menu");
    }

    private void OnMainSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe and fade in on new scene
        SceneManager.sceneLoaded -= OnMainSceneLoaded;
        BlackScreen.instance.FadeFromBlack();
    }
}