using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class Bouyancy : MonoBehaviour
{
    public GameObject WaterInteractionObject;
    public GameObject waterObject;
    public float underWaterDrag = 3f;
    public float underWaterAngularDrag = 1f;

    public float waterHeight = 0f;
    public float airDrag = 0f;
    public float airAngularDrag = 0.05f;

    public float floatingPower = 15f;

    Rigidbody m_rigidbody;

    bool underwater;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        WaterInteractionCheck();
        float diff = transform.position.y - waterHeight;
        if (diff < 0)
        {
            m_rigidbody.AddForceAtPosition(Vector3.up * floatingPower * Mathf.Abs(diff), transform.position, ForceMode.Force);
            if (underwater == false)
            {
                
                underwater = true;
                SwitchState(true);
            }

        }
        else if (underwater == true)
        {
            
            underwater = false;
            SwitchState(false);
        }

    

    }

    void WaterInteractionCheck()
    {
        waterObject = WaterInteractionObject.GetComponent<FindObjectByTagScript>().nearestObject;
        waterHeight = waterObject.transform.position.y;
    }

    void SwitchState(bool isUnderwater)
    {
        if (isUnderwater)
        {
            m_rigidbody.linearDamping = underWaterDrag;
            m_rigidbody.angularDamping = underWaterAngularDrag;
        }
        else
        {
            m_rigidbody.linearDamping = airDrag;
            m_rigidbody.angularDamping = airAngularDrag;
        }
    }
}
