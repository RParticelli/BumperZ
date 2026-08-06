using UnityEngine;

public sealed class BumperHurricaneVisual : MonoBehaviour
{
    BumperCarMotor target;
    Transform funnel;

    public static void Create(BumperCarMotor target, Vector3 center)
    {
        var root = new GameObject("FuracaoAtivo");
        root.transform.position = new Vector3(center.x, 0.05f, center.z);
        var effect = root.AddComponent<BumperHurricaneVisual>();
        effect.target = target;
        effect.funnel = BuildFunnel(root.transform, 1.35f);
    }

    public static Transform BuildFunnel(Transform parent, float size)
    {
        var root = new GameObject("FunilDeVento").transform;
        root.SetParent(parent, false);
        var material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        material.color = new Color(0.52f, 0.72f, 0.82f, 0.34f);
        material.SetFloat("_Metallic", 0.08f);
        material.SetFloat("_Glossiness", 0.72f);

        for (int i = 0; i < 6; i++)
        {
            var layer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            layer.name = "CamadaDeVento";
            Destroy(layer.GetComponent<CapsuleCollider>());
            layer.transform.SetParent(root, false);
            float t = i / 5f;
            float radius = Mathf.Lerp(0.16f, 0.62f, t) * size;
            layer.transform.localPosition = new Vector3(Mathf.Sin(i * 1.7f) * 0.06f * size, i * 0.13f * size, Mathf.Cos(i * 1.3f) * 0.05f * size);
            layer.transform.localScale = new Vector3(radius, 0.035f * size, radius);
            layer.GetComponent<Renderer>().sharedMaterial = material;
        }
        return root;
    }

    void Update()
    {
        if (!target || !target.IsHurricaneActive)
        {
            Destroy(gameObject);
            return;
        }
        funnel.Rotate(0f, 760f * Time.deltaTime, 0f, Space.Self);
        float pulse = 1f + Mathf.Sin(Time.time * 13f) * 0.06f;
        funnel.localScale = Vector3.one * pulse;
    }
}
