using UnityEngine;

public class InputManagerTime : MonoBehaviour
{
    void Update()
    {
        foreach (char c in Input.inputString)
        {
            // only process letter
            if (char.IsLetter(c))
            {
                GameManagerTime.Instance.ProcessInput(c);
            }
            // backspace key clears the whole sentence
            //else if (c == '\b')
            //{
            //    GameManager.Instance.ResetCurrentLine();
            //}
        }
    }
}