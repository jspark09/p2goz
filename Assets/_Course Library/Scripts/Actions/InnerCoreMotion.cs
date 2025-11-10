using UnityEngine;

public class InnerCoreMotion : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveRadius = 0.25f;      // How far it can reach (more than before)
    public float moveSpeed = 1.8f;        // Speed of turbulence
    public float turbulence = 3.0f;       // Random intensity
    public float smoothness = 0.15f;      // How smoothly it changes direction
    public float scaleVariation = 0.1f;   // Size pulsing amount
    public float rotationSpeed = 60f;     // Speed of internal spin

    private Vector3 targetPos;
    private Vector3 velocity;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
        SetNewTarget();
    }

    void Update()
    {
        // Smoothly move toward target
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPos, ref velocity, smoothness);

        // Randomly change direction
        if (Vector3.Distance(transform.localPosition, targetPos) < 0.05f)
            SetNewTarget();

        // Add spin for fluid energy feel
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        transform.Rotate(Vector3.right, rotationSpeed * 0.5f * Time.deltaTime, Space.Self);

        // Add pulsating scale (as if energy is compressing/expanding)
        float s = 1f + Mathf.Sin(Time.time * moveSpeed * 3f) * scaleVariation;
        transform.localScale = Vector3.one * s;
    }

    void SetNewTarget()
    {
        // Random point within a sphere of moveRadius
        Vector3 randomDir = Random.onUnitSphere * moveRadius;
        targetPos = startPos + randomDir;
    }
}
