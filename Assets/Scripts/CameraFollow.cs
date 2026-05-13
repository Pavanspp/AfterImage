using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 pos = transform.position;
        pos.x = target.position.x;
        pos.y = target.position.y;
        transform.position = pos;
    }
}