using UnityEngine;

public class RedLineFollow : MonoBehaviour
{
    public Transform target;
    public float followSpeed = 10f;
    public bool followBothAxis = false;


   void Update()
{
    if (target == null) return;

    Vector3 targetPos = target.position;
    Vector3 newPos = Vector3.Lerp(transform.position, target.position, followSpeed * Time.deltaTime);

    if (followBothAxis)
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
    else
        transform.position = new Vector3(newPos.x, transform.position.y, transform.position.z);
}
}
