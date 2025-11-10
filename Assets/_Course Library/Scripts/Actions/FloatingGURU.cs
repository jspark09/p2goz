using UnityEngine;

public class FloatingOrb : MonoBehaviour
{
    public float amplitude = 0.15f;  // how high it floats
    public float frequency = 1f;     // speed of bobbing
    private Vector3 startPos;

    void Start()
    {
        // remember where the orb starts
        startPos = transform.position;
    }

    void Update()
    {
        // simple up-and-down sine wave motion
        float y = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = startPos + new Vector3(0, y, 0);
    }
}
