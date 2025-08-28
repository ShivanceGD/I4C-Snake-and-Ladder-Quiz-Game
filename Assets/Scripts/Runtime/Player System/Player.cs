using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player : MonoBehaviour
{
    [Header("Player meta")]
    public string PlayerName = "Player";
    public bool IsCpu = false;
    public Color Color = Color.white;

    [HideInInspector] public List<QuizQuestionData> QuestionsList = new();

    public PlayerMovement Movement { get; private set; }

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
    }

    public ulong OwnerClientId
    {
        get
        {
            var no = GetComponent<NetworkObject>();
            return no != null ? no.OwnerClientId : 0ul;
        }
    }

    /*public QuizQuestionSummary[] QuestionsListForSummary()
    {
        var arr = new List<QuizQuestionSummary>();
        foreach (var q in QuestionsList)
        {
            arr.Add(new QuizQuestionSummary { Question = q.question, CorrectAnswer = q.options[q.correctAnswerIndex] });
        }
        return arr.ToArray();
    }*/

    /// <summary>
    /// Apply color to common renderers. Safe: checks for SpriteRenderer, Renderer, Image.
    /// Use this after spawning to ensure visuals match Player.Color.
    /// </summary>
    public void ApplyColor(Color c)
    {
        Color = c;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = c;
            return;
        }

        var r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            if (r.material != null) r.material.color = c;
            return;
        }

#if UNITY_UI
        var img = GetComponentInChildren<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.color = c;
            return;
        }
#endif
    }
}