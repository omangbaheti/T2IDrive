using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextButtonOutput : MonoBehaviour
{
    public TextMeshProUGUI outputText;
    [SerializeField] TextActionType textActionType;
    private Button button; 
    private TextMeshProUGUI text;
    void Start()
    {
        button = GetComponent<Button>();
        text = button.GetComponentInChildren<TextMeshProUGUI>();
        switch (textActionType)
        {
            case TextActionType.TextOutput:
                button.onClick.AddListener(OutputText);
                break;
            case TextActionType.Backspace:
                button.onClick.AddListener(Backspace);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnDestroy()
    {
        switch (textActionType)
        {
            case TextActionType.TextOutput:
                button.onClick.RemoveListener(OutputText);
                break;
            case TextActionType.Backspace:
                button.onClick.RemoveListener(Backspace);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OutputText()
    {
        outputText.text += text.text;
    }

    private void Backspace()
    {
        outputText.text = outputText.text.Substring(0, outputText.text.Length - 1);
    }
}

enum TextActionType
{
    TextOutput = 0,
    Backspace = 1
}
