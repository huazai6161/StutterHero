using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 新增可序列化的自定义类，用于存储重复位置信息
[System.Serializable]
public class RepeatPosition
{
    public int lineIndex;       // 行索引
    public int letterIndex;     // 字母索引
    public int repeatCount = 5; // 重复次数（默认5次）
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TextMeshProUGUI currentText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public GameObject continueButton;
    public GameObject retryButton;
    public GameObject gameCompletePanel;
    public Slider repeatProgressBar;  // 新增：进度条组件

    public string[] scriptLines;  // 完整剧本
    public List<RepeatPosition> requiredRepeatPositions;  // 改用自定义类列表

    private int currentLineIndex = 0;  // 当前行索引
    private int score = 0;
    private bool isGameActive = true;
    private List<char> currentLineLetters = new List<char>();  // 当前行的所有字母（过滤后）
    private int currentInputIndex = 0;  // 当前输入到的位置
    private int currentRepeatCount = 0;  // 当前重复输入计数
    private bool isInRepeatMode = false;  // 是否处于重复输入模式
    private int requiredRepeats = 5;  // 需要重复的次数

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
        repeatProgressBar.gameObject.SetActive(false);  // 隐藏进度条

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
        repeatProgressBar.gameObject.SetActive(false);  // 隐藏进度条
        
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
                // 检查当前字母是否需要重复输入
                bool isRepeatLetter = IsRepeatPosition(currentLineIndex, letterIndex);
                
                if (letterIndex < currentInputIndex)
                {
                    processedText += $"<color=green>{c}</color>";
                }
                else
                {
                    // 重复字母用特殊颜色标记（橙色）
                    string color = isRepeatLetter ? "#FFA500" : "#000000";
                    processedText += $"<color={color}>{c}</color>";
                }
                letterIndex++;
            }
            else
            {
                // 标点和空格保持浅灰色
                processedText += $"<color=#000000>{c}</color>";
            }
        }

        currentText.text = processedText;
    }

    // 检查当前位置是否需要重复输入
    private bool IsRepeatPosition(int lineIndex, int letterIndex)
    {
        if (requiredRepeatPositions == null) return false;
        
        foreach (var pos in requiredRepeatPositions)
        {
            if (pos.lineIndex == lineIndex && pos.letterIndex == letterIndex)
            {
                requiredRepeats = pos.repeatCount;  // 获取自定义重复次数
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

        // 转换为小写进行比较
        char lowerInput = char.ToLower(inputChar);
        char targetChar = currentLineLetters[currentInputIndex];

        // 检查当前位置是否需要重复输入
        bool isRepeatPosition = IsRepeatPosition(currentLineIndex, currentInputIndex);

        if (lowerInput == targetChar)
        {
            if (isRepeatPosition)
            {
                // 处理需要重复输入的位置
                isInRepeatMode = true;
                currentRepeatCount++;
                repeatProgressBar.gameObject.SetActive(true);
                repeatProgressBar.value = (float)currentRepeatCount / requiredRepeats;

                // 显示当前进度
                feedbackText.text = $"<color=yellow>Progress: {currentRepeatCount}/{requiredRepeats}</color>";

                // 达到要求的重复次数才算输入成功
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
                // 普通位置，一次输入成功
                CompleteCharacterInput();
            }
        }
        else
        {
            // 输入错误时重置重复计数
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

    // 完成一个字符的输入处理
    private void CompleteCharacterInput()
    {
        currentInputIndex++;
        score++;
        UpdateScoreText();
        UpdateDisplayText();

        // 检查是否完成当前行
        if (currentInputIndex >= currentLineLetters.Count)
        {
            OnCurrentLineComplete();
        }
    }
    
    private void OnCurrentLineComplete()
    {
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