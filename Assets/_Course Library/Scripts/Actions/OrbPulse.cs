using UnityEngine;

public class OrbPulse : MonoBehaviour
{
    [Header("References")]
    public Renderer orbRenderer;   // The sphere's mesh renderer
    public Light auraLight;        // The point light

    [Header("Pulse Settings")]
    public float pulseSpeed = 1.5f;   // How fast it pulses
    public float minIntensity = 1f;   // Minimum light brightness
    public float maxIntensity = 2f;   // Maximum light brightness
    public float minEmission = 0.3f;  // Minimum glow
    public float maxEmission = 1.5f;  // Maximum glow

    private Material orbMat;
    private Color baseEmission;

    void Start()
    {
        orbMat = orbRenderer.material;
        baseEmission = orbMat.GetColor("_EmissionColor");
    }

    void Update()
    {
        // Calculate a smooth wave value between 0 and 1
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        // Adjust emission brightness
        Color newEmission = baseEmission * Mathf.Lerp(minEmission, maxEmission, t);
        orbMat.SetColor("_EmissionColor", newEmission);

        // Adjust light brightness too
        if (auraLight != null)
            auraLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
