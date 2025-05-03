using UnityEngine;

public class HeadlightController : MonoBehaviour
{
    public bool isLightON;
    [SerializeField] GameObject FrontLeftLightReflect;
    [SerializeField] GameObject FrontRightLightReflect;
    [SerializeField] GameObject FrontLeftLightSpot;
    [SerializeField] GameObject FrontRightLightSpot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void LateUpdate()
    {
        /*
        if (Input.GetKey("e"))
        {
            if (isLightON == true)
            {
                turnOnHeadlights();
            }
            else
            {
                turnOffHeadlights();
            }
        }
        */
    }

    public void turnOnHeadlights()
    {
        FrontLeftLightReflect.SetActive(true);
        FrontRightLightReflect.SetActive(true);
        FrontLeftLightSpot.SetActive(true);
        FrontRightLightSpot.SetActive(true);
    }
    public void turnOffHeadlights()
    {
        FrontLeftLightReflect.SetActive(false);
        FrontRightLightReflect.SetActive(false);
        FrontLeftLightSpot.SetActive(false);
        FrontRightLightSpot.SetActive(false);
    }
}
