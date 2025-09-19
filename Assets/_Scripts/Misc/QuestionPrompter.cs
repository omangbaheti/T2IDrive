using System;
using System.Collections.Generic;
using System.Linq;
using Experiment;
using TMPro;
using ubco.ovilab.HPUI.Core;
using UnityEngine;

[Serializable]
public class QuestionPrompter : KeyboardInputStream 
{
    public bool IsAnswerCorrect => currentQuestion.answer == answerText.text;
    public (string difficulty, string question, string answer) CurrentQuestion => currentQuestion;
    
    
    [SerializeField] private TextMeshPro questionText;
    [SerializeField] private TextMeshPro answerText;
    [SerializeField] private TextAsset questionAsset;

    [Header("Question Distribution")] 
    [SerializeField] private int easyQuestions;
    [SerializeField] private int mediumQuestions;
    [SerializeField] private int hardQuestions;
    
    private List<(string difficulty, string question, string answer)> questionData = new();
    private Queue<(string difficulty, string question, string answer)> currentSet = new();
    private Dictionary<string, List<(string question, string answer)>> questionsByDifficulty;
    private (string difficulty, string question, string answer) currentQuestion;
    private UIDisplayFlasher _uiDisplayFlasher;
    void Start()
    {
        _uiDisplayFlasher = GetComponent<UIDisplayFlasher>(); 
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

        string[] lines = questionAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        IEnumerable<string> dataLines = lines.Where((t, i) => i != 0 || !t.Trim().ToLower().StartsWith("difficulty,question,answer"));

        foreach (string line in dataLines)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 3) continue;

            string difficulty = parts[0].Trim();
            string question = parts[1].Trim();
            string answer = string.Join(",", parts.Skip(2)).Trim();

            result.Add((difficulty, question, answer));
        }

        return result;
    }

    public override void OnCharacterInput(HPUICanvasEventArgs canvasArgs, InputStreamArgs inputArgs)
    {
        switch (inputArgs.inputAction)
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
                answerText.text += inputArgs.inputAction;
                break;
            }
        }
    }

    private void OnAnswerInput()
    {
        _uiDisplayFlasher.Flash(IsAnswerCorrect ? Color.green : Color.red);
        LoadNextQuestion();
    }
    
    public void LoadNextQuestion()
    {
        if (currentSet.Count == 0)
        {
            LoadNextSet();
        }
        currentQuestion = currentSet.Dequeue();
        questionText.text = currentQuestion.question;
        
    }

    public void LoadNextSet()
    {
        List<(string difficulty, string question, string answer)> newSet = new();
        currentSet.Clear();
        for (int i = 0; i < easyQuestions; i++)
        {
            System.Random rng = new();
            List<(string question, string answer)> questions = questionsByDifficulty["Easy"];
            int r = rng.Next(0, questions.Count);
            newSet.Add(new ("Easy", questions[r].question, questions[i].answer));
            questionsByDifficulty["Easy"].RemoveAt(r); 
        }
        
        for (int i = 0; i < mediumQuestions; i++)
        {
            System.Random rng = new();
            List<(string question, string answer)> questions = questionsByDifficulty["Medium"];
            int r = rng.Next(0, questions.Count);
            newSet.Add(new ("Medium", questions[r].question, questions[i].answer));
            questionsByDifficulty["Medium"].RemoveAt(r); 
        }
        
        for (int i = 0; i < hardQuestions; i++)
        {
            System.Random rng = new();
            List<(string question, string answer)> questions = questionsByDifficulty["Hard"];
            int r = rng.Next(0, questions.Count);
            newSet.Add(new ("Hard", questions[r].question, questions[i].answer));
            questionsByDifficulty["Hard"].RemoveAt(r); 
        }
        Shuffle(newSet);
        currentSet = new(newSet);
    }


    
    public void Shuffle<T>(List<T> array)
    {
        System.Random rng = new(); 
        int n = array.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1); // random index between 0 and n
            (array[k], array[n]) = (array[n], array[k]);
        }
    }

}