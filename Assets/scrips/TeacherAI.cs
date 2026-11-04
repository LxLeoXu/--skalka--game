using UnityEngine;

// Jediny skript, ktory rozhoduje, ci sa ucitel pozera.
public class TeacherAI : MonoBehaviour
{
    public enum State { Away, Warning, Looking }

    public State Current { get; private set; } = State.Away;
    public bool IsLooking => Current == State.Looking;
    public bool IsWarning => Current == State.Warning;

    float awayTime = 3f;
    float warningTime = 0.8f;
    float lookingTime = 2f;
    float randomness = 0.3f;
    float fakeAlarmChance;
    float timer;
    bool running;

    // GameManager to vola na zaciatku kazdej ulohy.
    public void Configure(float away, float warning, float looking, float random, float fakeAlarm)
    {
        awayTime = Mathf.Max(0.5f, away);
        warningTime = Mathf.Max(0.1f, warning);
        lookingTime = Mathf.Max(0.1f, looking);
        randomness = Mathf.Max(0f, random);
        fakeAlarmChance = Mathf.Clamp01(fakeAlarm);

        Current = State.Away;
        timer = RandomAway();
        running = true;
    }

    public void Stop() => running = false;

    float RandomAway()
    {
        return Mathf.Max(0.5f, awayTime + Random.Range(-randomness, randomness));
    }

    void Update()
    {
        if (!running) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        switch (Current)
        {
            case State.Away:
                Current = State.Warning;
                timer = warningTime;
                break;

            case State.Warning:
                if (Random.value < fakeAlarmChance)
                {
                    // Obcas sa ucitel iba pohne, ale neotoci sa.
                    Current = State.Away;
                    timer = RandomAway();
                }
                else
                {
                    Current = State.Looking;
                    timer = lookingTime;
                }
                break;

            case State.Looking:
                Current = State.Away;
                timer = RandomAway();
                break;
        }
    }
}
