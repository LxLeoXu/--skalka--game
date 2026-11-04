using UnityEngine;
using UnityEngine.InputSystem;

// Nespracuvame TMP_InputField: znaky citame priamo z klavesnice,
// ale iba ak hrac drzi lave tlacidlo mysi.
public class TypingController : MonoBehaviour
{
    [Header("References")]
    public TeacherAI teacher;
    public GameManager game;
    public UIManager ui;

    string typed = "";
    Keyboard subscribedKeyboard;

    void OnEnable()
    {
        subscribedKeyboard = Keyboard.current;
        if (subscribedKeyboard != null)
            subscribedKeyboard.onTextInput += OnTextInput;
    }

    void OnDisable()
    {
        if (subscribedKeyboard != null)
            subscribedKeyboard.onTextInput -= OnTextInput;
        subscribedKeyboard = null;
    }

    void Update()
    {
        // Backspace nemusi byt doruceny ako textovy znak cez onTextInput.
        if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
            TryBackspace();
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
            && game.State == GameManager.GameState.Playing
            && Mouse.current != null
            && Mouse.current.leftButton.isPressed;
    }

    void OnTextInput(char c)
    {
        // Enter, Tab, Backspace a ine riadiace znaky nepatria do vety.
        if (char.IsControl(c)) return;
        if (!CanType()) return;
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

        // Ak sme sa prave stali podozrivymi a prehrali, dalsi level nespustame.
        if (game.State == GameManager.GameState.Playing && typed == target)
            game.CompleteTask();
    }
}
