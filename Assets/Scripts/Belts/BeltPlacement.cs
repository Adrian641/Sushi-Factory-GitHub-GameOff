using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class BeltPlacement : MonoBehaviour
{

    [System.Serializable]
    public class ConveyorGroup
    {
        public Vector3[] conveyorsPos = { };
        public string beltGroupId;
    }

    [SerializeField] public List<ConveyorGroup> conveyorGroups;

    private static bool isHoldingMouse0 = false;
    private static bool isStartClickingMouse0 = false;
    private static bool isReleasingMouse0 = false;
    private static bool toggleFlip = false;
    private static bool justPressedF = false;
    private static bool isHoldingMouse1 = false;

    private static bool startedBeltGroup = false;

    [SerializeField] public Camera mainCam;

    private const int GRID_SIZE_X = 20;
    private const int GRID_SIZE_Y = 3;
    private const int GRID_SIZE_Z = 20;

    private static RaycastHit RayHit;
    private static Ray ray;
    private Vector2 hitPoint = Vector2.zero;
    private static Vector2 lastHitPoint = Vector2.zero;
    private Vector3 startPos;

    public ChangeLayer layers;
    private int currentLayer = 0;
    private bool hasChangedLayers = false;
    private int lastLayer = 0;


    public Vector3[] currentBeltPositions = { };
    public Vector3[] currentBeltGroupPositions = { };
    public Vector3[] overlappingPositions;

    void Update()
    {
        currentLayer = layers.currentLayer;
        hasChangedLayers = layers.hasChangedLayer;
        CheckUsersInputs();

        if (isStartClickingMouse0)
        {
            if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit RayHit))
                startPos = new Vector3(MathF.Round(RayHit.point.x), currentLayer, MathF.Round(RayHit.point.z));
            startedBeltGroup = true;
        }
        if (startedBeltGroup)
        {
            ConveyorGroup newConveyorGroup = new ConveyorGroup();
            if (isHoldingMouse0)
            {
                if (CheckForChangeOfPos() && !hasChangedLayers)
                {
                    GetBeltPositions();
                    overlappingPositions = CheckForOverlap();
                }
                else if (hasChangedLayers && currentBeltPositions.Length != 0)
                {
                    ChangeAnchorPoint();
                    SaveConveyorsPosition();
                    overlappingPositions = CheckForOverlap();
                    layers.hasChangedLayer = false;
                }
            }
            else if (isReleasingMouse0)
            {
                SaveConveyorsPosition();
                newConveyorGroup.conveyorsPos = new Vector3[currentBeltGroupPositions.Length];
                for (int i = 0; i < currentBeltGroupPositions.Length; i++)
                    newConveyorGroup.conveyorsPos[i] = currentBeltGroupPositions[i];
                conveyorGroups.Add(newConveyorGroup);
                startedBeltGroup = false;
                currentBeltPositions = new Vector3[0];
                currentBeltGroupPositions = new Vector3[0];
            }
        }
    }

    private void GetBeltPositions()
    {
        lastHitPoint = hitPoint;
        int distanceX = (int)math.abs(lastHitPoint.x - startPos.x);
        int distanceY = (int)math.abs(lastHitPoint.y - startPos.z);
        Vector2 dir = Vector2.one;
        if (startPos.x > lastHitPoint.x)
            dir.x = -1f;
        if (startPos.z > lastHitPoint.y)
            dir.y = -1f;

        currentBeltPositions = new Vector3[distanceX + distanceY + 1];
        currentBeltPositions[0] = new Vector3(startPos.x, currentLayer, startPos.z);
        if (!toggleFlip)
        {
            for (int i = 1; i <= distanceX; i++)
                currentBeltPositions[i] = new Vector3(startPos.x + (i * dir.x), currentLayer, startPos.z);
            for (int i = 1; i <= distanceY; i++)
                currentBeltPositions[i + distanceX] = new Vector3(currentBeltPositions[distanceX].x, currentLayer, startPos.z + (i * dir.y));
        }
        else
        {
            for (int i = 1; i <= distanceY; i++)
                currentBeltPositions[i] = new Vector3(startPos.x, currentLayer, startPos.z + (i * dir.y));
            for (int i = 1; i <= distanceX; i++)
                currentBeltPositions[i + distanceY] = new Vector3(startPos.x + (i * dir.x), currentLayer, currentBeltPositions[distanceY].z);
        }
    }

    private void ChangeAnchorPoint()
    {
        startPos = new Vector3(currentBeltPositions[currentBeltPositions.Length - 1].x, currentLayer, currentBeltPositions[currentBeltPositions.Length - 1].z);
    }

    private Vector3[] CheckForOverlap() // Might be working idk ill check when I have visual support
    {
        int amountOfOverlap = 0;
        Vector3[] overlappingPos = { };

        for (int i = 0; i < currentBeltPositions.Length; i++)
            for (int j = 0; j < currentBeltGroupPositions.Length; j++)
                if (currentBeltPositions[i] == currentBeltGroupPositions[j])
                    overlappingPos = Append(overlappingPos, Vector3.up);
        return overlappingPos;
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

    private void SaveConveyorsPosition()
    {
        Vector3[] newArray = new Vector3[currentBeltGroupPositions.Length + currentBeltPositions.Length];
        for (int i = 0; i < currentBeltGroupPositions.Length; i++)
            newArray[i] = currentBeltGroupPositions[i];
        for (int i = 0; currentBeltPositions.Length > i; i++)
            newArray[newArray.Length - currentBeltPositions.Length + i] = currentBeltPositions[i];

        currentBeltPositions = new Vector3[0];
        currentBeltGroupPositions = newArray;
    }

    private Vector3[] Append(Vector3[] array, Vector3 posToAppend)
    {
        Vector3[] newArray = new Vector3[array.Length + 1];
        for (int i = 0; i < array.Length; i++)
            newArray[i] = array[i];
        newArray[newArray.Length - 1] = posToAppend;
        return newArray;
    }

    private void CheckUsersInputs()
    {
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
