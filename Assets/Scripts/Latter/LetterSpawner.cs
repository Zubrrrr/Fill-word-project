using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LetterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] _letterPrefabs;
    [SerializeField] private GameObject[] _gameLetterPrefabs;
    [SerializeField] private GameObject[] _cellPrefabs;

    [SerializeField] private string[] _levelWords = new string[] { };

    [SerializeField] private Vector2[] _spawnPositions;
    [SerializeField] private Vector2 _bonusWordTargetPosition = new Vector2(-1.67f, -4.24f);

    [SerializeField] private float _gatherSpeed = 0.15f;
    [SerializeField] private float _returnSpeed = 0.15f;
    [SerializeField] private float _delayBetweenStages = 0.1f;
    [SerializeField] private float _forbiddenZoneRadius = 0.2f;
    [SerializeField] private float _updateRate = 0.01f;
    [SerializeField] private float _letterSpacing = 0.45f;

    [SerializeField] private Button _shuffleButton;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private TextMeshProUGUI _notificationText;
    [SerializeField] private Animator _notificationAnim;

    [SerializeField] private UiController _uiController;

    private float _timeSinceLastUpdate = 0f;
    private bool _isShuffling = false;

    private List<GameObject> _spawnedLetters = new List<GameObject>();
    private List<GameObject> _createdLetterObjects = new List<GameObject>();
    private HashSet<string> _guessedWords = new HashSet<string>();
    private HashSet<string> _allGuessedWords = new HashSet<string>();
    private List<int> _requiredLetterIndexes = new List<int>();
    private List<char> _selectedLetters = new List<char>();
    private List<Vector3> _linePositions = new List<Vector3>();
    private Dictionary<string, int> _hintsShown = new Dictionary<string, int>();
    private Vector2 _firstLetterPosition = new Vector2(0.05469149f, -0.8422791f);



    private void Start()
    {
        InitializeLetters();
        InitializeWordCells();
    }

    private void Update()
    {
        _timeSinceLastUpdate += Time.deltaTime;

        HandleInput();
    }

    public void ShowHint()
    {
        LevelConfiguration currentLevel = LevelManager.Instance.GetCurrentLevel();

        if (currentLevel == null) return;

        foreach (string word in currentLevel.words)
        {
            if (!_guessedWords.Contains(word))
            {
                int hintsCount = _hintsShown.ContainsKey(word) ? _hintsShown[word] : 0;

                if (hintsCount < word.Length)
                {
                    ShowLetterHint(word, hintsCount);
                    hintsCount++;
                    _hintsShown[word] = hintsCount;

                    if (hintsCount == word.Length)
                    {
                        _guessedWords.Add(word);
                    }

                    break;
                }
            }
        }
        CheckLevelCompletion();
    }

    public void ShowThreeRandomLettersFromAllWords()
    {
        LevelConfiguration currentLevel = LevelManager.Instance.GetCurrentLevel();
        if (currentLevel == null || currentLevel.words.Length == 0) return;

        List<string> wordsToConsider = currentLevel.words.Where(word => !_guessedWords.Contains(word)).ToList();

        int totalHintsAvailable = wordsToConsider.Sum(word => word.Length - (_hintsShown.ContainsKey(word) ? _hintsShown[word] : 0));

        int hintsToShow = Math.Min(3, totalHintsAvailable);

        while (hintsToShow > 0 && wordsToConsider.Any())
        {
            string selectedWord = wordsToConsider[UnityEngine.Random.Range(0, wordsToConsider.Count)];
            int hintsCount = _hintsShown.ContainsKey(selectedWord) ? _hintsShown[selectedWord] : 0;

            if (hintsCount < selectedWord.Length)
            {
                int letterIndex = hintsCount;
                ShowLetterHint(selectedWord, letterIndex);

                _hintsShown[selectedWord] = hintsCount + 1;

                if (_hintsShown[selectedWord] == selectedWord.Length)
                {
                    _guessedWords.Add(selectedWord);
                    wordsToConsider.Remove(selectedWord);
                }

                hintsToShow--;
            }
            else
            {
                wordsToConsider.Remove(selectedWord);
            }
        }

        CheckLevelCompletion();
    }

    public void ShuffleLetters()
    {
        if (_shuffleButton != null && _shuffleButton.interactable && !_isShuffling)
        {
            _shuffleButton.interactable = false;
            _isShuffling = true;
            StartCoroutine(ShuffleAnimation());
        }
    }

    public void ShowAllGuessedWords()
    {
        Debug.Log("Все угаданные слова:");
        foreach (string word in _allGuessedWords)
        {
            Debug.Log(word);
        }
    }

    private void InitializeLetters()
    {
        LevelConfiguration currentLevel = LevelManager.Instance.GetCurrentLevel();

        if (currentLevel != null)
        {
            _spawnPositions = currentLevel.spawnPositions;
            _levelWords = currentLevel.words;

        }

        _spawnedLetters.Clear();
        CalculateRequiredLetters();

        List<Vector2> availablePositions = new List<Vector2>(_spawnPositions);

        foreach (int letterIndex in _requiredLetterIndexes)
        {
            if (availablePositions.Count > 0)
            {
                int positionIndex = UnityEngine.Random.Range(0, availablePositions.Count);
                Vector2 position = availablePositions[positionIndex];
                availablePositions.RemoveAt(positionIndex);
                SpawnLetter(letterIndex, position);
            }
            else
            {
                Debug.LogError("Недостаточно уникальных позиций для спавна всех букв");
                break;
            }
        }
    }

    private void InitializeWordCells()
    {
        LevelConfiguration currentLevel = LevelManager.Instance.GetCurrentLevel();
        if (currentLevel == null || currentLevel.cellPositions.Length == 0) return;

        int cellPositionIndex = 0;

        foreach (string word in currentLevel.words)
        {
            foreach (char letter in word)
            {
                int prefabIndex = GetLetterIndex(letter) - 1;
                if (prefabIndex >= 0 && prefabIndex < _cellPrefabs.Length)
                {
                    Vector2 cellPosition = currentLevel.cellPositions[cellPositionIndex];
                    Instantiate(_cellPrefabs[prefabIndex], cellPosition, Quaternion.identity);
                    cellPositionIndex++;
                    if (cellPositionIndex >= currentLevel.cellPositions.Length)
                    {
                        Debug.LogWarning("Not enough cell positions defined for all letters.");
                        break; // Выход из цикла, если позиции кончились 
                    }
                }
                else
                {
                    Debug.LogError($"Invalid letter index: {prefabIndex} for letter {letter}");
                }
            }
        }
    }

    private void CalculateRequiredLetters()
    {
        foreach (string word in _levelWords)
        {
            foreach (char letter in word)
            {
                int letterIndex = GetLetterIndex(letter) - 1;
                if (!_requiredLetterIndexes.Contains(letterIndex))
                {
                    _requiredLetterIndexes.Add(letterIndex);
                }
            }
        }
    }

    private void SpawnLetter(int prefabIndex, Vector2 position)
    {
        if (prefabIndex >= 0 && prefabIndex < _letterPrefabs.Length)
        {
            GameObject letter = Instantiate(_letterPrefabs[prefabIndex], position, Quaternion.identity);
            _spawnedLetters.Add(letter);
        }
    }

    private void ShufflePositions(Vector2[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            Vector2 temp = positions[i];
            int randomIndex = UnityEngine.Random.Range(i, positions.Length);
            positions[i] = positions[randomIndex];
            positions[randomIndex] = temp;
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnStartDrawing();
        }

        if (Input.GetMouseButton(0) && _timeSinceLastUpdate >= _updateRate)
        {
            OnContinueDrawing();
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnEndDrawing();
        }
    }

    private Vector3 GetMousePositionInWorld()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        return mousePos;
    }

    private bool IsInForbiddenZone(Vector3 position)
    {
        return _linePositions.Any(pos => Vector3.Distance(pos, position) < _forbiddenZoneRadius);
    }

    private void OnStartDrawing()
    {
        ClearSelection();
    }

    private void OnContinueDrawing()
    {
        Vector3 mousePos = GetMousePositionInWorld();
        if (!IsInForbiddenZone(mousePos))
        {
            UpdateLine(mousePos);
            _timeSinceLastUpdate = 0f;
        }

        TrySelectLetter(mousePos);
    }

    private void UpdateLine(Vector3 newPosition)
    {
        if (_linePositions.Count > 0)
        {
            _lineRenderer.positionCount = _linePositions.Count + 1;
            _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, newPosition);
        }
    }

    private void OnEndDrawing()
    {
        if (_selectedLetters.Count > 0)
        {
            bool wordGuessed = CheckWord();
            if (!wordGuessed)
            {
                AnimateAndDestroyLetterPrefabs("RemoveLetter", 0.3f);
            }
        }
        else
        {
            // Здесь можно обработать случай, когда слово не начало формироваться
            // Например, игнорировать или показать уведомление, что нужно выбрать буквы
        }

        ClearSelection();
    }

    private void AddPointToLine(Vector3 position)
    {
        _linePositions.Add(position);
        _lineRenderer.positionCount = _linePositions.Count;
        _lineRenderer.SetPositions(_linePositions.ToArray());
    }

    private void AddLetter(char letter)
    {
        if (!_selectedLetters.Contains(letter))
        {
            _selectedLetters.Add(letter);

            int prefabIndex = LetterToPrefabIndex(letter);
            if (prefabIndex >= 0 && prefabIndex < _gameLetterPrefabs.Length)
            {
                Vector2 newPosition = CalculateLetterPosition();
                GameObject letterObject = Instantiate(_gameLetterPrefabs[prefabIndex], newPosition, Quaternion.identity);
                _createdLetterObjects.Add(letterObject);

                CenterCreatedLetters();
            }
            else
            {
                Debug.LogError($"Префаб для буквы '{letter}' не найден.");
            }
        }
    }

    private int LetterToPrefabIndex(char letter)
    {
        int index = (int)(char.ToLower(letter)) - 'а' + 1;
        if (letter >= 'ё')
        {
            index--;
        }
        return index - 1;
    }

    private Vector2 CalculateLetterPosition()
    {
        return _firstLetterPosition + new Vector2(_letterSpacing * _createdLetterObjects.Count, 0);
    }

    private void CenterCreatedLetters()
    {
        if (_createdLetterObjects.Count == 0) return;

        float totalLength = _letterSpacing * (_createdLetterObjects.Count - 1);
        float startOffset = -totalLength / 2;

        for (int i = 0; i < _createdLetterObjects.Count; i++)
        {
            GameObject letterObject = _createdLetterObjects[i];
            Vector2 newPosition = new Vector2(startOffset + i * _letterSpacing, _firstLetterPosition.y);
            letterObject.transform.position = newPosition;
        }
    }

    private void TrySelectLetter(Vector3 mousePos)
    {
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        if (hit.collider != null && hit.collider.GetComponent<Letter>() != null)
        {
            Vector3 hitPosition = hit.collider.transform.position;
            hitPosition.z = 0;

            if (!_linePositions.Contains(hitPosition))
            {
                AddPointToLine(hitPosition);
                AddLetter(hit.collider.GetComponent<Letter>().LetterChar);
            }
        }
    }

    private void MoveGuessedWordToCells(string guessedWord)
    {
        LevelConfiguration currentLevel = LevelManager.Instance.GetCurrentLevel();
        if (currentLevel == null) return;

        int wordIndex = Array.IndexOf(currentLevel.words, guessedWord);
        if (wordIndex == -1) return;

        int startPositionIndex = 0;
        for (int i = 0; i < wordIndex; i++)
        {
            startPositionIndex += currentLevel.words[i].Length;
        }

        for (int i = 0; i < guessedWord.Length; i++)
        {
            int prefabIndex = GetLetterIndex(guessedWord[i]);
            if (prefabIndex < 0 || prefabIndex >= _gameLetterPrefabs.Length) continue;

            if (i + startPositionIndex < currentLevel.cellPositions.Length)
            {
                Vector2 targetPosition = currentLevel.cellPositions[i + startPositionIndex];
                GameObject letterObject = _createdLetterObjects[i];
                float delay = i * 0.05f;
                StartCoroutine(MoveLettersSmoothly(letterObject, targetPosition, delay));
            }
        }

        _createdLetterObjects.Clear();
    }

    private bool CheckWord()
    {
        string formedWord = new string(_selectedLetters.ToArray()).ToLower();
        bool wordGuessed = false;

        if (LevelManager.Instance.GetCurrentLevel().bonusWords.Contains(formedWord))
        {
            if (!_allGuessedWords.Contains(formedWord))
            {
                _notificationText.text = $"Вы отгадали бонусное слово '<color=#316D98>{formedWord}</color>'";
                _uiController.ChangeImageColor("#00E0E0");
                _notificationAnim.SetTrigger("ActivateNotification");
                _guessedWords.Add(formedWord);
                _allGuessedWords.Add(formedWord);
                StartCoroutine(MoveBonusWordLetters(_createdLetterObjects, _bonusWordTargetPosition));
            }
            else
            {
                _notificationText.text = $"Бонусное слово '<color=#316D98>{formedWord}</color>' уже было угадано";
                _uiController.ChangeImageColor("#00E0E0");
                _notificationAnim.SetTrigger("ActivateNotification");
                AnimateAndDestroyLetterPrefabs("WordGuessed", 0.6f);
            }
            wordGuessed = true;
        }
        else if (_levelWords.Contains(formedWord))
        {
            if (!_guessedWords.Contains(formedWord))
            {
                _allGuessedWords.Add(formedWord);
                _guessedWords.Add(formedWord);
                MoveGuessedWordToCells(formedWord);
                _createdLetterObjects.Clear();
                wordGuessed = true;
            }
            else
            {
                _notificationText.text = $"Слово '<color=#316D98>{formedWord}</color>' уже угадано";
                _uiController.ChangeImageColor("#71F3AE");
                _notificationAnim.SetTrigger("ActivateNotification");
                wordGuessed = true;
                AnimateAndDestroyLetterPrefabs("WordGuessed", 0.6f);
            }
        }
        else
        {
            _notificationText.text = $"Слово '<color=#316D98>{formedWord}</color>' не загадано";
            _uiController.ChangeImageColor("#FD9090");
            _notificationAnim.SetTrigger("ActivateNotification");
        }

        if (wordGuessed)
        {
            CheckLevelCompletion();
        }

        return wordGuessed;
    }

    private void ClearSelection()
    {
        _linePositions.Clear();
        _lineRenderer.positionCount = 0;
        _selectedLetters.Clear();
    }

    private void CheckLevelCompletion()
    {
        if (_guessedWords.IsSupersetOf(_levelWords))
        {
            Debug.Log("Все основные слова угаданы! Переход к следующему уровню.");
            LevelManager.Instance.GoToNextLevel();
        }
    }

    private int GetLetterIndex(char letter)
    {
        if (letter >= 'а' && letter <= 'е')
        {
            return letter - 'а' + 1;
        }
        else if (letter > 'е' && letter <= 'я')
        {
            return letter - 'а' + 1;
        }
        return -1;
    }

    private void ShowLetterHint(string word, int letterIndex)
    {
        int startPositionIndex = CalculateStartPositionIndexForWord(word);
        int prefabIndex = GetLetterIndex(word[letterIndex]) - 1;
        Vector2 hintPosition = LevelManager.Instance.GetCurrentLevel().cellPositions[startPositionIndex + letterIndex];

        if (prefabIndex >= 0 && prefabIndex < _cellPrefabs.Length)
        {
            GameObject hintCell = Instantiate(_cellPrefabs[prefabIndex], hintPosition, Quaternion.identity);
            Animator hintAnimator = hintCell.GetComponent<Animator>();
            if (hintAnimator != null)
            {
                hintAnimator.SetBool("ShowHint", true);
            }
        }
        else
        {
            Debug.LogError("Invalid prefab index for hint letter.");
        }
    }

    private int CalculateStartPositionIndexForWord(string word)
    {
        int index = 0;
        for (int i = 0; i < _levelWords.Length; i++)
        {
            if (_levelWords[i] == word) break;
            index += _levelWords[i].Length;
        }
        return index;
    }

    private void AnimateAndDestroyLetterPrefabs(string animationBoolName, float destructionDelay)
    {
        foreach (var letterObject in _createdLetterObjects)
        {
            Animator anim = letterObject.GetComponent<Animator>();

            if (anim != null)
            {
                anim.SetBool(animationBoolName, true);
                Destroy(letterObject, destructionDelay);
            }
        }

        _createdLetterObjects.Clear();
    }

    private IEnumerator MoveBonusWordLetters(List<GameObject> letterObjects, Vector3 targetPosition)
    {
        for (int i = 0; i < letterObjects.Count; i++)
        {
            GameObject letterObject = letterObjects[i];

            Animator anim = letterObject.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("MoveBonusWord", true);
            }

            float delay = i * 0.05f;
            StartCoroutine(MoveLettersSmoothly(letterObjects[i], targetPosition, delay, false));
            Destroy(letterObject, 1f);
            yield return new WaitForSeconds(delay);
        }

        _createdLetterObjects.Clear();
    }

    private IEnumerator MoveLettersSmoothly(GameObject letterObject, Vector3 targetPosition, float delay, bool activateAnimation = true)
    {
        yield return new WaitForSeconds(delay);

        if (activateAnimation)
        {
            Animator anim = letterObject.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("MoveLetter", true);
            }
        }

        float duration = 0.3f;
        float elapsedTime = 0;
        Vector3 startPosition = letterObject.transform.position;

        while (elapsedTime < duration)
        {
            letterObject.transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        letterObject.transform.position = targetPosition;
    }

    private IEnumerator ShuffleAnimation()
    {
        Vector3 centerPoint = new Vector3(0, -3.02f, 0);

        foreach (var letter in _spawnedLetters)
        {
            StartCoroutine(MoveLetter(letter, centerPoint, _gatherSpeed));
        }

        yield return new WaitForSeconds(_gatherSpeed + _delayBetweenStages);

        ShufflePositions(_spawnPositions);

        foreach (var letter in _spawnedLetters)
        {
            StartCoroutine(MoveLetter(letter, _spawnPositions[_spawnedLetters.IndexOf(letter)], _returnSpeed));
        }

        yield return new WaitForSeconds(_returnSpeed);

        if (_shuffleButton != null)
        {
            _shuffleButton.interactable = true;
        }
        _isShuffling = false;
    }

    private IEnumerator MoveLetter(GameObject letter, Vector3 newPosition, float duration)
    {
        Vector3 startPosition = letter.transform.position;
        float time = 0;

        while (time < duration)
        {
            letter.transform.position = Vector3.Lerp(startPosition, newPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        letter.transform.position = newPosition;
    }
}