using UnityEngine;

public sealed class BumperPickup : MonoBehaviour
{
    Vector3 basePosition;
    float phase;
    bool collected;
    Transform visual;
    Renderer coreRenderer;
    Material coreMaterial;
    Vector3 coreBaseScale;

    public static BumperPickup Create(Vector3 position)
    {
        var root = new GameObject("CaixaDePoder");
        root.transform.position = position;

        var trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.76f;

        var pickup = root.AddComponent<BumperPickup>();
        pickup.basePosition = position;
        pickup.phase = Random.value * 6f;
        pickup.BuildVisual();
        return pickup;
    }

    void BuildVisual()
    {
        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);

        GameObject model = Resources.Load<GameObject>("Models/PowerCrate");
        if (model)
        {
            BuildBlenderVisual(model);
            return;
        }

        Material metal = MakeMaterial("Metal dourado", new Color(0.72f, 0.34f, 0.035f), 0.88f, 0.34f);
        Material edge = MakeGlowMaterial("Energia dourada", new Color(1f, 0.62f, 0.06f), 1.25f);
        Material orange = MakeGlowMaterial("Detalhe ambar", new Color(1f, 0.28f, 0.015f), 1.15f);
        coreMaterial = MakeGlowMaterial("Nucleo dourado", new Color(1f, 0.72f, 0.12f), 1.55f);

        // A broad armored body keeps a clear square crate silhouette from the game camera.
        CreatePrimitive(PrimitiveType.Cube, "Corpo blindado", visual, Vector3.zero,
            new Vector3(0.74f, 0.58f, 0.74f), metal);
        CreatePrimitive(PrimitiveType.Cube, "Placa superior", visual, new Vector3(0f, 0.305f, 0f),
            new Vector3(0.58f, 0.045f, 0.58f), metal);

        var core = CreatePrimitive(PrimitiveType.Cube, "Nucleo", visual, new Vector3(0f, 0.35f, 0f),
            new Vector3(0.27f, 0.055f, 0.27f), coreMaterial);
        core.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        coreRenderer = core.GetComponent<Renderer>();
        coreBaseScale = core.transform.localScale;

        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            CreatePrimitive(PrimitiveType.Cube, "Canto reforcado", visual,
                new Vector3(x * 0.33f, 0.20f, z * 0.33f), new Vector3(0.105f, 0.22f, 0.105f), orange);
        }

        CreatePrimitive(PrimitiveType.Cube, "Faixa frontal", visual, new Vector3(0f, 0f, -0.376f),
            new Vector3(0.36f, 0.07f, 0.018f), edge);
        CreatePrimitive(PrimitiveType.Cube, "Faixa traseira", visual, new Vector3(0f, 0f, 0.376f),
            new Vector3(0.36f, 0.07f, 0.018f), edge);

        var lightObject = new GameObject("Luz suave");
        lightObject.transform.SetParent(visual, false);
        var glow = lightObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.02f, 0.65f, 1f);
        glow.range = 1.35f;
        glow.intensity = 0.35f;
        glow.shadows = LightShadows.None;
    }

    void BuildBlenderVisual(GameObject model)
    {
        GameObject instance = Instantiate(model, visual);
        instance.name = "PowerCrate_LowPoly";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Material graphite = MakeMaterial("Ouro mobile", new Color(0.72f, 0.34f, 0.035f), 0.88f, 0.3f);
        Material dark = MakeMaterial("Ouro escuro mobile", new Color(0.15f, 0.055f, 0.008f), 0.66f, 0.4f);
        Material orange = MakeGlowMaterial("Ambar mobile", new Color(1f, 0.34f, 0.015f), 1.05f);
        coreMaterial = MakeGlowMaterial("Nucleo dourado mobile", new Color(1f, 0.72f, 0.12f), 1.55f);

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] source = renderer.sharedMaterials;
            Material[] assigned = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                string materialName = source[i] ? source[i].name.ToLowerInvariant() : "";
                if (materialName.Contains("core")) assigned[i] = coreMaterial;
                else if (materialName.Contains("orange")) assigned[i] = orange;
                else if (materialName.Contains("dark")) assigned[i] = dark;
                else assigned[i] = graphite;
            }
            renderer.sharedMaterials = assigned;

            if (renderer.gameObject.name.Contains("Core"))
                coreRenderer = renderer;
        }

        if (!coreRenderer)
            coreRenderer = instance.GetComponentInChildren<Renderer>();
        coreBaseScale = coreRenderer.transform.localScale;
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().material = material;
        return part;
    }

    static Material MakeMaterial(string name, Color color, float metallic, float smoothness)
    {
        var material = new Material(Shader.Find("Standard")) { name = name, color = color };
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    static Material MakeGlowMaterial(string name, Color color, float strength)
    {
        var material = MakeMaterial(name, color, 0.3f, 0.76f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * strength);
        return material;
    }

    void Update()
    {
        float time = Time.time + phase;
        transform.position = basePosition + Vector3.up * (Mathf.Sin(time * 2.25f) * 0.105f);
        visual.Rotate(0f, 42f * Time.deltaTime, 0f, Space.World);
        float pulse = 0.96f + Mathf.Sin(time * 4.8f) * 0.07f;
        coreRenderer.transform.localScale = coreBaseScale * pulse;
        coreMaterial.SetColor("_EmissionColor", new Color(1f, 0.58f, 0.055f) * (1.25f + pulse * 0.5f));
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        var car = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<BumperCarMotor>() : null;
        if (car && car.IsAlive)
        {
            collected = true;
            BumperGame.Instance.PickupCollected(this, car);
        }
    }
}
