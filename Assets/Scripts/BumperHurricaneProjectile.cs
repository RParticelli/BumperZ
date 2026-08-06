using UnityEngine;

public sealed class BumperHurricaneProjectile : MonoBehaviour
{
    BumperCarMotor owner;
    Vector3 direction;
    float bornAt;
    Transform funnel;
    const float Speed = 9.5f;

    public static BumperHurricaneProjectile Create(Vector3 position, Vector3 direction, BumperCarMotor owner)
    {
        var root = new GameObject("ProjetilFuracao");
        root.transform.position = position;
        var projectile = root.AddComponent<BumperHurricaneProjectile>();
        projectile.owner = owner;
        projectile.direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        projectile.bornAt = Time.time;
        projectile.funnel = BumperHurricaneVisual.BuildFunnel(root.transform, 0.72f);
        return projectile;
    }

    void Update()
    {
        float distance = Speed * Time.deltaTime;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.28f, direction, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            var car = hit.rigidbody ? hit.rigidbody.GetComponent<BumperCarMotor>() : null;
            if (car == owner) continue;
            if (car && car.IsAlive)
            {
                car.ApplyHurricane(car.transform.position, direction);
                Destroy(gameObject);
                return;
            }
            if (!car) { Destroy(gameObject); return; }
        }
        transform.position += direction * distance;
        if (funnel) funnel.Rotate(0f, 620f * Time.deltaTime, 0f, Space.Self);
        if (Time.time - bornAt > 2.5f) Destroy(gameObject);
    }
}
