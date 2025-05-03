using UnityEngine;
using UnityEngine.UI;

public class GForceScript : MonoBehaviour
{
    public Rigidbody rb;
    public RectTransform gForceDot;      // For UI representation
    public Text gForceText;
    //public Text speedText;

    private Vector3 lastVelocity;
    public float gScale = 60f;

    // Optional: for screen shake or camera feedback
    public Vector3 currentGForce;       // Public so other scripts can access
    public Vector3 gForceChange;
    public float suddenChangeThreshold = 1.0f;
    public float GforceThreshold = 1.0f;
    public bool isHIGHGForce = false;
    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        lastVelocity = rb.linearVelocity;
    }

    void FixedUpdate()
    {
        // Acceleration = Δv / Δt
        Vector3 acceleration = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = rb.linearVelocity;

        // Convert to local space (relative to vehicle)
        Vector3 localAccel = transform.InverseTransformDirection(acceleration);
        Vector3 TEMPcurrentGForce = localAccel / 9.81f; // in g units
        currentGForce = new Vector3((Mathf.Round(TEMPcurrentGForce.x * 100f) / 100f), (Mathf.Round(TEMPcurrentGForce.y * 100f) / 100f), (Mathf.Round(TEMPcurrentGForce.z * 100f) / 100f));

        /*
        Vector3 newGForce = new Vector3(
                    Mathf.Round(TEMPcurrentGForce.x * 100f) / 100f,
                    Mathf.Round(TEMPcurrentGForce.y * 100f) / 100f,
                    Mathf.Round(TEMPcurrentGForce.z * 100f) / 100f
        );
        */
        Vector3 newGForce = new Vector3(
        TEMPcurrentGForce.x,
        TEMPcurrentGForce.y,
        TEMPcurrentGForce.z
        );
        // Calculate the change in G-force
        gForceChange = newGForce - currentGForce;

        // Detect sudden change
        if (gForceChange.magnitude > suddenChangeThreshold)
        {
            Debug.Log("Sudden G-force change detected: " + gForceChange);
        }

        // Update current G-force
        currentGForce = newGForce;



        float gForceMagnitude = currentGForce.magnitude;
        if (newGForce.magnitude > GforceThreshold)
        {
            Debug.Log("HIGH G FORCE");
            isHIGHGForce = true;
        }
        else
        {
            isHIGHGForce = false;
        }
        // UI shows only horizontal g-force direction (X = lateral, Z = longitudinal)
        Vector2 gVector = new Vector2(currentGForce.x, currentGForce.z);
        gVector = Vector2.ClampMagnitude(gVector, 3f);

        // Update UI
        if (gForceDot != null)
        {
            gForceDot.anchoredPosition = gVector * -gScale;
        }

        if (gForceText != null)
            gForceText.text = gForceMagnitude.ToString("0.00") + "G";


    }
}
