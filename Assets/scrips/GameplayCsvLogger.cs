using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// One event CSV per difficulty run. Input statistics exclude text typed outside gameplay.
public sealed class GameplayCsvLogger : IDisposable
{
    readonly GameManager game;
    readonly string sessionId = Guid.NewGuid().ToString("N");
    readonly double started = Time.realtimeSinceStartupAsDouble;
    readonly string difficulty;
    StreamWriter writer;
    double taskStarted;
    double? taskEnded;
    double promptStarted;
    double promptSeconds;
    bool promptVisible;
    int attempt;
    int previousTask = -1;
    int inputs;
    int correctInputs;
    int backspaces;

    public GameplayCsvLogger(GameManager game, string difficulty)
    {
        this.game = game;
        this.difficulty = difficulty;
        try
        {
            string folder = Path.Combine(Application.dataPath, "GameplayLogs");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "gameplay_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
                + "_" + sessionId + ".csv");
            writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(true));
            writer.AutoFlush = true;
            writer.WriteLine("schema_version,session_id,utc_timestamp,session_seconds,difficulty,task_number,attempt,event,game_state,teacher_state,time_remaining_seconds,suspicion,gameplay_mistakes,input_attempts,correct_input_attempts,backspaces,accuracy_percent,task_seconds,prompt_visible,prompt_seconds,grade,detail");
            Debug.Log("Gameplay CSV saved to " + path);
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            DisableLogging(exception);
        }
    }

    public void StartTask()
    {
        attempt = game.TaskNumber == previousTask ? attempt + 1 : 1;
        previousTask = game.TaskNumber;
        inputs = 0;
        correctInputs = 0;
        backspaces = 0;
        promptSeconds = 0;
        promptVisible = false;
        taskEnded = null;
        taskStarted = Time.realtimeSinceStartupAsDouble;
        Write("task_started", game.CurrentSentence);
    }

    public void EndTask(string result, string detail, int? grade = null)
    {
        SetPromptVisible(false);
        taskEnded = Time.realtimeSinceStartupAsDouble;
        Write(result, detail, grade);
    }

    public void RecordInput(bool backspace, bool correct)
    {
        if (backspace)
            backspaces++;
        else
        {
            inputs++;
            if (correct) correctInputs++;
        }

        Write(backspace ? "backspace" : "input", correct ? "correct" : "incorrect_or_blocked");
    }

    public void SetPromptVisible(bool visible)
    {
        if (promptVisible == visible) return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (promptVisible) promptSeconds += now - promptStarted;
        promptStarted = now;
        promptVisible = visible;
        Write(visible ? "prompt_shown" : "prompt_hidden");
    }

    public void Write(string eventName, string detail = "", int? grade = null)
    {
        if (writer == null) return;
        double now = Time.realtimeSinceStartupAsDouble;
        double visibleSeconds = promptSeconds + (promptVisible ? now - promptStarted : 0);
        object[] values =
        {
            1, sessionId, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), now - started,
            difficulty, game.TaskNumber, attempt, eventName, game.State, game.teacher.Current,
            game.TimeRemaining, game.Suspicion, game.Mistakes, inputs, correctInputs, backspaces,
            inputs > 0 ? (object)(100.0 * correctInputs / inputs) : "",
            attempt > 0 ? (object)((taskEnded ?? now) - taskStarted) : "", promptVisible, visibleSeconds,
            grade.HasValue ? (object)grade.Value : "", detail
        };
        try
        {
            writer.WriteLine(string.Join(",", Array.ConvertAll(values, CsvCell)));
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            DisableLogging(exception);
        }
    }

    static string CsvCell(object value)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        // Quoted cells preserve commas, quotes and line breaks in prompts and reasons.
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    static bool IsFileError(Exception exception)
    {
        return exception is IOException || exception is UnauthorizedAccessException
            || exception is System.Security.SecurityException || exception is NotSupportedException;
    }

    void DisableLogging(Exception exception)
    {
        Dispose();
        Debug.LogWarning("Gameplay CSV logging stopped. " + exception.Message);
    }

    public void Dispose()
    {
        StreamWriter closing = writer;
        writer = null;
        try
        {
            closing?.Dispose();
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            Debug.LogWarning("Could not close gameplay CSV. " + exception.Message);
        }
    }
}
