using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Ucitel - vyber SpriteRenderer ALEBO Image")]
    public TeacherAI teacher;
    public SpriteRenderer teacherSprite; // ak je ucitel sprite vo svete
    public Image teacherImage;          // ak je ucitel UI Image v Canvase
    public Sprite awaySprite;           // volitelne
    public Sprite lookingSprite;        // volitelne
    public Color awayColor = new Color(0.3f, 0.8f, 0.3f);
    public Color warningColor = new Color(1f, 0.6f, 0.1f);
    public Color lookingColor = new Color(0.9f, 0.1f, 0.1f);

    [Header("Texty (TextMeshPro - Text, nie TMP_InputField)")]
    public TMP_Text taskText;
    public TMP_Text typedText;
    public TMP_Text timerText;
    public TMP_Text gradeText;
    public TMP_Text messageText;

    [Header("Volitelne ukazovatele")]
    public Image suspicionFill;  // Image Type = Filled, Horizontal
    public Image redBorder;     // cerveny celoplosny Image, vypni Raycast Target

    float suspicion;

    void Awake()
    {
        // Zadanie je doslovny text; pripadne specialne znaky sa nesmu tvarit ako TMP tagy.
        if (taskText != null) taskText.richText = false;
        if (typedText != null) typedText.richText = true;
    }

    void Update()
    {
        UpdateTeacherVisual();
        UpdateRedOverlay();
    }

    void UpdateTeacherVisual()
    {
        if (teacher == null) return;

        Color color = awayColor;
        Sprite selectedSprite = awaySprite;

        if (teacher.Current == TeacherAI.State.Warning)
            color = warningColor;
        else if (teacher.Current == TeacherAI.State.Looking)
        {
            color = lookingColor;
            selectedSprite = lookingSprite;
        }

        if (teacherSprite != null)
        {
            teacherSprite.color = color;
            if (selectedSprite != null) teacherSprite.sprite = selectedSprite;
        }

        if (teacherImage != null)
        {
            teacherImage.color = color;
            if (selectedSprite != null) teacherImage.sprite = selectedSprite;
        }
    }

    void UpdateRedOverlay()
    {
        if (redBorder == null) return;

        float targetAlpha = suspicion * 0.5f;

        if (teacher != null)
        {
            if (teacher.Current == TeacherAI.State.Warning)
                targetAlpha = Mathf.Max(targetAlpha,
                    0.1f + Mathf.PingPong(Time.time * 4f, 1f) * 0.25f);
            else if (teacher.Current == TeacherAI.State.Looking)
                targetAlpha = Mathf.Max(targetAlpha, 0.45f);
        }

        Color c = redBorder.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 12f);
        redBorder.color = c;
    }

    public void ShowTask(string sentence, int number, int total)
    {
        if (taskText != null) taskText.text = sentence;
        if (typedText != null) typedText.text = "";
        if (gradeText != null) gradeText.text = "Uloha " + number + "/" + total;
        if (messageText != null) messageText.text = "";
        SetSuspicion(0f);
    }

    public void ShowTyped(string typed, string target)
    {
        if (typedText == null) return;

        var sb = new StringBuilder();
        for (int i = 0; i < typed.Length; i++)
        {
            bool correct = i < target.Length && typed[i] == target[i];
            sb.Append(correct ? "<color=#6CFF6C><noparse>" : "<color=#FF5555><noparse>");
            sb.Append(typed[i]);
            sb.Append("</noparse></color>");
        }
        typedText.text = sb.ToString();
    }

    public void SetTime(float seconds)
    {
        if (timerText == null) return;
        seconds = Mathf.Max(0f, seconds);
        timerText.text = Mathf.CeilToInt(seconds) + " s";
        timerText.color = seconds < 5f ? Color.red : Color.white;
    }

    public void SetSuspicion(float value)
    {
        suspicion = Mathf.Clamp01(value);
        if (suspicionFill != null) suspicionFill.fillAmount = suspicion;
    }

    public void ShowGrade(int grade)
    {
        if (messageText != null) messageText.text = "ZNAMKA: " + grade;
    }

    public void ShowMessage(string text)
    {
        if (messageText != null) messageText.text = text;
    }
}
