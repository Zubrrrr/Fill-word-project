using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelConfiguration[] _levels;
    [SerializeField] private int _currentLevelIndex = 0;

    public static LevelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public LevelConfiguration GetCurrentLevel()
    {
        if (_currentLevelIndex >= 0 && _currentLevelIndex < _levels.Length)
        {
            return _levels[_currentLevelIndex];
        }
        return null;
    }

    public void GoToNextLevel()
    {
       // _currentLevelIndex++;

       // Scene currentScene = SceneManager.GetActiveScene();
       // SceneManager.LoadScene(currentScene.name);
        // Загрузка нового уровня или обновление текущего сценария игры
    }
}
