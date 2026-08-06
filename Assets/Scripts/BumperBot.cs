using UnityEngine;

public sealed class BumperBot : MonoBehaviour
{
    BumperCarMotor motor;
    float nextThink;
    BumperCarMotor target;

    void Awake() => motor = GetComponent<BumperCarMotor>();

    void Update()
    {
        if (!motor || !motor.IsAlive) return;
        if (Time.time >= nextThink)
        {
            nextThink = Time.time + Random.Range(0.18f, 0.35f);
            target = BumperGame.Instance.ClosestOpponent(motor);
        }

        Vector3 position = transform.position;
        Vector3 desired;
        float radius = new Vector2(position.x, position.z).magnitude;
        if (radius > 7.05f)
            desired = -new Vector3(position.x, 0f, position.z).normalized;
        else if (target)
            desired = (target.transform.position - position).normalized;
        else
            desired = transform.forward;

        float signedAngle = Vector3.SignedAngle(transform.forward, desired, Vector3.up);
        float steer = Mathf.Clamp(signedAngle / 35f, -1f, 1f);
        float drive = Mathf.Abs(signedAngle) > 115f ? -0.45f : 1f;
        motor.SetInput(drive, steer);
        if (motor.HasPower && target && Mathf.Abs(signedAngle) < 13f &&
            Vector3.Distance(transform.position, target.transform.position) < 4.5f)
            motor.UsePower();
    }
}
