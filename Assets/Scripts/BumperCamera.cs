using UnityEngine;

public sealed class BumperCamera : MonoBehaviour
{
    public Transform Target;

    void LateUpdate()
    {
        // Close diagonal framing: preserves visible car bodies and fits the
        // complete outer ring with a short knockout margin on every side.
        transform.position = new Vector3(0f, 12f, -12f);
        transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }
}
