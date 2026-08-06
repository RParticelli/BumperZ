using UnityEngine;

public sealed class BumperRivalNameplate : MonoBehaviour
{
    BumperCarMotor target;
    TextMesh label;
    LineRenderer underline;

    public static void Create(BumperCarMotor target, Color driverColor)
    {
        var root = new GameObject("Nome_" + target.DriverName);
        var plate = root.AddComponent<BumperRivalNameplate>();
        plate.target = target;

        // Lift dark paint colors so every rival stays readable against the arena.
        Color readableColor = Color.Lerp(driverColor, Color.white, 0.18f);
        float peak = Mathf.Max(readableColor.r, Mathf.Max(readableColor.g, readableColor.b));
        if (peak < 0.72f) readableColor *= 0.72f / Mathf.Max(peak, 0.01f);
        readableColor.a = 0.98f;

        var glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(root.transform, false);
        glowObject.transform.localPosition = new Vector3(0f, 0f, 0.012f);
        var glow = glowObject.AddComponent<TextMesh>();
        Configure(glow, target.DriverName, new Color(readableColor.r, readableColor.g, readableColor.b, 0.22f), 0.071f);

        var shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(root.transform, false);
        shadowObject.transform.localPosition = new Vector3(0.007f, -0.007f, 0.006f);
        var shadow = shadowObject.AddComponent<TextMesh>();
        Configure(shadow, target.DriverName, new Color(0f, 0f, 0f, 0.88f), 0.066f);

        plate.label = root.AddComponent<TextMesh>();
        Configure(plate.label, target.DriverName, readableColor, 0.066f);
        root.GetComponent<MeshRenderer>().sortingOrder = 23;
        shadowObject.GetComponent<MeshRenderer>().sortingOrder = 21;
        glowObject.GetComponent<MeshRenderer>().sortingOrder = 20;

        plate.underline = root.AddComponent<LineRenderer>();
        plate.underline.useWorldSpace = false;
        plate.underline.positionCount = 2;
        plate.underline.SetPosition(0, new Vector3(-0.24f, -0.018f, 0.018f));
        plate.underline.SetPosition(1, new Vector3(0.24f, -0.018f, 0.018f));
        plate.underline.startWidth = 0.018f;
        plate.underline.endWidth = 0.018f;
        plate.underline.numCapVertices = 2;
        plate.underline.material = new Material(Shader.Find("Sprites/Default"));
        plate.underline.startColor = new Color(readableColor.r, readableColor.g, readableColor.b, 0.9f);
        plate.underline.endColor = new Color(readableColor.r, readableColor.g, readableColor.b, 0.28f);
        plate.underline.sortingOrder = 22;
    }

    static void Configure(TextMesh text, string value, Color color, float characterSize)
    {
        text.text = value;
        text.anchor = TextAnchor.LowerCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 64;
        text.characterSize = characterSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
    }

    void LateUpdate()
    {
        if (!target || !target.IsAlive)
        {
            gameObject.SetActive(false);
            return;
        }
        Camera camera = Camera.main;
        if (!camera) return;
        float height = target.IsGiant ? 1.26f : 0.70f;
        transform.position = target.transform.position + Vector3.up * height;
        transform.rotation = camera.transform.rotation;
    }
}
