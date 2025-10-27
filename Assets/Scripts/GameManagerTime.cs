using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class RepeatPositionTwo
{
    public int lineIndex;       // 行索引
    public int letterIndex;     // 字母索引
    public int repeatCount = 5; // 重复次数（默认5次）
}

public class GameManagerTime : MonoBehaviour
{
    public static GameManagerTime Instance;

    public CameraShake cameraShake;

    // UI组件
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public GameObject continueButton;
    public GameObject retryButton;
    public GameObject gameCompletePanel;
    public Slider repeatProgressBar;  // 重复输入进度条
    public Slider timeProgressBar;    // 时间流逝进度条（整句话的进度）
    public Slider typingProgressBar;  // 打字进度条（当前行完成度）

    // 游戏配置
    public string[] scriptLines;  // 完整剧本
    public List<RepeatPosition> requiredRepeatPositions;  // 重复位置配置
    public float targetSecondsPerChar = 1.5f;  // 每个字符的目标输入时间（秒）
    public float speedTolerance = 0.4f;  // 速度容忍度（±40%）

    // Failure configuration (可在 Inspector 调整)
    public bool failOnDeviation = true;                  // 超出致命偏差即失败
    public float failToleranceMultiplier = 2.0f;         // 超出 speedTolerance * multiplier 则视为致命偏差

    // 游戏状态
    private int currentLineIndex = 0;
    private int score = 0;
    private bool isGameActive = true;
    private List<char> currentLineLetters = new List<char>();
    private int currentInputIndex = 0;
    private int currentRepeatCount = 0;
    private bool isInRepeatMode = false;
    private int requiredRepeats = 5;

    // 速度控制相关
    private float currentCharTimer;      // 当前字符的计时（用于每字符速度检测）
    private float currentLineTimer;      // 当前整行的计时（用于整句话时间进度条）
    private float currentLineAllowedTime;// 当前整行允许的总时间 = targetSecondsPerChar * numberOfLetters
    private bool isSpeedValid = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 确保 retryButton 点击会重置关卡（重载场景）
        if (retryButton != null)
        {
            var btn = retryButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(RetryLevel);
            }
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        // 仅在游戏激活且未完成当前行时更新计时器
        if (isGameActive && currentInputIndex < currentLineLetters.Count && !isInRepeatMode)
        {
            // per-character timer (keeps checking each character speed)
            currentCharTimer += Time.deltaTime;
            // line-level timer (total elapsed time for the whole line)
            currentLineTimer += Time.deltaTime;

            // 更新时间进度条为 "整句话进度"（0~1）
            if (currentLineAllowedTime > 0f)
            {
                float timeProgress = Mathf.Clamp01(currentLineTimer / currentLineAllowedTime);
                timeProgressBar.value = timeProgress;
            }
            else
            {
                timeProgressBar.value = 0f;
            }

            // 超过单字符最大容忍时间提示太慢（保留原有每字符提示）
            if (currentCharTimer > targetSecondsPerChar * (1 + speedTolerance) && isSpeedValid)
            {
                feedbackText.text = "<color=red>Too slow! Hurry up!</color>";
                isSpeedValid = false;
            }

            // 如果启用了致命失败判定：整行耗时超过致命上限则失败
            if (failOnDeviation && currentLineAllowedTime > 0f)
            {
                float maxLineFail = currentLineAllowedTime * (1 + speedTolerance * failToleranceMultiplier);
                if (currentLineTimer > maxLineFail)
                {
                    TriggerGameFail("Line time exceeded maximum allowed (too slow)");
                }
            }
        }
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
        currentCharTimer = 0;
        currentLineTimer = 0;
        currentLineAllowedTime = 0;
        isSpeedValid = true;

        // 初始化UI
        UpdateScoreText();
        feedbackText.text = "";
        SetActiveWithButtonState(continueButton, false);
        SetActiveWithButtonState(retryButton, false);
        gameCompletePanel.SetActive(false);
        repeatProgressBar.gameObject.SetActive(false);
        timeProgressBar.gameObject.SetActive(true);  // 显示时间进度条
        typingProgressBar.gameObject.SetActive(true); // 显示打字进度条
        timeProgressBar.value = 0;
        typingProgressBar.value = 0;

        LoadCurrentLine();
    }

    private void LoadCurrentLine()
    {
        if (currentLineIndex >= scriptLines.Length)
        {
            GameComplete();
            return;
        }

        // 重置当前行状态
        currentLineLetters.Clear();
        currentInputIndex = 0;
        currentRepeatCount = 0;
        isInRepeatMode = false;
        currentCharTimer = 0;
        currentLineTimer = 0;
        isSpeedValid = true;

        // 过滤当前行的字母（仅保留字母并转小写）
        foreach (char c in scriptLines[currentLineIndex])
        {
            if (char.IsLetter(c))
            {
                currentLineLetters.Add(char.ToLower(c));
            }
        }

        // 计算整句允许时间（确保不为零）
        currentLineAllowedTime = Mathf.Max(0.1f, targetSecondsPerChar * Mathf.Max(1, currentLineLetters.Count));

        // 重置进度条
        repeatProgressBar.gameObject.SetActive(false);
        timeProgressBar.value = 0;
        typingProgressBar.value = 0;

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
                bool isRepeatLetter = IsRepeatPosition(currentLineIndex, letterIndex);

                if (letterIndex < currentInputIndex)
                {
                    processedText += $"<color=green>{c}</color>";
                }
                else
                {
                    string color = isRepeatLetter ? "#FFA500" : "#000000";
                    processedText += $"<color={color}>{c}</color>";
                }
                letterIndex++;
            }
            else
            {
                processedText += $"<color=#666666>{c}</color>"; // 标点符号灰色
            }
        }

        currentText.text = processedText;

        // 更新打字进度条（当前完成比例）
        if (currentLineLetters.Count > 0)
        {
            float typingProgress = (float)currentInputIndex / currentLineLetters.Count;
            typingProgressBar.value = typingProgress;
        }
    }

    private bool IsRepeatPosition(int lineIndex, int letterIndex)
    {
        if (requiredRepeatPositions == null) return false;

        foreach (var pos in requiredRepeatPositions)
        {
            if (pos.lineIndex == lineIndex && pos.letterIndex == letterIndex)
            {
                requiredRepeats = pos.repeatCount;
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
        SetActiveWithButtonState(retryButton, false);

        char lowerInput = char.ToLower(inputChar);
        char targetChar = currentLineLetters[currentInputIndex];
        bool isRepeatPosition = IsRepeatPosition(currentLineIndex, currentInputIndex);

        if (lowerInput == targetChar)
        {
            if (isRepeatPosition)
            {
                // 处理重复输入模式
                isInRepeatMode = true;
                currentRepeatCount++;
                repeatProgressBar.gameObject.SetActive(true);
                repeatProgressBar.value = (float)currentRepeatCount / requiredRepeats;
                feedbackText.text = $"<color=yellow>Repeat: {currentRepeatCount}/{requiredRepeats}</color>";

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
                // 普通字符输入
                CompleteCharacterInput();
            }
        }
        else
        {
            // 输入错误处理
            if (isRepeatPosition || isInRepeatMode)
            {
                currentRepeatCount = 0;
                isInRepeatMode = false;
                repeatProgressBar.value = 0;
                repeatProgressBar.gameObject.SetActive(false);
            }

            // 重置每字符计时器，但保留整句计时（整句进度不在错误时重置）
            currentCharTimer = 0;
            isSpeedValid = true;

            feedbackText.text = "<color=red>Wrong! Try again!</color>";
            SetActiveWithButtonState(retryButton, true);
        }
    }

    private void CompleteCharacterInput()
    {
        // 致命偏差判定（如果启用）：如果当前字符时间超出致命范围则直接失败
        if (failOnDeviation)
        {
            float minFail = targetSecondsPerChar * (1 - speedTolerance * failToleranceMultiplier);
            float maxFail = targetSecondsPerChar * (1 + speedTolerance * failToleranceMultiplier);
            if (currentCharTimer < minFail)
            {
                cameraShake.Shake(0.2f, 0.1f); // 触发相机震动效果
                TriggerGameFail($"Too fast on a character ({currentCharTimer:F2}s) — exceeded fatal deviation");
                return;
            }
            if (currentCharTimer > maxFail)
            {
                cameraShake.Shake(0.2f, 0.1f); // 触发相机震动效果
                TriggerGameFail($"Too slow on a character ({currentCharTimer:F2}s) — exceeded fatal deviation");
                return;
            }
        }

        // 检查输入速度是否符合要求（基于每字符计时）
        CheckInputSpeed();

        currentInputIndex++;
        score = Mathf.Max(0, score); // 确保分数不为负
        UpdateScoreText();
        UpdateDisplayText();

        // 重置当前字符计时器（整句计时不重置）
        currentCharTimer = 0;
        isSpeedValid = true;

        // 检查是否完成当前行
        if (currentInputIndex >= currentLineLetters.Count)
        {
            OnCurrentLineComplete();
        }
    }

    private void CheckInputSpeed()
    {
        float minTime = targetSecondsPerChar * (1 - speedTolerance); // 最小允许时间（太快）
        float maxTime = targetSecondsPerChar * (1 + speedTolerance); // 最大允许时间（太慢）

        if (currentCharTimer < minTime)
        {
            feedbackText.text = "<color=orange>Too fast! Slow down</color>";
            score -= 1; // 太快扣分
        }
        else if (currentCharTimer > maxTime)
        {
            feedbackText.text = "<color=orange>Too slow! Speed up</color>";
            score -= 1; // 太慢扣分
        }
        else
        {
            feedbackText.text = "<color=green>Good speed!</color>";
            score += 2; // 速度适中额外加分
        }
    }

    private void OnCurrentLineComplete()
    {
        feedbackText.text = "<color=green>Line complete! Ready for next?</color>";
        SetActiveWithButtonState(continueButton, true);
        timeProgressBar.gameObject.SetActive(false); // 完成行时隐藏时间进度条
    }

    public void ContinueToNextLine()
    {
        if (!isGameActive) return;

        currentLineIndex++;
        SetActiveWithButtonState(continueButton, false);
        feedbackText.text = "";
        timeProgressBar.gameObject.SetActive(true); // 新行显示时间进度条
        LoadCurrentLine();
    }

    public void ResetCurrentLine()
    {
        currentInputIndex = 0;
        currentRepeatCount = 0;
        isInRepeatMode = false;
        currentCharTimer = 0;
        currentLineTimer = 0; // 重置整句计时（因为用户选择重置）
        isSpeedValid = true;
        feedbackText.text = "";
        SetActiveWithButtonState(retryButton, false);
        repeatProgressBar.gameObject.SetActive(false);
        timeProgressBar.value = 0;
        UpdateDisplayText();
    }

    private void GameComplete()
    {
        isGameActive = false;
        gameCompletePanel.SetActive(true);
        feedbackText.text = "";
        SetActiveWithButtonState(continueButton, false);
        timeProgressBar.gameObject.SetActive(false);
        typingProgressBar.gameObject.SetActive(false);
        repeatProgressBar.gameObject.SetActive(false);
    }

    public void RestartGame()
    {
        gameCompletePanel.SetActive(false);
        InitializeGame();
    }

    /// <summary>
    /// 重新加载当前场景，作为速度失败后的“重置关卡”实现。
    /// retryButton 的点击事件在 Awake 中已自动绑定到此方法。
    /// </summary>
    public void RetryLevel()
    {
        // 可在这里添加短暂的 UI 动画或声音
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

    // 将按钮 GameObject 的 SetActive 与 Button.interactable 一起处理，避免激活不可交互的按钮导致不可点问题
    private void SetActiveWithButtonState(GameObject go, bool active)
    {
        if (go == null) return;
        go.SetActive(active);
        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = active;
        }
    }

    // 触发游戏失败（速度偏离过大）
    private void TriggerGameFail(string reason)
    {
        if (!isGameActive) return;

        isGameActive = false;
        feedbackText.text = $"<color=red>Game Over: {reason}</color>";
        SetActiveWithButtonState(retryButton, true);
        SetActiveWithButtonState(continueButton, false);
        timeProgressBar.gameObject.SetActive(false);
        typingProgressBar.gameObject.SetActive(false);
        repeatProgressBar.gameObject.SetActive(false);
        Debug.LogWarning("[GameManagerTime] TriggerGameFail: " + reason);
    }
}