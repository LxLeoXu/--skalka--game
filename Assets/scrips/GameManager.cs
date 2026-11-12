using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class TaskConfig
{
    public string sentence;
    public float timeLimit = 30f;
    public float away = 3f;
    public float warning = 0.8f;
    public float looking = 2f;
    public float randomness = 0.3f;
    [Range(0f, 1f)] public float fakeAlarmChance = 0f;
}

[System.Serializable]
public class DifficultyConfig
{
    public string name = "Lahka";
    public TaskConfig[] tasks;
}

public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Playing, Between, Lost, Finished }

    [Header("Referencie")]
    public TeacherAI teacher;
    public TypingController typing;
    public UIManager ui;

    [Header("Podozrenie")]
    public float suspicionPerTypo = 0.15f;
    public float suspicionPerKeyInWarning = 0.08f;
    public float suspicionDecayPerSecond = 0.1f;

    [Header("Obtiaznosti - kazda ma svoje 3 texty")]
    public DifficultyConfig[] difficulties = CreateDefaultDifficulties();

    public GameState State { get; private set; } = GameState.Menu;
    public float Suspicion { get; private set; }
    public string CurrentSentence =>
        (tasks != null && index >= 0 && index < tasks.Length) ? tasks[index].sentence : "";

    public int TaskNumber => tasks == null ? 0 : index + 1;
    public float TimeRemaining => Mathf.Max(0f, timeLeft);
    public int Mistakes => mistakes;

    GameplayCsvLogger gameplayLog;

    TaskConfig[] tasks;
    int difficultyIndex = -1;
    int index;
    int mistakes;
    float timeLeft;
    float endTime;
    readonly List<int> grades = new List<int>();

    static DifficultyConfig[] CreateDefaultDifficulties()
    {
        return new[]
        {
            new DifficultyConfig
            {
                name = "Lahka",
                tasks = new[]
                {
                    new TaskConfig { sentence = "/zhrn #tema", timeLimit = 30f,
                        away = 4f, warning = 1.0f, looking = 1.5f, randomness = 0.1f, fakeAlarmChance = 0f },
                    new TaskConfig { sentence = "/vysvetli {pojem}", timeLimit = 30f,
                        away = 4f, warning = 1.0f, looking = 1.5f, randomness = 0.1f, fakeAlarmChance = 0f },
                    new TaskConfig { sentence = "/pis @ziak #esej", timeLimit = 30f,
                        away = 3.8f, warning = 0.9f, looking = 1.5f, randomness = 0.2f, fakeAlarmChance = 0f },
                }
            },
            new DifficultyConfig
            {
                name = "Stredna",
                tasks = new[]
                {
                    new TaskConfig { sentence = "/zhrn #tema @ziak", timeLimit = 25f,
                        away = 3.2f, warning = 0.8f, looking = 2f, randomness = 0.3f, fakeAlarmChance = 0f },
                    new TaskConfig { sentence = "/vysvetli {fotosynteza} [3 vety]", timeLimit = 30f,
                        away = 3f, warning = 0.8f, looking = 2f, randomness = 0.4f, fakeAlarmChance = 0.1f },
                    new TaskConfig { sentence = "/oprav #gramatika ~strucne", timeLimit = 30f,
                        away = 2.8f, warning = 0.7f, looking = 2f, randomness = 0.5f, fakeAlarmChance = 0.15f },
                }
            },
            new DifficultyConfig
            {
                name = "Tazka",
                tasks = new[]
                {
                    new TaskConfig { sentence = "@ai /preloz #angl ~strucne {max:50}", timeLimit = 35f,
                        away = 2.2f, warning = 0.6f, looking = 1.8f, randomness = 0.8f, fakeAlarmChance = 0.3f },
                    new TaskConfig { sentence = "@ai /napis {esej} [5 viet] #dejepis", timeLimit = 40f,
                        away = 2f, warning = 0.5f, looking = 1.8f, randomness = 0.9f, fakeAlarmChance = 0.35f },
                    new TaskConfig { sentence = "@ai /vygeneruj #zhrnutie ~formalne {max:80} [2 vety]", timeLimit = 45f,
                        away = 1.8f, warning = 0.5f, looking = 2f, randomness = 1.0f, fakeAlarmChance = 0.4f },
                }
            },
        };
    }

    void Start()
    {
        // Ak su stare serializovane data prazdne, pouzi predvolene obtiaznosti.
        if (difficulties == null || difficulties.Length == 0)
            difficulties = CreateDefaultDifficulties();

        var missing = new List<string>();
        if (teacher == null) missing.Add("Teacher");
        if (typing == null) missing.Add("Typing");
        if (ui == null) missing.Add("Ui");

        for (int i = 0; i < difficulties.Length; i++)
        {
            if (difficulties[i] == null || difficulties[i].tasks == null || difficulties[i].tasks.Length == 0)
                missing.Add("difficulties[" + i + "].tasks");
        }

        if (missing.Count > 0)
        {
            Debug.LogError("GameManager: chyba nastavenie: " + string.Join(", ", missing), this);
            enabled = false;
            return;
        }

        teacher.StateChanged += LogTeacherState;
        ShowMenu();
    }

    void ShowMenu()
    {
        EndGameplayLogging("returned_to_menu");
        StopAllCoroutines();
        State = GameState.Menu;
        teacher.Stop();
        grades.Clear();
        tasks = null;
        index = 0;
        ui.ShowMenu(true);
        Debug.Log("GameManager: menu zobrazene.");
    }

    // Volaj z OnClick tlacidiel: 0 = Lahka, 1 = Stredna, 2 = Tazka
    public void SelectDifficulty(int i)
    {
        Debug.Log("GameManager.SelectDifficulty(" + i + "), state = " + State);

        if (State != GameState.Menu) return;
        if (i < 0 || i >= difficulties.Length)
        {
            Debug.LogError("GameManager: obtiaznost " + i + " neexistuje.", this);
            return;
        }

        difficultyIndex = i;
        index = -1;
        timeLeft = 0f;
        mistakes = 0;
        Suspicion = 0f;
        tasks = difficulties[i].tasks;
        gameplayLog = new GameplayCsvLogger(this, difficulties[i].name);
        gameplayLog.Write("session_started");
        grades.Clear();
        ui.ShowMenu(false);
        StartTask(0);
    }

    void StartTask(int i)
    {
        index = i;
        mistakes = 0;
        Suspicion = 0f;
        timeLeft = Mathf.Max(1f, tasks[i].timeLimit);
        State = GameState.Playing;

        TaskConfig t = tasks[i];
        teacher.Configure(t.away, t.warning, t.looking, t.randomness, t.fakeAlarmChance);

        ui.ShowTask(t.sentence, i + 1, tasks.Length);
        ui.SetTime(timeLeft);
        typing.ResetTyping();
        gameplayLog?.StartTask();

        Debug.Log("GameManager: start ulohy " + (i + 1) + "/" + tasks.Length + ": " + t.sentence);
    }

    void Update()
    {
        var kb = Keyboard.current;

        // Esc = spat do menu
        if (State != GameState.Menu && kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            ShowMenu();
            return;
        }

        // Klavesove skratky v menu (1/2/3) - uzitocne aj na testovanie
        if (State == GameState.Menu && kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) SelectDifficulty(0);
            else if (kb.digit2Key.wasPressedThisFrame) SelectDifficulty(1);
            else if (kb.digit3Key.wasPressedThisFrame) SelectDifficulty(2);
        }

        if (State == GameState.Playing)
        {
            timeLeft -= Time.deltaTime;
            Suspicion = Mathf.Max(0f, Suspicion - suspicionDecayPerSecond * Time.deltaTime);

            ui.SetTime(timeLeft);
            ui.SetSuspicion(Suspicion);

            if (timeLeft <= 0f)
                Lose("Cas vyprsal!");
        }
        else if ((State == GameState.Lost || State == GameState.Finished)
                 && Time.time - endTime > 0.7f
                 && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (State == GameState.Finished)
                ShowMenu();
            else
                StartTask(index);
        }
    }

    public void RegisterTypo()
    {
        if (State != GameState.Playing) return;
        mistakes++;
        gameplayLog?.Write("typo_registered");
        AddSuspicion(suspicionPerTypo);
    }

    public void AddSuspicion(float amount)
    {
        if (State != GameState.Playing) return;
        Suspicion = Mathf.Clamp01(Suspicion + amount);
        ui.SetSuspicion(Suspicion);
        gameplayLog?.Write("suspicion_added");
        if (Suspicion >= 1f)
            Lose("Ucitel nadobudol podozrenie!");
    }

    public void Lose(string reason)
    {
        if (State != GameState.Playing) return;
        gameplayLog?.EndTask("task_failed", reason);
        State = GameState.Lost;
        endTime = Time.time;
        teacher.Stop();
        ui.SetSuspicion(Suspicion);
        ui.ShowMessage("CHYTENY!\n" + reason + "\n\n<size=50%>Klikni pre opakovanie\nEsc = menu</size>");
    }

    public void CompleteTask()
    {
        if (State != GameState.Playing) return;
        State = GameState.Between;
        teacher.Stop();

        int grade = CalculateGrade();
        grades.Add(grade);
        gameplayLog?.EndTask("task_completed", "", grade);
        ui.ShowGrade(grade);
        StartCoroutine(NextTaskRoutine());
    }

    IEnumerator NextTaskRoutine()
    {
        yield return new WaitForSeconds(1.8f);

        if (index + 1 < tasks.Length)
        {
            StartTask(index + 1);
        }
        else
        {
            State = GameState.Finished;
            endTime = Time.time;

            float sum = 0f;
            foreach (int g in grades) sum += g;
            float average = sum / grades.Count;
            EndGameplayLogging("completed");

            ui.ShowMessage("KONIEC! (" + difficulties[difficultyIndex].name + ")\nPriemer: "
                + average.ToString("0.0")
                + "\n\n<size=50%>Klikni pre vyber obtiaznosti</size>");
        }
    }

    public void LogInput(bool backspace, bool correct)
    {
        gameplayLog?.RecordInput(backspace, correct);
    }

    public void LogPromptVisibility(bool visible)
    {
        gameplayLog?.SetPromptVisible(visible);
    }

    void LogTeacherState(TeacherAI.State state)
    {
        if (State == GameState.Playing)
            gameplayLog?.Write("teacher_state_changed", state.ToString());
    }

    void EndGameplayLogging(string reason)
    {
        if (gameplayLog == null) return;
        if (State == GameState.Playing)
            gameplayLog.EndTask("task_abandoned", reason);
        gameplayLog.Write("session_ended", reason);
        gameplayLog.Dispose();
        gameplayLog = null;
    }

    void OnApplicationFocus(bool focused)
    {
        gameplayLog?.Write(focused ? "focus_gained" : "focus_lost");
    }

    void OnApplicationPause(bool paused)
    {
        gameplayLog?.Write(paused ? "application_paused" : "application_resumed");
    }

    void OnApplicationQuit() => EndGameplayLogging("application_quit");

    void OnDisable() => EndGameplayLogging("game_disabled");

    void OnDestroy()
    {
        if (teacher != null) teacher.StateChanged -= LogTeacherState;
        EndGameplayLogging("scene_closed");
    }
    // Slovenske znamkovanie: 1 = najlepsia, 4 = najhorsia.
    int CalculateGrade()
    {
        float limit = Mathf.Max(1f, tasks[index].timeLimit);
        float usedRatio = (limit - timeLeft) / limit;

        int penalty = mistakes;
        if (usedRatio > 0.8f) penalty += 2;
        else if (usedRatio > 0.5f) penalty += 1;

        return Mathf.Clamp(1 + penalty, 1, 4);
    }
}