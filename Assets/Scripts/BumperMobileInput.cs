using UnityEngine;

public sealed class BumperMobileInput : MonoBehaviour
{
    public static Vector2 Value { get; private set; }
    public static bool IsDriving { get; private set; }
    static bool powerQueued;

    const float MaxRadius = 92f;
    int touchId = -1;
    Vector2 origin;
    Vector2 knob;
    Texture2D baseTexture;
    Texture2D knobTexture;
    Texture2D spinShotIcon;
    Texture2D shieldIcon;
    Texture2D giantIcon;
    Texture2D hurricaneIcon;
    float retainedDrive;

    void Awake()
    {
        baseTexture = CircleTexture(128, new Color(0.03f, 0.04f, 0.05f, 0.38f), new Color(1f, 1f, 1f, 0.18f));
        knobTexture = CircleTexture(96, new Color(0.95f, 0.35f, 0.08f, 0.68f), new Color(1f, 0.72f, 0.25f, 0.9f));
        spinShotIcon = Resources.Load<Texture2D>("UI/SpinShot");
        shieldIcon = Resources.Load<Texture2D>("UI/Shield");
        giantIcon = Resources.Load<Texture2D>("UI/Giant");
        hurricaneIcon = Resources.Load<Texture2D>("UI/Hurricane");
    }

    void Update()
    {
        if (BumperGame.Instance && BumperGame.Instance.IsMatchFinished)
        {
            if (IsDriving) End();
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouches();
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        Vector2 mouse = Input.mousePosition;
        if (Input.GetMouseButtonDown(0) && mouse.x <= Screen.width * 0.55f)
            Begin(mouse, 0);
        if (IsDriving && touchId == 0 && Input.GetMouseButton(0))
            Move(mouse);
        if (IsDriving && touchId == 0 && Input.GetMouseButtonUp(0))
            End();
#endif
    }

    void HandleTouches()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && touchId < 0 && touch.position.x <= Screen.width * 0.55f)
                Begin(touch.position, touch.fingerId);
            if (touch.fingerId != touchId) continue;
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                Move(touch.position);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                End();
        }
    }

    void Begin(Vector2 screenPosition, int id)
    {
        touchId = id;
        origin = screenPosition;
        knob = screenPosition;
        Value = Vector2.zero;
        retainedDrive = 0f;
        IsDriving = true;
    }

    void Move(Vector2 screenPosition)
    {
        Vector2 delta = Vector2.ClampMagnitude(screenPosition - origin, MaxRadius);
        knob = origin + delta;
        Vector2 raw = delta / MaxRadius;

        // Preserve the last forward/reverse command when the thumb moves to a
        // purely horizontal steering position. This allows long turns without
        // accidentally releasing the accelerator.
        if (Mathf.Abs(raw.y) >= 0.18f)
            retainedDrive = raw.y;
        else if (Mathf.Abs(raw.x) >= 0.18f && Mathf.Abs(retainedDrive) >= 0.05f)
            raw.y = retainedDrive;

        Value = raw;
    }

    void End()
    {
        touchId = -1;
        retainedDrive = 0f;
        Value = Vector2.zero;
        IsDriving = false;
    }

    void OnGUI()
    {
        if (BumperGame.Instance && BumperGame.Instance.IsMatchFinished)
            return;

        if (IsDriving)
        {
            DrawCentered(baseTexture, origin, 176f);
            DrawCentered(knobTexture, knob, 78f);
        }

        GUIStyle buttonStyle = new GUIStyle(GUIStyle.none)
        {
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter
        };
        float size = Mathf.Clamp(Screen.height * 0.16f, 90f, 150f);
        Rect powerRect = new Rect(Screen.width - size - 34f, Screen.height - size - 42f, size, size);
        Texture2D icon = null;
        if (BumperCarMotor.LocalPlayer)
        {
            if (BumperCarMotor.LocalPlayer.HeldPowerId == 2) icon = spinShotIcon;
            else if (BumperCarMotor.LocalPlayer.HeldPowerId == 3) icon = shieldIcon;
            else if (BumperCarMotor.LocalPlayer.HeldPowerId == 4) icon = giantIcon;
            else if (BumperCarMotor.LocalPlayer.HeldPowerId == 5) icon = hurricaneIcon;
        }
        GUIContent content = new GUIContent(icon ? icon : baseTexture);
        if (GUI.Button(powerRect, content, buttonStyle))
            powerQueued = true;
    }

    public static bool ConsumePower()
    {
        if (!powerQueued) return false;
        powerQueued = false;
        return true;
    }

    static void DrawCentered(Texture texture, Vector2 screenPosition, float size)
    {
        float guiY = Screen.height - screenPosition.y;
        GUI.DrawTexture(new Rect(screenPosition.x - size * 0.5f, guiY - size * 0.5f, size, size), texture);
    }

    static Texture2D CircleTexture(int size, Color fill, Color rim)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeJoystickCircle";
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            if (d > radius) pixels[y * size + x] = Color.clear;
            else if (d > radius - 5f) pixels[y * size + x] = rim;
            else pixels[y * size + x] = fill;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
