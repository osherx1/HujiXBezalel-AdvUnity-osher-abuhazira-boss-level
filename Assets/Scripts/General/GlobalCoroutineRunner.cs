using System.Collections;
using UnityEngine;

namespace General
{
    public class GlobalCoroutineRunner : MonoBehaviour
    {
        public static GlobalCoroutineRunner Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RunCoroutine(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }
}