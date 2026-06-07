using UnityEngine;
public class FollowBoss : MonoBehaviour
{
    public Transform boss;

    void Update()
    {
        if (boss != null)
            transform.position = boss.position;
    }
}
