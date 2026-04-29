using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AdditiveSceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName = "Clock Scene Additive";
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private Vector3 loadedSceneOffset = new(4.5f, 0f, 0f);

    void Start()
    {
        if (loadOnStart)
        {
            Load();
        }
    }

    public void Load()
    {
        if (string.IsNullOrWhiteSpace(sceneName) || IsLoaded(sceneName))
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    private static bool IsLoaded(string scene)
    {
        for (var index = 0; index < SceneManager.sceneCount; index++)
        {
            if (SceneManager.GetSceneAt(index).name == scene)
            {
                return true;
            }
        }

        return false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Additive || scene.name != sceneName)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>())
            {
                camera.enabled = false;
            }

            foreach (var audioListener in root.GetComponentsInChildren<AudioListener>())
            {
                audioListener.enabled = false;
            }

            root.transform.position += loadedSceneOffset;
        }
    }
}
