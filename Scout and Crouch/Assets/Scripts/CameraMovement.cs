using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] Transform playerTransform;

    private void Update() {
        transform.position = new Vector3(playerTransform.position.x, transform.position.y, transform.position.z);
        transform.LookAt(playerTransform.position);
    }
}
