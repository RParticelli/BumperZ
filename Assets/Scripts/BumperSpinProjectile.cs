using UnityEngine;

public sealed class BumperSpinProjectile : MonoBehaviour
{
    BumperCarMotor owner;
    Vector3 direction;
    float bornAt;
    const float Speed = 13.5f;
    const float Radius = 0.17f;

    public static BumperSpinProjectile Create(Vector3 position, Vector3 direction, BumperCarMotor owner)
    {
        var shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shot.name = "TiroDeGiro";
        Destroy(shot.GetComponent<SphereCollider>());
        shot.transform.position = position;
        shot.transform.localScale = Vector3.one * 0.30f;

        var material = new Material(Shader.Find("Standard"));
        material.color = new Color(1f, 0.22f, 0.04f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.08f, 0.01f) * 2.4f);
        shot.GetComponent<Renderer>().material = material;

        var trail = shot.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.startWidth = 0.18f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.04f;
        var trailMaterial = new Material(Shader.Find("Unlit/Color"));
        trailMaterial.color = new Color(1f, 0.18f, 0.02f, 0.8f);
        trail.material = trailMaterial;

        var projectile = shot.AddComponent<BumperSpinProjectile>();
        projectile.owner = owner;
        projectile.direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        projectile.bornAt = Time.time;
        return projectile;
    }

    void Update()
    {
        float distance = Speed * Time.deltaTime;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, Radius, direction, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            var car = hit.rigidbody ? hit.rigidbody.GetComponent<BumperCarMotor>() : null;
            if (car == owner) continue;
            if (car && car.IsAlive)
            {
                if (car.IsShieldActive)
                    car.BlockSpinShot();
                else
                    car.ApplySpinShot(direction, owner);
                Destroy(gameObject);
                return;
            }
            if (!car)
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position += direction * distance;
        transform.Rotate(360f * Time.deltaTime, 540f * Time.deltaTime, 0f);
        if (Time.time - bornAt > 2.2f)
            Destroy(gameObject);
    }
}
