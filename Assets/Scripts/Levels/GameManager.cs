using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("The SceneList ScriptableObject asset.")]
    public SceneList sceneList;

    [Tooltip("The LevelLoader in this scene.")]
    public LevelLoader levelLoader;

    [Header("Current Level")]
    [Tooltip("Zero-based index of this scene in the SceneList.")]
    public int currentLevelIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void CompleteLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        
        if (nextIndex >= sceneList.levels.Length)
        {
            levelLoader.LoadScene("LevelSelect");
            return;
        }

        levelLoader.LoadScene(sceneList.levels[nextIndex]);
    }

    public void RestartLevel()
    {
        levelLoader.LoadScene(sceneList.levels[currentLevelIndex]);
    }
}