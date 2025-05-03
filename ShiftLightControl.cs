using System.Collections;
using UnityEngine;

public class ShiftLightControl : MonoBehaviour
{
    public GameObject bike;
    public Material ShiftLightMaterial;
    public float RPMLimit = 10000f;
    public float RPM;
    public float shiftLightRPM = 8000f;
    public bool isShiftLightOn = false;
    public bool isShiftLightBlinking = false;
    public bool keepBlinking = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShiftLightMaterial = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RPM = bike.GetComponent<BikeControlRemake>().currentRPM;

        if (RPM > shiftLightRPM && RPM < RPMLimit - 450f)
        {
            /*
            if (isShiftLightOn == false)
            {

            }
            */
            TurnOnShiftLight();
            isShiftLightOn = true;
            isShiftLightBlinking = false;
            keepBlinking = false;
        }
        else if (RPM >= RPMLimit - 450f || RPM >= RPMLimit)
        {
            keepBlinking = true;
            if (isShiftLightBlinking == false)
            {
                StartCoroutine(ShiftLightBlink());
                isShiftLightBlinking = true;
            }
        }
        else
        {
            isShiftLightOn = false;
            TurnOffShiftLight();
            //StopAllCoroutines();
            /*
            if (isShiftLightBlinking == true)
            {
                StopCoroutine(ShiftLightBlink());
                isShiftLightBlinking = false;
            }
            */

            StopCoroutine(ShiftLightBlink());
            isShiftLightBlinking = false;
            keepBlinking = false;
            //StopCoroutine(ShiftLightBlink());
        }


    }
    void TurnOnShiftLight()
    {
        ShiftLightMaterial.EnableKeyword("_EMISSION");
        isShiftLightOn = true;
    }
    void TurnOffShiftLight()
    {
        ShiftLightMaterial.DisableKeyword("_EMISSION");
        isShiftLightOn = false;
    }
    /*
    public IEnumerator ShiftLightBlink()
    {
        float blinkDuration = 0.5f; // Duration of the blink in seconds
        float blinkInterval = 0.15f; // Interval between blinks in seconds
        float elapsedTime = 0f;

        while (elapsedTime < blinkDuration)
        {
            //bike.GetComponent<BikeControlRemake>().shiftLight.SetActive(true);
            ShiftLightMaterial.DisableKeyword("_EMISSION");
            yield return new WaitForSeconds(blinkInterval);
            //bike.GetComponent<BikeControlRemake>().shiftLight.SetActive(false);
            ShiftLightMaterial.EnableKeyword("_EMISSION");
            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval * 10; // Account for both on and off time
        }
    }
    */
    public IEnumerator ShiftLightBlink()
    {
        //float blinkDuration = 0.5f; // Duration of the blink in seconds
        float blinkInterval = 0.15f; // Interval between blinks in seconds
        //float elapsedTime = 0f;
        while (keepBlinking == true)
        {
            ShiftLightMaterial.EnableKeyword("_EMISSION");
            yield return new WaitForSeconds(blinkInterval);
            ShiftLightMaterial.DisableKeyword("_EMISSION");
            yield return new WaitForSeconds(blinkInterval);

        }

    }
}
