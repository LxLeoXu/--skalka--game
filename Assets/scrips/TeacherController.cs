using TMPro;
using UnityEngine;

// IBA vizualizacia stavu; casovanie riadi TeacherAI.
// Je to nahrada povodneho TeacherController, aby ucitel nemal dva nezavisle casovace.
public class TeacherController : MonoBehaviour
{
    [Header("References")]
    public TeacherAI teacher;
    public TMP_Text statusText;           // volitelny text SAFE / POZOR / NEBEZPECENSTVO
    public Transform directionPointer;  // volitelna sipka ako child ucitela

    [Header("Otacanie sipky (stupne okolo osi Z)")]
    public float awayAngle = 180f;
    public float warningAngle = 90f;
    public float lookingAngle = 0f;
    public float turnSpeed = 8f;

    TeacherAI.State previousState = (TeacherAI.State)(-1);

    void Awake()
    {
        if (teacher == null)
            teacher = GetComponent<TeacherAI>();
    }

    void Update()
    {
        if (teacher == null) return;

        if (teacher.Current != previousState)
        {
            previousState = teacher.Current;
            UpdateStatusText();
        }

        if (directionPointer != null)
        {
            float angle = awayAngle;
            if (teacher.Current == TeacherAI.State.Warning) angle = warningAngle;
            if (teacher.Current == TeacherAI.State.Looking) angle = lookingAngle;

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            directionPointer.localRotation = Quaternion.Slerp(
                directionPointer.localRotation,
                targetRotation,
                Mathf.Clamp01(Time.deltaTime * turnSpeed));
        }
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;

        switch (teacher.Current)
        {
            case TeacherAI.State.Away:
                statusText.text = "SAFE - ucitel je otoceny";
                statusText.color = new Color(0.35f, 1f, 0.45f);
                break;
            case TeacherAI.State.Warning:
                statusText.text = "POZOR - ucitel sa otaca!";
                statusText.color = new Color(1f, 0.75f, 0.15f);
                break;
            case TeacherAI.State.Looking:
                statusText.text = "STOP - ucitel sa pozera!";
                statusText.color = new Color(1f, 0.3f, 0.3f);
                break;
        }
    }
}
