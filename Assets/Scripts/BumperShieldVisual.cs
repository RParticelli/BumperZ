using UnityEngine;

public sealed class BumperShieldVisual : MonoBehaviour
{
    BumperCarMotor target;
    Material material;
    float flashUntil;

    public static void Create(BumperCarMotor target)
    {
        var existing = target.GetComponentInChildren<BumperShieldVisual>();
        if (existing)
        {
            existing.Flash();
            return;
        }

        var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bubble.name = "EscudoVisual";
        Destroy(bubble.GetComponent<SphereCollider>());
        bubble.transform.SetParent(target.transform, false);
        bubble.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        bubble.transform.localScale = new Vector3(1.38f, 0.88f, 1.58f);

        Shader shader = Shader.Find("Standard");
        var material = new Material(shader);
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = new Color(0.03f, 0.62f, 1f, 0.23f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.01f, 0.20f, 0.48f));
        material.SetFloat("_Metallic", 0.25f);
        material.SetFloat("_Glossiness", 0.92f);

        var renderer = bubble.GetComponent<MeshRenderer>();
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var visual = bubble.AddComponent<BumperShieldVisual>();
        visual.target = target;
        visual.material = material;
    }

    public void Flash()
    {
        flashUntil = Time.time + 0.28f;
    }

    void Update()
    {
        if (!target || !target.IsShieldActive)
        {
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 5.5f) * 0.025f;
        if (Time.time < flashUntil) pulse += 0.16f;
        transform.localScale = new Vector3(1.38f, 0.88f, 1.58f) * pulse;
        Color color = Time.time < flashUntil
            ? new Color(0.35f, 0.92f, 1f, 0.52f)
            : new Color(0.03f, 0.62f, 1f, 0.23f);
        material.color = color;
        transform.Rotate(0f, 24f * Time.deltaTime, 0f);
    }
}
