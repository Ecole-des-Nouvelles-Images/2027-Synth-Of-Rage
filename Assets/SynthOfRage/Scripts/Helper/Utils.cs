using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SynthOfRage.Scripts.Helper
{
    public static class Utils
    {
        public static void QuitGame()
        {
            #if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
            #endif
            
            Application.Quit(0);
        }
        
        public static void RestartLevel()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            SceneManager.LoadScene(currentSceneIndex, LoadSceneMode.Single);
        }

        public static void LoadLevel(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
        }
    }
}
