using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Serializable custom class to store repeat position information
[System.Serializable]
public class RepeatPosition
{
    public int lineIndex;       // line index
    public int letterIndex;     // letter index
    public int repeatCount = 5; // repeat count (default 5)
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject gracetext;  // Added: gratitude/thanks text object
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public GameObject continueButton;
    public GameObject retryButton;
    public GameObject gameCompletePanel;
    public Slider repeatProgressBar;  // Added: progress bar component

    public string[] scriptLines;  // full script lines
    public List<RepeatPosition> requiredRepeatPositions;  // use custom class list

    private int currentLineIndex = 0;  // current line index
    private int score = 0;
    private bool isGameActive = true;
    private List<char> currentLineLetters = new List<char>();  // all letters of the current line (filtered)
    private int currentInputIndex = 0;  // current input position
    private int currentRepeatCount = 0;  // current repeat input count
    private bool isInRepeatMode = false;  // whether in repeat input mode
    private int requiredRepeats = 5;  // number of required repeats

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        score = 0;
        currentLineIndex = 0;
        currentInputIndex = 0;
        currentRepeatCount = 0;
        isInRepeatMode = false;
        currentLineLetters.Clear();
        isGameActive = true;

        UpdateScoreText();
        feedbackText.text = "";
        continueButton.SetActive(false);
        retryButton.SetActive(false);
        gameCompletePanel.SetActive(false);
        repeatProgressBar.gameObject.SetActive(false);  // hide progress bar

        LoadCurrentLine();
    }
    
    private void LoadCurrentLine()
    {
        if (currentLineIndex >= scriptLines.Length)
        {
            GameComplete();
            return;
        }

        currentLineLetters.Clear();
        currentInputIndex = 0;
        currentRepeatCount = 0;
        isInRepeatMode = false;
        repeatProgressBar.gameObject.SetActive(false);  // hide progress bar
        
        foreach (char c in scriptLines[currentLineIndex])
        {
            if (char.IsLetter(c))
            {
                currentLineLetters.Add(char.ToLower(c));
            }
        }

        UpdateDisplayText();
    }
    
    private void UpdateDisplayText()
    {
        string originalText = scriptLines[currentLineIndex];

        string processedText = "";
        int letterIndex = 0;

        foreach (char c in originalText)
        {
            if (char.IsLetter(c))
            {
                // Check whether the current letter requires repeated input
                bool isRepeatLetter = IsRepeatPosition(currentLineIndex, letterIndex);
                
                if (letterIndex < currentInputIndex)
                {
                    processedText += $"<color=green>{c}</color>";
                }
                else
                {
                    // Repeat letters are marked with a special color (orange)
                    string color = isRepeatLetter ? "#FFA500" : "#000000";
                    processedText += $"<color={color}>{c}</color>";
                }
                letterIndex++;
            }
            else
            {
                // Punctuation and spaces remain black
                processedText += $"<color=#000000>{c}</color>";
            }
        }

        currentText.text = processedText;
    }

    // Check whether the current position requires repeated input
    private bool IsRepeatPosition(int lineIndex, int letterIndex)
    {
        if (requiredRepeatPositions == null) return false;
        
        foreach (var pos in requiredRepeatPositions)
        {
            if (pos.lineIndex == lineIndex && pos.letterIndex == letterIndex)
            {
                requiredRepeats = pos.repeatCount;  // get custom repeat count
                return true;
            }
        }
        return false;
    }

    public void ProcessInput(char inputChar)
    {
        if (!isGameActive || currentInputIndex >= currentLineLetters.Count)
            return;
        
        feedbackText.text = "";
        retryButton.SetActive(false);

        // Convert to lowercase for comparison
        char lowerInput = char.ToLower(inputChar);
        char targetChar = currentLineLetters[currentInputIndex];

        // Check whether current position requires repeated input
        bool isRepeatPosition = IsRepeatPosition(currentLineIndex, currentInputIndex);

        if (lowerInput == targetChar)
        {
            if (isRepeatPosition)
            {
                // Handle positions that require repeated input
                isInRepeatMode = true;
                currentRepeatCount++;
                repeatProgressBar.gameObject.SetActive(true);
                repeatProgressBar.value = (float)currentRepeatCount / requiredRepeats;

                Camera.main.GetComponent<CameraShake>().Shake(0.1f, 0.05f);  // light camera shake feedback

                // Display current progress
                feedbackText.text = $"<color=yellow>Progress: {currentRepeatCount}/{requiredRepeats}</color>";

                // Only when the required repeat count is reached does the input count as successful
                if (currentRepeatCount >= requiredRepeats)
                {
                    CompleteCharacterInput();
                    currentRepeatCount = 0;
                    isInRepeatMode = false;
                    repeatProgressBar.gameObject.SetActive(false);
                }
            }
            else
            {
                // Normal position: single successful input
                CompleteCharacterInput();
            }
        }
        else
        {
            // On incorrect input, reset repeat count
            if (isRepeatPosition || isInRepeatMode)
            {
                currentRepeatCount = 0;
                isInRepeatMode = false;
                repeatProgressBar.value = 0;
                repeatProgressBar.gameObject.SetActive(false);
            }
            
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = "<color=red>wrong! try again!</color>";
            retryButton.SetActive(true);
        }
    }

    // Handle completion of a character input
    private void CompleteCharacterInput()
    {
        currentInputIndex++;
        score++;
        UpdateScoreText();
        UpdateDisplayText();

        // Check whether the current line is complete
        if (currentInputIndex >= currentLineLetters.Count)
        {
            OnCurrentLineComplete();
        }
    }
    
    private void OnCurrentLineComplete()
    {
        gracetext.SetActive(true);
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = "<color=green>finish! prepare for the next line</color>";
        continueButton.SetActive(true);
    }
    
    public void ContinueToNextLine()
    {
        if (!isGameActive) return;

        currentLineIndex++;
        continueButton.SetActive(false);
        feedbackText.text = "";
        LoadCurrentLine();
    }
    
    public void ResetCurrentLine()
    {
        currentInputIndex = 0;
        currentRepeatCount = 0;
        isInRepeatMode = false;
        feedbackText.text = "";
        retryButton.SetActive(false);
        repeatProgressBar.gameObject.SetActive(false);
        UpdateDisplayText();
    }
    
    private void GameComplete()
    {
        SceneManager.LoadScene("Level02");
        isGameActive = false;
        gameCompletePanel.SetActive(true);
        feedbackText.text = "";
        continueButton.SetActive(false);
        repeatProgressBar.gameObject.SetActive(false);
    }
    
    public void RestartGame()
    {
        InitializeGame();
    }
    
    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }
}