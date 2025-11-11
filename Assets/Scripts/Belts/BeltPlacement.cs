using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class BeltPlacement : MonoBehaviour
{

    [System.Serializable]
    public class ConveyorGroup
    {
        public Transform conveyorGroupTransform;
        public BoxCollider collider;
        public Vector3[] conveyorsPos = { };
        public string beltGroupId;

    }

    [SerializeField] public List<ConveyorGroup> conveyorGroups;
    public Transform BeltGroupPrefab;

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
    public string currentId = "";

    public ChangeLayer layers;
    private int currentLayer = 0;
    private bool hasChangedLayers = false;
    private int lastLayer = 0;


    public Vector3[] currentBeltPositions = { };
    public Vector3[] currentBeltGroupPositions = { };
    public Vector3[] overlappingPositions = { };
    public string[] overlappedGroups = { };

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
                    currentId = GetId(currentBeltPositions);
                    CheckIfEnteredOtherGroup();
                    overlappingPositions = new Vector3[0];
                    for (int i = 0; i < conveyorGroups.Count; i++)
                        if (overlappedGroups.Contains(conveyorGroups[i].beltGroupId))
                            overlappingPositions = AppendArray(overlappingPositions, CheckForOverlap(currentBeltPositions, conveyorGroups[i].conveyorsPos));
                }
                else if (hasChangedLayers && currentBeltPositions.Length != 0)
                {
                    ChangeAnchorPoint();
                    SaveConveyorsPosition();
                    layers.hasChangedLayer = false;
                }
            }
            else if (isReleasingMouse0)
            {
                CreateConveyorBeltGroupClassItem(newConveyorGroup);
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

    private void CreateConveyorBeltGroupClassItem(ConveyorGroup newConveyorGroup)
    {
        SaveConveyorsPosition();
        newConveyorGroup.conveyorsPos = new Vector3[currentBeltGroupPositions.Length];
        for (int i = 0; i < currentBeltGroupPositions.Length; i++)
            newConveyorGroup.conveyorsPos[i] = currentBeltGroupPositions[i];

        newConveyorGroup.beltGroupId = GetId(currentBeltGroupPositions) + "-" + GenerateRandomString();

        Vector3 minCorner = GetMin(newConveyorGroup.conveyorsPos);
        Vector3 maxCorner = GetMax(newConveyorGroup.conveyorsPos);
        Vector3 middle = Vector3.Lerp(minCorner, maxCorner, 0.5f);

        newConveyorGroup.conveyorGroupTransform = Instantiate(BeltGroupPrefab, middle + new Vector3(0f, 0.5f, 0f), quaternion.identity, gameObject.transform);
        newConveyorGroup.conveyorGroupTransform.gameObject.name = newConveyorGroup.beltGroupId;

        newConveyorGroup.collider = newConveyorGroup.conveyorGroupTransform.gameObject.GetComponent<BoxCollider>();
        newConveyorGroup.collider.size = new Vector3(maxCorner.x - minCorner.x + 1, maxCorner.y - minCorner.y + 1, maxCorner.z - minCorner.z + 1);

        conveyorGroups.Add(newConveyorGroup);

        startedBeltGroup = false;
        currentBeltPositions = new Vector3[0];
        currentBeltGroupPositions = new Vector3[0];
        overlappingPositions = new Vector3[0];
        overlappedGroups = new string[0];
    }

    private string[] CheckIfEnteredOtherGroup()
    {
        overlappedGroups = new string[0];
        for (int i = 0; i < currentBeltPositions.Length; i++)
        {
            Ray ray = new Ray(currentBeltPositions[i] + (Vector3.up * 4f), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 5f))
                if (raycastHit.collider.CompareTag("BeltGroup") && !overlappedGroups.Contains(raycastHit.transform.name))
                    overlappedGroups = Append(overlappedGroups, raycastHit.transform.name);
        }
        return overlappedGroups;
    }

    //private Vector3[] CheckForOverlap()
    //{
    //    int amountOfOverlap = 0;
    //    Vector3[] overlappingPos = { };

    //    for (int i = 0; i < currentBeltPositions.Length; i++)
    //        for (int j = 0; j < currentBeltGroupPositions.Length; j++)
    //            if (currentBeltPositions[i] == currentBeltGroupPositions[j])
    //                overlappingPos = Append(overlappingPos, Vector3.up);
    //    return overlappingPos;
    //}

    private Vector3[] CheckForOverlap(Vector3[] array1, Vector3[] array2)
    {
        Vector3[] intersections = { };
        for (int i = 0; i < array1.Length; i++)
            if (array2.Contains(array1[i]))
                intersections = Append(intersections, array1[i]);
        return intersections;
    }

    private Vector3 GetMin(Vector3[] array)
    {
        float minX = array[0].x;
        float minY = array[0].y;
        float minZ = array[0].z;

        for (int i = 0; i < array.Length; i++)
        {
            if (minX > array[i].x)
                minX = array[i].x;
            if (minY > array[i].y)
                minY = array[i].y;
            if (minZ > array[i].z)
                minZ = array[i].z;
        }
        return new Vector3(minX, minY, minZ);
    }

    private Vector3 GetMax(Vector3[] array)
    {
        float maxX = array[0].x;
        float maxY = array[0].y;
        float maxZ = array[0].z;

        for (int i = 0; i < array.Length; i++)
        {
            if (maxX < array[i].x)
                maxX = array[i].x;
            if (maxY < array[i].y)
                maxY = array[i].y;
            if (maxZ < array[i].z)
                maxZ = array[i].z;
        }
        return new Vector3(maxX, maxY, maxZ);
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
    private Vector3[] AppendArray(Vector3[] array, Vector3[] arrayToAppend)
    {
        Vector3[] newArray = new Vector3[array.Length + arrayToAppend.Length];
        for (int i = 0; i < array.Length; i++)
            newArray[i] = array[i];
        for (int i = 0; i < arrayToAppend.Length; i++)
            newArray[array.Length + i] = arrayToAppend[i];
        return newArray;
    }
    private string[] Append(string[] array, string stringToAppend)
    {
        string[] newArray = new string[array.Length + 1];
        for (int i = 0; i < array.Length; i++)
            newArray[i] = array[i];
        newArray[newArray.Length - 1] = stringToAppend;
        return newArray;
    }

    private string GetId(Vector3[] posArray)
    {
        if (posArray.Length > 1)
        {
            int amoutOfRepetitions = 1;
            char typeOfBelt;
            Vector3 dir = posArray[1] - posArray[0];
            Vector3 lastDir = dir;
            string id = "";
            for (int i = 1; i < posArray.Length - 1; i++)
            {
                dir = posArray[i + 1] - posArray[i];
                amoutOfRepetitions++;
                if (dir != lastDir)
                {
                    typeOfBelt = GetTypeOfBelt(lastDir);
                    id += amoutOfRepetitions.ToString();
                    id += typeOfBelt;
                    amoutOfRepetitions = 1;
                }
                lastDir = dir;
            }
            amoutOfRepetitions++;
            typeOfBelt = GetTypeOfBelt(dir);
            id += amoutOfRepetitions.ToString();
            id += typeOfBelt;
            return id;
        }
        else
        {
            return "1F";
        }
    }

    private string GenerateRandomString()
    {
        int randomInt = UnityEngine.Random.Range(0, 9999);
        return randomInt.ToString();
    }

    private char GetTypeOfBelt(Vector3 dir)
    {
        char typeOfBelt = 'F';
        if (dir == new Vector3(0f, 0f, 1f))
            typeOfBelt = 'F';
        else if (dir == new Vector3(0f, 0f, -1f))
            typeOfBelt = 'B';
        else if (dir == new Vector3(1f, 0f, 0f))
            typeOfBelt = 'R';
        else if (dir == new Vector3(-1f, 0f, 0f))
            typeOfBelt = 'L';
        else if (dir == new Vector3(0f, 1f, 0f))
            typeOfBelt = 'U';
        else if (dir == new Vector3(0f, -1f, 0f))
            typeOfBelt = 'D';
        return typeOfBelt;
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
