using UnityEngine;

public sealed class BumperOilTrackMark : MonoBehaviour
{
    static Material sharedMaterial;
    float bornAt;
    const float Lifetime = 3.2f;

    public static void Create(Vector3 position, float heading)
    {
        var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mark.name = "MarcaDeOleo";
        Destroy(mark.GetComponent<MeshCollider>());
        mark.transform.position = position;
        mark.transform.rotation = Quaternion.Euler(90f, heading, 0f);
        mark.transform.localScale = new Vector3(0.14f, 0.24f, 1f);

        if (!sharedMaterial)
        {
            Shader shader = Shader.Find("BumperZ/OilTrack");
            sharedMaterial = new Material(shader ? shader : Shader.Find("Unlit/Color"));
            sharedMaterial.color = new Color(0.008f, 0.012f, 0.018f, 0.9f);
        }
        mark.GetComponent<MeshRenderer>().sharedMaterial = sharedMaterial;
        var component = mark.AddComponent<BumperOilTrackMark>();
        component.bornAt = Time.time;
    }

    void Update()
    {
        float age = Time.time - bornAt;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (age > Lifetime - 0.55f)
        {
            float scale = Mathf.Clamp01((Lifetime - age) / 0.55f);
            transform.localScale = new Vector3(0.14f * scale, 0.24f * scale, 1f);
        }
    }
}
