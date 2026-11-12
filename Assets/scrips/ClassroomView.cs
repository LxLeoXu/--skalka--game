using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public sealed class ClassroomView : MonoBehaviour
{
    [SerializeField] UIManager hud;
    [SerializeField] Shader flatColorShader;
    [SerializeField] GameObject oldLight;

    readonly Dictionary<Color32, Material> palette = new();
    Transform room;
    Transform teacher;
    Mesh cube;
    MeshRenderer signal;
    TMP_Text status;
    [SerializeField] GameManager game;
    GameObject statusPanel;
    TeacherAI.State displayedState = (TeacherAI.State)(-1);

    static readonly Color32 Ink = new(24, 33, 43, 255);
    static readonly Color32 Cream = new(240, 227, 199, 255);
    static readonly Color32 Wood = new(173, 112, 67, 255);
    static readonly Color32 Metal = new(53, 69, 75, 255);
    static readonly Color32 Teal = new(59, 105, 103, 255);
    static readonly Color32 Skin = new(222, 161, 110, 255);

    void Awake()
    {
        oldLight.SetActive(false);

        Camera view = GetComponent<Camera>();
        view.orthographic = false;
        view.fieldOfView = 48f;
        view.allowHDR = false;
        view.farClipPlane = 30f;
        view.transform.SetPositionAndRotation(new Vector3(0f, 1.56f, -4.4f), Quaternion.Euler(5f, 0, 0));
        view.backgroundColor = Cream;

        room = new GameObject("Low-poly classroom").transform;
        cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        BuildRoom();
        BuildTeacher();
        ArrangeHud();
    }

    void BuildRoom()
    {
        Box(room, "Floor", new(0, -.11f, 1), new(12, .2f, 17), new Color32(146, 130, 108, 255));
        Box(room, "Front wall", new(0, 2, 7), new(12, 4, .2f), Cream);
        Box(room, "Left wall", new(-6, 2, 1), new(.2f, 4, 12), new Color32(208, 194, 167, 255));
        Box(room, "Right wall", new(6, 2, 1), new(.2f, 4, 12), new Color32(216, 202, 176, 255));
        Box(room, "Ceiling", new(0, 4.1f, 1), new(12, .2f, 12), new Color32(222, 211, 191, 255));
        Box(room, "Baseboard", new(0, .12f, 6.87f), new(12, .24f, .09f), Wood);

        Box(room, "Board frame", new(0, 2.45f, 6.83f), new(5.9f, 1.85f, .18f), Wood);
        Box(room, "Chalkboard", new(0, 2.45f, 6.71f), new(5.7f, 1.65f, .09f), new Color32(35, 79, 70, 255));
        Box(room, "Chalk tray", new(0, 1.57f, 6.65f), new(5.9f, .06f, .24f), Metal);
        for (int row = 0; row < 3; row++)
            Box(room, "Chalk marks", new(-1.7f, 2.89f - row * .25f, 6.65f),
                new(1.45f - row * .3f, .026f, .02f), new Color32(163, 188, 159, 255));

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 4.35f;
            Box(room, "Window frame", new(x, 2.52f, 6.83f), new(1.85f, 1.75f, .16f), Metal);
            Box(room, "Window", new(x, 2.52f, 6.72f), new(1.67f, 1.57f, .1f), new Color32(163, 204, 208, 255));
            Box(room, "Mullion", new(x, 2.52f, 6.64f), new(.065f, 1.57f, .07f), Cream);
            Box(room, "Crossbar", new(x, 2.52f, 6.64f), new(1.67f, .065f, .07f), Cream);
            Desk(side * 2.35f, 1.1f, 1.35f);
            Desk(side * 2.35f, 3.1f, 1.35f);
        }

        Desk(1.35f, 5.35f, 1.85f);
        Desk(0, -1.9f, 1.9f);
        Box(room, "Notebook cover", new(.12f, .925f, -1.88f), new(.65f, .04f, .44f), Teal);
        Box(room, "Notebook pages", new(.13f, .952f, -1.88f), new(.59f, .025f, .41f), Cream);
        Box(room, "Pencil", new(-.32f, .931f, -1.89f), new(.032f, .05f, .31f), new Color32(243, 179, 62, 255));
    }

    void Desk(float x, float z, float width)
    {
        Box(room, "Desktop", new(x, .86f, z), new(width, .12f, .78f), Wood);
        Box(room, "Desk apron", new(x, .74f, z), new(width - .06f, .14f, .64f), Teal);
        for (int side = -1; side <= 1; side += 2)
        {
            float leg = x + side * (width * .5f - .1f);
            Box(room, "Desk leg", new(leg, .38f, z - .28f), new(.08f, .76f, .08f), Metal);
            Box(room, "Desk leg", new(leg, .38f, z + .28f), new(.08f, .76f, .08f), Metal);
        }
    }

    void BuildTeacher()
    {
        teacher = new GameObject("Teacher model").transform;
        teacher.SetParent(room, false);
        teacher.localPosition = new Vector3(0, 0, 4.8f);

        Box(teacher, "Jacket", new(0, 1.39f, 0), new(.66f, .73f, .38f), Teal);
        Box(teacher, "Shirt", new(0, 1.57f, .201f), new(.24f, .37f, .04f), Cream);
        Box(teacher, "Tie", new(0, 1.54f, .231f), new(.07f, .28f, .02f), Wood);
        Box(teacher, "Head", new(0, 2.02f, 0), new(.46f, .48f, .43f), Skin);
        Box(teacher, "Hair", new(0, 2.27f, -.027f), new(.49f, .13f, .46f), Ink);
        Box(teacher, "Back of hair", new(0, 2.1f, -.21f), new(.48f, .26f, .07f), Ink);
        Box(teacher, "Glasses bridge", new(0, 2.06f, .23f), new(.09f, .025f, .04f), Ink);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(teacher, "Glasses", new(side * .127f, 2.06f, .23f), new(.165f, .1f, .043f), Metal);
            Box(teacher, "Lens", new(side * .127f, 2.06f, .255f), new(.12f, .061f, .012f), Cream);
            Box(teacher, "Eye", new(side * .127f, 2.06f, .265f), new(.026f, .034f, .012f), Ink);
            Box(teacher, "Sleeve", new(side * .385f, 1.43f, 0), new(.18f, .58f, .31f), Teal);
            Box(teacher, "Hand", new(side * .385f, 1.08f, 0), new(.15f, .16f, .22f), Skin);
            Box(teacher, "Trouser leg", new(side * .167f, .59f, 0), new(.27f, .99f, .32f), Ink);
            Box(teacher, "Shoe", new(side * .167f, .09f, .08f), new(.29f, .17f, .49f), Metal);
        }

        signal = Box(room, "Teacher status light", new(0, 2.62f, 4.8f), new(.15f, .15f, .15f), Cream);
        signal.transform.rotation = Quaternion.Euler(18, 30, 45);
    }

    MeshRenderer Box(Transform parent, string label, Vector3 position, Vector3 size, Color32 color)
    {
        GameObject part = new(label, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<MeshFilter>().sharedMesh = cube;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = FlatMaterial(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    Material FlatMaterial(Color32 color)
    {
        if (!palette.TryGetValue(color, out Material material))
        {
            material = new Material(flatColorShader) { color = color };
            palette.Add(color, material);
        }

        return material;
    }

    void ArrangeHud()
    {
        foreach (string name in new[] { "Background", "TeacherDisc", "TeacherLabel", "TeacherPanel", "TypingPanel", "DangerOverlay" })
            hud.transform.Find(name).gameObject.SetActive(false);

        hud.teacher.GetComponent<SpriteRenderer>().enabled = false;
        hud.teacherImage = null;
        hud.redBorder = null;
        Transform root = hud.transform;
        StylePanel("TopBar", 0, .89f, 1, 1);
        StylePanel("TaskPanel", .04f, .055f, .96f, .235f);

        PlaceText(hud.gradeText, .04f, .89f, .28f, 1, 30);
        PlaceText(hud.timerText, .43f, .89f, .57f, 1, 34);
        PlaceText(hud.taskText, .07f, .09f, .93f, .19f, 40);
        PlaceText(hud.typedText, .07f, .09f, .93f, .19f, 40);
        PlaceText(hud.messageText, .12f, .34f, .88f, .71f, 52);
        Image resultBackdrop = AddPanel(root, "Result backdrop", .11f, .32f, .89f, .73f);
        resultBackdrop.transform.SetSiblingIndex(hud.messageText.transform.GetSiblingIndex());

        status = root.Find("TeacherStatus").GetComponent<TMP_Text>();
        PlaceText(status, .31f, .79f, .69f, .875f, 28);
        Image statusBackdrop = AddPanel(root, "Teacher status backdrop", .31f, .79f, .69f, .875f);
        statusBackdrop.transform.SetSiblingIndex(status.transform.GetSiblingIndex());
        statusPanel = statusBackdrop.gameObject;

        TMP_Text suspicion = root.Find("SuspicionLabel").GetComponent<TMP_Text>();
        PlaceText(suspicion, .72f, .93f, .96f, .987f, 23);
        Image track = AddPanel(root, "Suspicion track", .74f, .908f, .94f, .925f);
        track.color = new Color32(76, 86, 92, 255);
        track.transform.SetSiblingIndex(hud.suspicionFill.transform.GetSiblingIndex());
        Place(hud.suspicionFill.rectTransform, .74f, .908f, .94f, .925f);
        hud.suspicionFill.sprite = null;
        hud.suspicionFill.color = new Color32(244, 151, 73, 255);

        TMP_Text hint = root.Find("TaskLabel").GetComponent<TMP_Text>();
        hint.text = "PODRZ LAVE TLACIDLO MYSI PRE ZADANIE   |   ESC MENU";
        PlaceText(hint, .07f, .065f, .93f, .105f, 19);

        RectTransform menu = (RectTransform)hud.menuPanel.transform;
        Place(menu, .13f, .035f, .87f, .285f);
        Image menuBackdrop = AddPanel(menu, "Menu backdrop", 0, 0, 1, 1);
        menuBackdrop.transform.SetAsFirstSibling();

        TMP_Text title = new GameObject("Menu title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        title.transform.SetParent(menu, false);
        title.font = hud.gradeText.font;
        title.text = "TAJNE PISANIE";
        PlaceText(title, .04f, .61f, .96f, .98f, 46);

        Button[] choices = menu.GetComponentsInChildren<Button>(true);
        string[] labels = { "1  Lahka", "2  Stredna", "3  Tazka" };
        for (int i = 0; i < choices.Length; i++)
        {
            Button button = choices[i];
            float left = .035f + i * .315f;
            Place((RectTransform)button.transform, left, .13f, left + .30f, .55f);
            Image surface = button.GetComponent<Image>();
            surface.sprite = null;
            surface.color = Teal;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = labels[i];
            PlaceText(label, .04f, 0, .96f, 1, 28);
        }

        taskPanel = root.Find("TaskPanel").gameObject;
        taskHint = hint.gameObject;
        resultPanel = resultBackdrop.gameObject;
    }

    GameObject taskPanel;
    GameObject taskHint;
    GameObject resultPanel;

    void LateUpdate()
    {
        bool playing = game.State == GameManager.GameState.Playing;
        bool menu = game.State == GameManager.GameState.Menu;
        TeacherAI.State state = menu ? TeacherAI.State.Away : hud.teacher.Current;
        float angle = state == TeacherAI.State.Looking ? 180f : state == TeacherAI.State.Warning ? 68f : 0f;
        teacher.localRotation = Quaternion.RotateTowards(
            teacher.localRotation, Quaternion.Euler(0, angle, 0), 780f * Time.deltaTime);

        if (displayedState != state)
            ShowTeacherState(state);

        RectTransform fill = hud.suspicionFill.rectTransform;
        fill.anchorMax = new Vector2(Mathf.Lerp(.74f, .94f, game.Suspicion), .925f);
        hud.suspicionFill.enabled = !menu && game.Suspicion > 0f;
        taskPanel.SetActive(!menu);
        taskHint.SetActive(playing);
        statusPanel.SetActive(!menu);
        status.gameObject.SetActive(!menu);
        resultPanel.SetActive(!string.IsNullOrEmpty(hud.messageText.text));
    }

    void ShowTeacherState(TeacherAI.State state)
    {
        displayedState = state;
        Color32 color = state switch
        {
            TeacherAI.State.Looking => new Color32(255, 112, 110, 255),
            TeacherAI.State.Warning => new Color32(255, 198, 96, 255),
            _ => new Color32(148, 230, 166, 255)
        };
        signal.sharedMaterial = FlatMaterial(color);
        status.color = color;
        status.text = state switch
        {
            TeacherAI.State.Looking => "STOP  /  UCITEL SA POZERA",
            TeacherAI.State.Warning => "POZOR  /  UCITEL SA OTACA",
            _ => "SAFE  /  UCITEL JE OTOCENY"
        };

    }

    void StylePanel(string name, float left, float bottom, float right, float top)
    {
        Image image = hud.transform.Find(name).GetComponent<Image>();
        Place(image.rectTransform, left, bottom, right, top);
        image.sprite = null;
        image.color = Ink;
        image.raycastTarget = false;
    }

    static Image AddPanel(Transform parent, string label, float left, float bottom, float right, float top)
    {
        Image image = new GameObject(label, typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = Ink;
        image.raycastTarget = false;
        Place(image.rectTransform, left, bottom, right, top);
        return image;
    }

    static void PlaceText(TMP_Text label, float left, float bottom, float right, float top, float size)
    {
        Place(label.rectTransform, left, bottom, right, top);
        label.alignment = TextAlignmentOptions.Center;
        label.margin = Vector4.zero;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10;
        label.fontSizeMax = size;
        label.color = Cream;
        label.raycastTarget = false;
    }

    static void Place(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        foreach (Material material in palette.Values)
            Destroy(material);
    }
}
