using UnityEngine;
using UnityEngine.UIElements;

public class FindObjectByTagScript : MonoBehaviour
{
    public GameObject waterInteractionObject;
    public GameObject targetObject;
    public GameObject nearestObject;
    public float distanceToNearest;
    public float distanceToCheck;
    public float distanceToEnableScript;
    public bool enableScript;
    public GameObject[] allObjectsWithTag;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        allObjectsWithTag = GameObject.FindGameObjectsWithTag("Water Surface");
    }


    void LateUpdate()
    {
        //Assume first object is the closest
        nearestObject = allObjectsWithTag[0];
        distanceToNearest = Vector3.Distance(targetObject.transform.position, nearestObject.transform.position);

        //Traverse list of objects to find the nearest object
        for (int i = 0; i < allObjectsWithTag.Length; i++)
        {
            float distanceToCurrent = Vector3.Distance(targetObject.transform.position, allObjectsWithTag[i].transform.position);

            if (distanceToCurrent < distanceToNearest)
            {
                nearestObject = allObjectsWithTag[i];
                distanceToNearest = distanceToCurrent;
            }
        }

        Vector3 currentNearestObjectScale = nearestObject.transform.localScale;
        /*
        float objectScaleX = currentNearestObjectScale.x;
        float objectScaleY = currentNearestObjectScale.y;
        float objectScaleZ = currentNearestObjectScale.z;
        */
        float MaxScaleDistance = Mathf.Max(Mathf.Max(currentNearestObjectScale.x, currentNearestObjectScale.y), currentNearestObjectScale.z);
        distanceToCheck = distanceToNearest - MaxScaleDistance;

        if (distanceToCheck < distanceToEnableScript)
        {
            enableScript = true;


        }
        else
        {
            enableScript = false;
        }

    }
}