using UnityEngine;
using UnityEngine.InputSystem;

// Pisanie funguje stale. Lave tlacidlo mysi iba prepina, co je vidiet:
// drzis = zadanie, pustis = tvoj napisany text.
public class TypingController : MonoBehaviour
{
    [Header("References")]
    public TeacherAI teacher;
    public GameManager game;
    public UIManager ui;

    string typed = "";
    Keyboard subscribedKeyboard;

    void Awake()
    {
        var missing = new System.Collections.Generic.List<string>();
        if (teacher == null) missing.Add("Teacher");
        if (game == null) missing.Add("Game");
        if (ui == null) missing.Add("Ui");

        if (missing.Count > 0)
            Debug.LogError("TypingController na '" + gameObject.name + "': nepriradene: "
                + string.Join(", ", missing), this);
    }

    void OnEnable() => Subscribe();

    void OnDisable() => Unsubscribe();

    void Subscribe()
    {
        if (Keyboard.current == null || subscribedKeyboard == Keyboard.current) return;
        Unsubscribe();
        subscribedKeyboard = Keyboard.current;
        subscribedKeyboard.onTextInput += OnTextInput;
    }

    void Unsubscribe()
    {
        if (subscribedKeyboard != null)
            subscribedKeyboard.onTextInput -= OnTextInput;
        subscribedKeyboard = null;
    }

    void Update()
    {
        Subscribe();

        if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
            TryBackspace();

        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        if (ui == null || game == null) return;

        if (game.State == GameManager.GameState.Playing)
        {
            bool holding = Mouse.current != null && Mouse.current.leftButton.isPressed;
            ui.SetPromptVisible(holding);
            game.LogPromptVisibility(holding);
        }
        else
        {
            // Mimo hry (menu, vysledky) nechaj oba texty zapnute.
            if (ui.taskText != null) ui.taskText.enabled = true;
            if (ui.typedText != null) ui.typedText.enabled = true;
        }
    }

    public void ResetTyping()
    {
        typed = "";
        if (ui != null && game != null)
            ui.ShowTyped(typed, game.CurrentSentence);
    }

    bool CanType()
    {
        return game != null && teacher != null && ui != null
            && game.State == GameManager.GameState.Playing;
    }

    void OnTextInput(char c)
    {
        if (char.IsControl(c)) return;
        if (!CanType()) return;
        game.LogInput(false, !teacher.IsLooking && typed.Length < game.CurrentSentence.Length
            && game.CurrentSentence[typed.Length] == c);
        if (LoseIfTeacherIsLooking()) return;

        string target = game.CurrentSentence;
        bool wasCorrectPrefix = target.StartsWith(typed, System.StringComparison.Ordinal);

        typed += c;

        bool isCorrectPrefix = target.StartsWith(typed, System.StringComparison.Ordinal);
        if (wasCorrectPrefix && !isCorrectPrefix)
            game.RegisterTypo();

        AfterKey(target);
    }

    void TryBackspace()
    {
        if (!CanType() || typed.Length == 0) return;
        game.LogInput(true, !teacher.IsLooking);
        if (LoseIfTeacherIsLooking()) return;

        typed = typed.Substring(0, typed.Length - 1);
        AfterKey(game.CurrentSentence);
    }

    bool LoseIfTeacherIsLooking()
    {
        if (!teacher.IsLooking) return false;
        game.Lose("Pisal si, ked sa ucitel pozeral!");
        return true;
    }

    void AfterKey(string target)
    {
        if (teacher.IsWarning)
            game.AddSuspicion(game.suspicionPerKeyInWarning);

        ui.ShowTyped(typed, target);

        if (game.State == GameManager.GameState.Playing && typed == target)
            game.CompleteTask();
    }
}