using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private GameObject VerticalAim;

    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float rotateSensitivity = 2f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float fieldOfViewMax = 100f;
    [SerializeField] private float fieldOfViewMin = 20f;

    //[SerializeField] private Vector2 limitX = new Vector2(30f, 123f);
    //[SerializeField] private Vector2 limitZ = new Vector2(30f, 123f);

    float targetFieldOfView = 60f;

    private void Update()
    {
        Vector3 inputDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) inputDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) inputDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) inputDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) inputDir.x = +1f;

        Vector3 moveDir = transform.forward * inputDir.z + transform.right * inputDir.x;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        //Vector3 clampedPos = transform.position;
        //clampedPos.x = Mathf.Clamp(clampedPos.x, limitX.x, limitX.y);
        //clampedPos.z = Mathf.Clamp(clampedPos.z, limitZ.x, limitZ.y);
        //transform.position = clampedPos;

        float rotateDirX = 0f;
        float rotateDirY = 0f;

        if (Input.GetKey(KeyCode.Mouse2))
        {
            rotateDirX += Input.GetAxis("Mouse X") * rotateSensitivity;

            rotateDirY += Input.GetAxis("Mouse Y") * rotateSensitivity;
            Debug.Log(rotateDirY);
        }

        transform.Rotate(Vector3.up, rotateDirX * rotateSensitivity *  Time.deltaTime);
        VerticalAim.transform.Translate(0f, rotateDirY / 2 * rotateSensitivity * Time.deltaTime, 0f);

        HandleCameraZoom();
    }

    private void HandleCameraZoom()
    {
        if (Input.mouseScrollDelta.y < 0)
        {
            targetFieldOfView += zoomSpeed;
        }
        if (Input.mouseScrollDelta.y > 0)
        {
            targetFieldOfView -= zoomSpeed;
        }

        targetFieldOfView = Mathf.Clamp(targetFieldOfView, fieldOfViewMin, fieldOfViewMax);
        virtualCamera.m_Lens.FieldOfView = targetFieldOfView;
    }

}
