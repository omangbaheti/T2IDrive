using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InterfaceManager : MonoBehaviour
{
    public string currentSolution = "55";
    
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private TextMeshProUGUI questionField;
    [SerializeField] private Image outputPanelBackground;
    [SerializeField] private Button validateButton;

    private void Start()
    {
        TextButtonOutput[] buttons = GetComponentsInChildren<TextButtonOutput>();
        foreach (TextButtonOutput button in buttons)
        {
            button.outputText = outputText;
        }
        
        validateButton.onClick.AddListener(Validate);
    }

    private void Validate()
    {
        bool isCorrect = outputText.text == currentSolution;
        FlashPanel(isCorrect ? Color.green : Color.red);
        if (isCorrect)
        {
            outputText.text = "";
        }
    }
    private void FlashPanel(Color color)
    {
        Color previousColor = outputPanelBackground.color;
        outputPanelBackground.color = color;
        outputPanelBackground.DOColor(previousColor, 0.2f);
    }

    public void SetNextQuestion(string question)
    {
        questionField.text = question;
    }
}
