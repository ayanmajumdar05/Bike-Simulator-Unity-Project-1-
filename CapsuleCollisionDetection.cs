using UnityEngine;

public class CapsuleCollisionDetection : MonoBehaviour
{
    public bool isColliding;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider CapsuleCollider)
    {
        isColliding = true;
    }
    void OnTriggerExit(Collider CapsuleCollider)
    {
        isColliding = false;
    }
}
