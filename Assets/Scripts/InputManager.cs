using UnityEngine;

public class InputManager : MonoBehaviour
{
    void Update()
    {
        foreach (char c in Input.inputString)
        {
            // only process letter
            if (char.IsLetter(c))
            {
                GameManager.Instance.ProcessInput(c);
            }
            // backspace key clears the whole sentence
            //else if (c == '\b')
            //{
            //    GameManager.Instance.ResetCurrentLine();
            //}
        }
    }
}