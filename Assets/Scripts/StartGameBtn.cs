using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGameBtn : MonoBehaviour
{
    Button startGameBtn;

    private void Start()
    {
        startGameBtn = GetComponent<Button>();
        startGameBtn.onClick.AddListener(StartGame);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Level01");
    }
}
