using UnityEngine;

public sealed class BumperSpinHitEffect : MonoBehaviour
{
    BumperCarMotor target;
    Transform orbit;

    public static void Create(BumperCarMotor target)
    {
        CreateImpactBurst(target.transform.position + Vector3.up * 0.3f);

        var root = new GameObject("EfeitoTiroDeGiro");
        root.transform.SetParent(target.transform, false);
        root.transform.localPosition = new Vector3(0f, 0.74f, 0f);
        var effect = root.AddComponent<BumperSpinHitEffect>();
        effect.target = target;
        effect.orbit = root.transform;

        Shader shader = Shader.Find("Standard");
        var material = new Material(shader);
        material.color = new Color(1f, 0.24f, 0.025f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.08f, 0.01f) * 3f);

        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "FaíscaDeAtordoamento";
            Destroy(spark.GetComponent<SphereCollider>());
            spark.transform.SetParent(root.transform, false);
            spark.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.55f, i * 0.07f, Mathf.Sin(angle) * 0.55f);
            spark.transform.localScale = Vector3.one * 0.13f;
            spark.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    static void CreateImpactBurst(Vector3 position)
    {
        var root = new GameObject("ExplosaoTiroDeGiro");
        root.transform.position = position;
        var ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.12f, 0.01f), new Color(1f, 0.78f, 0.05f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader)
        {
            var particleMaterial = new Material(particleShader);
            particleMaterial.color = new Color(1f, 0.32f, 0.025f, 1f);
            renderer.material = particleMaterial;
        }
        ps.Emit(22);
        ps.Play();
    }

    void Update()
    {
        if (!target || !target.IsSpinShotActive)
        {
            Destroy(gameObject);
            return;
        }
        orbit.Rotate(0f, 520f * Time.deltaTime, 0f, Space.Self);
        float pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.12f;
        orbit.localScale = Vector3.one * pulse;
    }
}
