using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("---Camera Pan---")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float smoothTime = 0.15f;

    [Header("---Camera Zoom---")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoomDistance = 3f;
    [SerializeField] private float maxZoomDistance = 20f;

    [Header("---Camera Orbit---")]
    [SerializeField] private float orbitSpeed = 5f;
    [SerializeField] private float minRot = 20f;
    [SerializeField] private float maxRot = 90f;

    [SerializeField] GameObject floorAim;
    [SerializeField] GameObject cam;

    private Vector3 velocity;
    private float horizonRot;
    private float verticalRot;

    private float dist;

    private void Start()
    {
        Vector3 angles = cam.transform.eulerAngles;
        horizonRot = angles.y;
        verticalRot = angles.x;

        dist = Vector3.Distance(cam.transform.position, floorAim.transform.position);
        Vector3 offset = Quaternion.Euler(verticalRot, horizonRot, 0f) * Vector3.back * dist;

        cam.transform.position = floorAim.transform.position + offset;
        cam.transform.LookAt(floorAim.transform);
    }

    private void Update()
    {
        Vector3 inputDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) inputDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) inputDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) inputDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) inputDir.x = +1f;

        Vector3 targetPos = transform.position + (transform.forward * inputDir.z + transform.right * inputDir.x) * moveSpeed;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 zoomLine = (cam.transform.position - floorAim.transform.position).normalized;
            float currentDistance = Vector3.Distance(cam.transform.position, floorAim.transform.position);
            float targetDistance = currentDistance - scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
            float newDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minZoomDistance, maxZoomDistance);

            cam.transform.position = floorAim.transform.position + zoomLine * newDistance;
        }

        if (Input.GetKey(KeyCode.Mouse2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            horizonRot += mouseX * orbitSpeed * Time.deltaTime;
            verticalRot -= mouseY * orbitSpeed * Time.deltaTime;

            verticalRot = Mathf.Clamp(verticalRot, minRot, maxRot);

            dist = Vector3.Distance(cam.transform.position, floorAim.transform.position);
            Vector3 offset = Quaternion.Euler(verticalRot, horizonRot, 0f) * Vector3.back * dist;

            cam.transform.position = floorAim.transform.position + offset;
            cam.transform.LookAt(floorAim.transform);
        }
    }
}