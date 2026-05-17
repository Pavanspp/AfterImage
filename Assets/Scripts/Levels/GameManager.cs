using UnityEngine;

// Tracks current level index and drives level progression.
// Lives on a GameManager GameObject in every scene.
// Not DontDestroyOnLoad — freshly initialized each scene.
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
    }

    // Called by LevelExit when the player reaches the goal.
    public void CompleteLevel()
    {
        int nextIndex = currentLevelIndex + 1;

        if (nextIndex >= sceneList.levels.Length)
        {
            levelLoader.LoadScene(sceneList.levels[currentLevelIndex]);
            return;
        }

        levelLoader.LoadScene(sceneList.levels[nextIndex]);
    }

    // Restart the current level.
    public void RestartLevel()
    {
        levelLoader.LoadScene(sceneList.levels[currentLevelIndex]);
    }
}