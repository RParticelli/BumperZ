using UnityEngine;
using System.Collections.Generic;

public sealed class BumperOilSlick : MonoBehaviour
{
    BumperCarMotor owner;
    float safeUntil;
    float destroyAt;
    readonly HashSet<int> affectedDrivers = new();

    public static BumperOilSlick Create(Vector3 position, BumperCarMotor owner)
    {
        var oil = new GameObject("PocaDeOleo", typeof(MeshFilter), typeof(MeshRenderer));
        oil.name = "PocaDeOleo";
        oil.transform.position = position;
        oil.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        var trigger = oil.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.12f;

        oil.GetComponent<MeshFilter>().sharedMesh = CreatePuddleMesh();

        Shader oilShader = Shader.Find("BumperZ/OilPuddle");
        var material = new Material(oilShader ? oilShader : Shader.Find("Standard"));
        material.SetColor("_BaseColor", new Color(0.025f, 0.038f, 0.055f, 1f));
        material.SetColor("_SheenA", new Color(0.02f, 0.58f, 0.92f, 1f));
        material.SetColor("_SheenB", new Color(0.72f, 0.04f, 0.42f, 1f));
        oil.GetComponent<MeshRenderer>().material = material;

        var slick = oil.AddComponent<BumperOilSlick>();
        slick.owner = owner;
        slick.safeUntil = Time.time + 0.8f;
        slick.destroyAt = Time.time + 7f;
        return slick;
    }

    static Mesh CreatePuddleMesh()
    {
        const int segments = 24;
        // Center + raised inner ring + irregular lip + vertical lower edge.
        var vertices = new Vector3[1 + segments * 3];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments * 15];
        vertices[0] = Vector3.zero;
        vertices[0].y = 0.075f;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float irregular = 1f + Mathf.Sin(angle * 3f + 0.7f) * 0.16f +
                              Mathf.Sin(angle * 5f - 0.4f) * 0.10f;
            float radiusX = 1.18f * irregular;
            float radiusZ = 0.94f * irregular;
            float x = Mathf.Cos(angle) * radiusX;
            float z = Mathf.Sin(angle) * radiusZ;
            int inner = 1 + i;
            int outer = 1 + segments + i;
            int lower = 1 + segments * 2 + i;
            vertices[inner] = new Vector3(x * 0.58f, 0.115f, z * 0.58f);
            vertices[outer] = new Vector3(x, 0.035f, z);
            vertices[lower] = new Vector3(x * 0.96f, -0.055f, z * 0.96f);
            uv[inner] = new Vector2(vertices[inner].x / 2.8f + 0.5f, vertices[inner].z / 2.4f + 0.5f);
            uv[outer] = new Vector2(x / 2.8f + 0.5f, z / 2.4f + 0.5f);
            uv[lower] = uv[outer];

            int next = (i + 1) % segments;
            int nextInner = 1 + next;
            int nextOuter = 1 + segments + next;
            int nextLower = 1 + segments * 2 + next;
            int t = i * 15;
            // Raised top surface.
            triangles[t] = 0; triangles[t + 1] = nextInner; triangles[t + 2] = inner;
            triangles[t + 3] = inner; triangles[t + 4] = nextInner; triangles[t + 5] = nextOuter;
            triangles[t + 6] = inner; triangles[t + 7] = nextOuter; triangles[t + 8] = outer;
            // Rounded-looking outer thickness.
            triangles[t + 9] = outer; triangles[t + 10] = nextOuter; triangles[t + 11] = nextLower;
            triangles[t + 12] = outer; triangles[t + 13] = nextLower; triangles[t + 14] = lower;
        }

        var mesh = new Mesh { name = "PocaDeOleo_Irregular" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


    void Update()
    {
        if (Time.time >= destroyAt)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        TrySlip(other);
    }

    void OnTriggerStay(Collider other)
    {
        TrySlip(other);
    }

    void TrySlip(Collider other)
    {
        var car = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<BumperCarMotor>() : null;
        if (!car || !car.IsAlive) return;
        if (car == owner && Time.time < safeUntil) return;
        if (!affectedDrivers.Add(car.DriverIndex)) return;
        car.ApplyOilSlip(transform.position);
    }
}
