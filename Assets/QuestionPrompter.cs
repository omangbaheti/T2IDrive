using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class QuestionPrompter : MonoBehaviour, IKeyboardInputStream
{
    [SerializeField] private TextMeshPro questionText;
    [SerializeField] private TextMeshPro answerText;
    [SerializeField] private TextAsset questionAsset;

    [Header("Question Distribution")] 
    [SerializeField] private int easyQuestions;
    [SerializeField] private int mediumQuestions;
    [SerializeField] private int hardQuestions;
    
    private List<(string difficulty, string question, string answer)> questionData = new();
    private Dictionary<string, List<(string question, string answer)>> questionsByDifficulty;

    private List<(string difficulty, string question, string answer)> currentSet = new();
    void Start()
    {
        questionData = LoadQuestions();
        questionsByDifficulty = questionData
            .GroupBy(q => q.difficulty)
            .ToDictionary(
                g => g.Key,
                g => g.Select(q => (q.question, q.answer)).ToList()
            );
    }

    private List<(string difficulty, string question, string answer)> LoadQuestions()
    {
        List<(string difficulty, string question, string answer)> result = new List<(string difficulty, string question, string answer)>();
        if (questionAsset == null) return result;

        // Split on newlines, remove empty lines
        string[] lines = questionAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Filter out header if present; accept "difficulty,question,answer" in any case
        IEnumerable<string> dataLines = lines.Where((t, i) => i != 0 || !t.Trim().ToLower().StartsWith("difficulty,question,answer"));

        // Simple CSV split by comma; if questions might contain commas, consider a real CSV parser
        foreach (string line in dataLines)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 3) continue;

            string difficulty = parts[0].Trim();
            string question = parts[1].Trim();
            // Join any remaining columns into the answer in case answer contains commas
            string answer = string.Join(",", parts.Skip(2)).Trim();

            result.Add((difficulty, question, answer));
        }

        return result;
    }

    public void OnCharacterInput(string inputCharacter)
    {
        switch (inputCharacter)
        {
            case "\\u2190":
            {
                Debug.Log("Detected Backspace");
                if (answerText.text.Length > 0)
                {
                    answerText.text = answerText.text.Substring(0, answerText.text.Length - 1);
                }
                break;
            }
            // Right Arrow (→)
            case "\\u2192":
            {
                OnAnswerInput();
                break;
            }
            default:
            {
                answerText.text += inputCharacter;
                break;
            }
        }
    }

    public void LoadNextQuestion()
    {
        if (currentSet.Count == 0)
        {
            LoadNextSet();
        }
    }

    public void LoadNextSet()
    {
        currentSet.Clear();
        for (int i = 0; i < easyQuestions; i++)
        {
            System.Random rng = new();
            List<(string question, string answer)> questions = questionsByDifficulty["Easy"];
            int r = rng.Next(0, questions.Count);
            currentSet.Add(new ("Easy", questions[r].question, questions[i].answer));
            questionsByDifficulty["Easy"].RemoveAt(r); 
        }
        
        for (int i = 0; i < mediumQuestions; i++)
        {
            System.Random rng = new();
            List<(string question, string answer)> questions = questionsByDifficulty["Medium"];
            int r = rng.Next(0, questions.Count);
            currentSet.Add(new ("Medium", questions[r].question, questions[i].answer));
            questionsByDifficulty["Medium"].RemoveAt(r); 
        }
        
        for (int i = 0; i < hardQuestions; i++)
        {
            System.Random rng = new();
            List<(string question, string answer)> questions = questionsByDifficulty["Hard"];
            int r = rng.Next(0, questions.Count);
            currentSet.Add(new ("Hard", questions[r].question, questions[i].answer));
            questionsByDifficulty["Hard"].RemoveAt(r); 
        }
        
    }
    public void OnAnswerInput()
    {
        ValidateAnswer();
    }

    private void ValidateAnswer()
    {
        
    }
}