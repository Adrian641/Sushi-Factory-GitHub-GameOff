using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BeltPlacement : MonoBehaviour
{

    [System.Serializable]
    public class ConveyorGroup
    {
        public Vector3[] conveyorsPos;
        public string beltGroupId;
    }

    [SerializeField] public List<ConveyorGroup> conveyorGroups;

    private static bool isHoldingLeftShift = false;
    private static bool isHoldingMouse0 = false;
    private static bool isStartClickingMouse0 = false;
    private static bool isReleasingMouse0 = false;
    private static bool toggleFlip = false;
    private static bool justPressedF = false;
    private static bool isHoldingMouse1 = false;

    [SerializeField] public Camera mainCam;

    private static RaycastHit RayHit;
    private static Ray ray;
    private static Vector2 hitPoint = Vector2.zero;
    public static Vector2 lastHitPoint = Vector2.zero;
    public static Vector2 startPos;

    private int currentLayer = 0;

    private Vector3[] currentBeltPositions = { };

    void Update()
    {
        CheckUsersInputs();
        if (isStartClickingMouse0)
        {
            if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit RayHit))
                startPos = new Vector2(MathF.Round(RayHit.point.x), MathF.Round(RayHit.point.z));
        }
        else if (isHoldingMouse0)
        {
            if (CheckForChangeOfPos())
            {
                GetBeltPositions();
            }
        }
        else if (isReleasingMouse0)
        {
            ConveyorGroup newConveyorGroup = new ConveyorGroup();
            newConveyorGroup.conveyorsPos = new Vector3[currentBeltPositions.Length];
            for (int i = 0; i < currentBeltPositions.Length; i++)
                newConveyorGroup.conveyorsPos[i] = currentBeltPositions[i];
            conveyorGroups.Add(newConveyorGroup);
        }
    }

    private void GetBeltPositions()
    {
        lastHitPoint = hitPoint;
        int distanceX = (int)math.abs(lastHitPoint.x - startPos.x);
        int distanceY = (int)math.abs(lastHitPoint.y - startPos.y);
        Vector2 dir = Vector2.one;
        if (startPos.x > lastHitPoint.x)
            dir.x = -1f;
        if (startPos.y > lastHitPoint.y)
            dir.y = -1f;

        currentBeltPositions = new Vector3[distanceX + distanceY + 1];
        currentBeltPositions[0] = new Vector3(startPos.x, currentLayer, startPos.y);
        if (!toggleFlip)
        {
            for (int i = 1; i <= distanceX; i++)
                currentBeltPositions[i] = new Vector3(startPos.x + (i * dir.x), currentLayer, startPos.y);
            for (int i = 1; i <= distanceY; i++)
                currentBeltPositions[i + distanceX] = new Vector3(currentBeltPositions[distanceX].x, currentLayer, startPos.y + (i * dir.y));
        }
        else
        {
            for (int i = 1; i <= distanceY; i++)
                currentBeltPositions[i] = new Vector3(startPos.x, currentLayer, startPos.y + (i * dir.y));
            for (int i = 1; i <= distanceX; i++)
                currentBeltPositions[i + distanceY] = new Vector3(startPos.x + (i * dir.x), currentLayer, currentBeltPositions[distanceY].z);
        }
    }


    private bool CheckForChangeOfPos()
    {
        ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RayHit))
        {
            hitPoint = new Vector2(MathF.Round(RayHit.point.x), MathF.Round(RayHit.point.z));
            if (hitPoint != lastHitPoint || justPressedF)
                return true;
        }
        return false;
    }

    private void CheckUsersInputs()
    {
        if (Input.GetKey(KeyCode.LeftShift))
            isHoldingLeftShift = true;
        else
            isHoldingLeftShift = false;
        if (Input.GetKey(KeyCode.Mouse0))
            isHoldingMouse0 = true;
        else
            isHoldingMouse0 = false;
        if (Input.GetKeyDown(KeyCode.Mouse0))
            isStartClickingMouse0 = true;
        else
            isStartClickingMouse0 = false;
        if (Input.GetKeyUp(KeyCode.Mouse0))
            isReleasingMouse0 = true;
        else
            isReleasingMouse0 = false;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!toggleFlip)
                toggleFlip = true;
            else if (toggleFlip)
                toggleFlip = false;
        }

        if (Input.GetKeyDown(KeyCode.F))
            justPressedF = true;
        else
            justPressedF = false;
        if (Input.GetKey(KeyCode.Mouse1))
            isHoldingMouse1 = true;
        else
            isHoldingMouse1 = false;
    }
}
