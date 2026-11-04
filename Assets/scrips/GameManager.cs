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

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Between, Lost, Finished }

    [Header("Referencie")]
    public TeacherAI teacher;
    public TypingController typing;
    public UIManager ui;

    [Header("Podozrenie")]
    public float suspicionPerTypo = 0.15f;
    public float suspicionPerKeyInWarning = 0.08f;
    public float suspicionDecayPerSecond = 0.1f;

    [Header("Ulohy - mozes ich menit v Inspectore")]
    public TaskConfig[] tasks =
    {
        new TaskConfig { sentence = "/zhrn #tema @ziak", timeLimit = 25f,
                         away = 3.5f, warning = 0.9f, looking = 2f, randomness = 0.2f, fakeAlarmChance = 0f },
        new TaskConfig { sentence = "/vysvetli {fotosynteza} [3 vety]", timeLimit = 30f,
                         away = 3f, warning = 0.8f, looking = 2f, randomness = 0.4f, fakeAlarmChance = 0f },
        new TaskConfig { sentence = "@ai /preloz #angl ~strucne {max:50}", timeLimit = 35f,
                         away = 2.2f, warning = 0.6f, looking = 1.8f, randomness = 0.8f, fakeAlarmChance = 0.3f },
    };

    public GameState State { get; private set; } = GameState.Between;
    public float Suspicion { get; private set; }
    public string CurrentSentence => tasks[index].sentence;

    int index;
    int mistakes;
    float timeLeft;
    float endTime;
    readonly List<int> grades = new List<int>();

    void Start()
    {
        if (teacher == null || typing == null || ui == null || tasks == null || tasks.Length == 0)
        {
            Debug.LogError("GameManager: Prirad TeacherAI, TypingController, UIManager a aspon 1 task v Inspectore!", this);
            enabled = false;
            return;
        }

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
    }

    void Update()
    {
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
            {
                grades.Clear();
                StartTask(0);
            }
            else
            {
                StartTask(index);
            }
        }
    }

    public void RegisterTypo()
    {
        if (State != GameState.Playing) return;
        mistakes++;
        AddSuspicion(suspicionPerTypo);
    }

    public void AddSuspicion(float amount)
    {
        if (State != GameState.Playing) return;
        Suspicion = Mathf.Clamp01(Suspicion + amount);
        ui.SetSuspicion(Suspicion);
        if (Suspicion >= 1f)
            Lose("Ucitel nadobudol podozrenie!");
    }

    public void Lose(string reason)
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        endTime = Time.time;
        teacher.Stop();
        ui.SetSuspicion(Suspicion);
        ui.ShowMessage("CHYTENY!\n" + reason + "\n\n<size=50%>Klikni pre opakovanie</size>");
    }

    public void CompleteTask()
    {
        if (State != GameState.Playing) return;
        State = GameState.Between;
        teacher.Stop();

        int grade = CalculateGrade();
        grades.Add(grade);
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

            ui.ShowMessage("KONIEC!\nPriemer: " + average.ToString("0.0")
                + "\n\n<size=50%>Klikni pre novu hru</size>");
        }
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
