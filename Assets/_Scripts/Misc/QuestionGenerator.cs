using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EditorAttributes;
using UnityEngine;

public class QuestionGenerator : MonoBehaviour
{
    [SerializeField] private int easyQuestions = 3;
    [SerializeField] private int mediumQuestions = 2;
    [SerializeField] private int hardQuestions = 1;
    [SerializeField] private int blocks = 10;
    
    [Header("Ranges (inclusive)")]
    [SerializeField] private Vector2Int easyRange = new(0, 20);
    [SerializeField] private Vector2Int mediumRange = new(10, 100);
    [SerializeField] private Vector2Int hardRange = new(-100, 200);
    
    [Header("Constraints")]
    [SerializeField] private bool easyNonNegativeAnswer = false;
    [SerializeField] private bool mediumNonNegativeAnswer = false;
    [SerializeField] private bool hardNonNegativeAnswer = false;
    
    [Header("Output")]
    [SerializeField] private string fileName = "arithmetic_questions.csv";
    [Button]
    public void GenerateQuestions()
    {
        List<string> rows = new();
        rows.Add("difficulty,question,answer");
        System.Random rng = new();
        for (int i = 0; i < blocks; i++)
        {
            
        rows.AddRange(GenerateSet("Easy", easyQuestions, easyRange, easyNonNegativeAnswer, rng));
        rows.AddRange(GenerateSet("Medium", mediumQuestions, mediumRange, mediumNonNegativeAnswer, rng));
        rows.AddRange(GenerateSet("Hard", hardQuestions, hardRange, hardNonNegativeAnswer, rng ));
        }
        string csv = string.Join("\r\n", rows) + "\r\n";
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(path, csv, Encoding.UTF8);
        Debug.Log($"Wrote CSV with {easyQuestions + mediumQuestions + hardQuestions} questions to: {path}");
    }

    private IEnumerable<string> GenerateSet(string label, int count, Vector2Int range, bool nonNegativeAnswer,
        System.Random rng, bool allowNegativeOperands = false)
    {
        var set = new HashSet<string>();
        var output = new List<string>();

        int min = range.x;
        int max = range.y;

        // Safety to prevent infinite loops if constraints are too tight
        int guard = 0;
        int guardLimit = Math.Max(10000, count * 200);

        while (output.Count < count && guard++ < guardLimit)
        {
            int a = NextInt(rng, min, max);
            int b = NextInt(rng, min, max);

            // For easy/medium, keep operands non-negative by default (based on range)
            if (!allowNegativeOperands)
            {
                a = Mathf.Max(a, 0);
                b = Mathf.Max(b, 0);
            }

            char op = rng.Next(2) == 0 ? '+' : '-';
            long answer = op == '+' ? (long)a + b : (long)a - b;

            if (nonNegativeAnswer && answer < 0)
                continue;

            string equation = $"{a} {op} {b}";
            if (!set.Add(equation))
                continue;

            output.Add($"{label},{equation},{answer}");
        }
        if (output.Count < count)
        {
            Debug.LogWarning($"Only generated {output.Count}/{count} for {label}. Consider widening ranges or relaxing constraints.");
        }

        return output;
    }

    private int NextInt(System.Random rng, int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
            (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
        // Handle full int range correctly
        long range = (long)maxInclusive - minInclusive + 1;
        long sample = (long)(rng.NextDouble() * range);
        return (int)(minInclusive + sample);
    }
}