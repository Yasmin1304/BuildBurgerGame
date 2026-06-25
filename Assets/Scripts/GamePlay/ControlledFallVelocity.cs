using UnityEngine;

/// <summary>
/// Keeps spawned items moving downward at the configured level speed.
/// Prefabs use Rigidbody drag, so setting velocity only once can slow them down.
/// </summary>
public class ControlledFallVelocity : MonoBehaviour
{
    private Rigidbody cachedRigidbody;
    private float fallSpeed = 2.5f;

    public void Configure(float speed)
    {
        fallSpeed = Mathf.Max(0f, speed);

        if (cachedRigidbody == null)
            cachedRigidbody = GetComponent<Rigidbody>();

        ApplyVelocity();
    }

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        if (cachedRigidbody == null || cachedRigidbody.isKinematic)
            return;

        cachedRigidbody.useGravity = false;
        cachedRigidbody.linearVelocity = Vector3.down * fallSpeed;
    }
}
