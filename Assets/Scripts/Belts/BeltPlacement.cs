using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static BeltPlacement;
using static UnityEngine.UI.GridLayoutGroup;

public class BeltPlacement : MonoBehaviour
{

    [System.Serializable]
    public class ConveyorGroup
    {
        [HideInInspector] public Transform conveyorGroupTransform;
        [HideInInspector] public BoxCollider collider;
        public Vector3[] conveyorsPos = { };
        public string beltGroupId;
        [HideInInspector] public string groupAttachedTo = string.Empty;

        [System.Serializable]
        public class Splitter
        {
            public Vector3 splitterPos;
            [HideInInspector] public bool HasTwoOutputs;
            [HideInInspector] public Vector3 output1Dir;
            [HideInInspector] public Vector3 output2Dir;
            public string output1GroupId;
            public string output2GroupId;
            [HideInInspector] public bool isSetInStone;
        }

        [System.Serializable]
        public class Merger
        {
            public string groupMergeredIntoId;
            public Vector3 positionMergedInto;
        }
        public List<Splitter> splitters = new List<Splitter>();
        [HideInInspector] public Merger merger;
    }

    [SerializeField] private List<ConveyorGroup> conveyorGroups;
    public Transform BeltGroupPrefab;

    private static bool isHoldingMouse0 = false;
    private static bool isStartClickingMouse0 = false;
    private static bool isReleasingMouse0 = false;
    private static bool toggleFlip = false;
    private static bool justPressedF = false;
    private static bool isHoldingMouse1 = false;
    private static bool justPressedC = false;

    private static bool startedBeltGroup = false;

    [SerializeField] private Camera mainCam;

    private static RaycastHit RayHit;
    private static Ray ray;
    private Vector3 hitPoint;
    private Vector3 lastHitPoint;
    private Vector3 startPos;
    private string currentId = "";
    public char lastBeltType;

    public ChangeLayer layers;
    private int currentLayer = 0;
    private bool hasChangedLayers = false;
    private int lastLayer = 0;


    private Vector3[] currentBeltPositions = { };
    private Vector3[] currentBeltGroupPositions = { };
    private Vector3[] overlappingPositions = { };
    private string[] overlappedGroups = { };

    private ConveyorGroup.Splitter currentSplitter;
    private Vector3 lastSplitterPos = Vector3.negativeInfinity;
    private string groupAttachedTo = string.Empty;

    #region Belt prefabs and materials

    public GameObject straightBelt;
    public GameObject cornerBelt;
    public GameObject fullMerge;
    public GameObject leftMerger;
    public GameObject rightMerger;
    public GameObject cornerMerger;
    public GameObject leftSplitter;
    public GameObject rightSplitter;
    public GameObject cornerSplitter;
    public GameObject fullSplitter;
    public GameObject bottomVertical;
    public GameObject topVertical;
    public GameObject Vertical;

    public Material selectedMaterial;
    public Material overlappingMaterial;
    public Material normalMaterial;

    #endregion

    [HideInInspector] public List<GameObject> allSelectedBelts;
     public List<GameObject> currentSelectedBelts;

    void Update()
    {
        currentLayer = layers.currentLayer;
        hasChangedLayers = layers.hasChangedLayer;
        CheckUsersInputs();

        if (isStartClickingMouse0)
        {
            FindStartPosition();
            DrawSelectedBeltGroup("1F", startPos, ' ');
            startedBeltGroup = true;
        }
        else if (isHoldingMouse1)
        {
            if (CheckForChangeOfPos())
                DeleteConveyors();
        }
        if (startedBeltGroup)
        {
            bool madeFirstPath = false;
            ConveyorGroup newConveyorGroup = new ConveyorGroup();
            if (isHoldingMouse0)
            {
                if (justPressedC)
                {
                    lastBeltType = GetLastBeltType();
                    ChangeAnchorPoint();
                    SaveSelectedBelts(true);
                    SaveConveyorsPosition(true);
                    DrawSelectedBeltGroup("1" + char.ToString(lastBeltType), startPos, lastBeltType);
                    madeFirstPath = true;
                }
                else if (CheckForChangeOfPos() && !hasChangedLayers)
                {
                    GetBeltPositions();
                    currentId = GetId(currentBeltPositions, string.Empty, false);
                    CheckIfEnteredOtherGroup();
                    FindAllOverlappingGroup();
                    DeleteCurrentSelectedBelts();
                    if (!madeFirstPath)
                        CheckForSplitter(currentId);
                    DrawSelectedBeltGroup(currentId, startPos, lastBeltType);
                }
                else if (hasChangedLayers && currentBeltPositions.Length != 0)
                {
                    ChangeAnchorPoint();
                    SaveConveyorsPosition(false);
                    SaveSelectedBelts(false);
                    DrawSelectedBeltGroup(currentId, startPos, lastBeltType);
                    madeFirstPath = true;
                }
                layers.hasChangedLayer = false;
            }
            else if (isReleasingMouse0)
            {
                newConveyorGroup = CreateConveyorBeltGroupClassItem(newConveyorGroup);
                SaveSelectedBelts(false);
                DrawFinalBelts(newConveyorGroup.conveyorGroupTransform);
                DeleteAllSelectedBelts();
            }
        }
    }
    private void GetBeltPositions()
    {
        lastHitPoint = hitPoint;
        int distanceX = (int)math.abs(lastHitPoint.x - startPos.x);
        int distanceY = (int)math.abs(lastHitPoint.z - startPos.z);
        Vector2 dir = Vector2.one;
        if (startPos.x > lastHitPoint.x)
            dir.x = -1f;
        if (startPos.z > lastHitPoint.z)
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
    private void FindStartPosition()
    {
        if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit RayHit))
            startPos = new Vector3(MathF.Round(RayHit.point.x), currentLayer, MathF.Round(RayHit.point.z));
        lastHitPoint = Vector3.negativeInfinity;
    }
    private void ChangeAnchorPoint()
    {
        startPos = new Vector3(currentBeltPositions[currentBeltPositions.Length - 1].x, currentLayer, currentBeltPositions[currentBeltPositions.Length - 1].z);
    }
    private ConveyorGroup CreateConveyorBeltGroupClassItem(ConveyorGroup newConveyorGroup)
    {
        SaveConveyorsPosition(false);
        newConveyorGroup.conveyorsPos = new Vector3[currentBeltGroupPositions.Length];
        for (int i = 0; i < currentBeltGroupPositions.Length; i++)
            newConveyorGroup.conveyorsPos[i] = currentBeltGroupPositions[i];

        newConveyorGroup.beltGroupId = GetId(currentBeltGroupPositions, string.Empty, false) + "-" + GenerateRandomString();
        if (currentSplitter != null)
        {
            if (currentSplitter.HasTwoOutputs)
                currentSplitter.output2GroupId = newConveyorGroup.beltGroupId;
            else
                currentSplitter.output1GroupId = newConveyorGroup.beltGroupId;

            if (!currentSplitter.isSetInStone && currentBeltGroupPositions.Length > 0)
                currentSplitter.output1Dir = currentBeltGroupPositions[1] - currentSplitter.splitterPos;
            currentSplitter.isSetInStone = true;
            newConveyorGroup.groupAttachedTo = groupAttachedTo;

        }
        else
        {
            DeleteOverlappingBelts();
            newConveyorGroup = MergeLinkedBeltGroups(newConveyorGroup);
        }

        CreateTriggerBoxOnGroup(newConveyorGroup);
        Debug.Log(newConveyorGroup.conveyorGroupTransform.name);

        conveyorGroups.Add(newConveyorGroup);

        startedBeltGroup = false;
        currentSplitter = null;
        groupAttachedTo = string.Empty;
        lastHitPoint = Vector3.negativeInfinity;
        currentBeltPositions = new Vector3[0];
        currentBeltGroupPositions = new Vector3[0];
        overlappingPositions = new Vector3[0];
        overlappedGroups = new string[0];
        return newConveyorGroup;
    }
    private void CreateTriggerBoxOnGroup(ConveyorGroup group)
    {
        Vector3 minCorner = GetMin(group.conveyorsPos);
        Vector3 maxCorner = GetMax(group.conveyorsPos);
        Vector3 middle = Vector3.Lerp(minCorner, maxCorner, 0.5f);

        group.conveyorGroupTransform = Instantiate(BeltGroupPrefab, middle + new Vector3(0f, 0.5f, 0f), quaternion.identity, gameObject.transform);
        group.conveyorGroupTransform.gameObject.name = group.beltGroupId;

        group.collider = group.conveyorGroupTransform.gameObject.GetComponent<BoxCollider>();
        group.collider.size = new Vector3(maxCorner.x - minCorner.x + 1, maxCorner.y - minCorner.y + 1, maxCorner.z - minCorner.z + 1);
    }
    private string[] CheckIfEnteredOtherGroup()
    {
        overlappedGroups = new string[0];
        for (int i = 0; i < currentBeltPositions.Length; i++)
        {
            Ray ray = new Ray(currentBeltPositions[i] + (Vector3.up * 4f), Vector3.down);
            RaycastHit[] rayHits = Physics.RaycastAll(ray, 5f);
            for (int j = 0; j < rayHits.Length; j++)
                if (rayHits[j].collider.CompareTag("BeltGroup") && !overlappedGroups.Contains(rayHits[j].transform.name))
                    overlappedGroups = Append(overlappedGroups, rayHits[j].transform.name);
        }
        return overlappedGroups;
    }
    private void FindAllOverlappingGroup()
    {
        overlappingPositions = new Vector3[0];
        for (int i = 0; i < conveyorGroups.Count; i++)
            if (overlappedGroups.Contains(conveyorGroups[i].beltGroupId))
                overlappingPositions = AppendArray(overlappingPositions, CheckForOverlap(currentBeltPositions, conveyorGroups[i].conveyorsPos));
    }
    private Vector3[] CheckForOverlap(Vector3[] array1, Vector3[] array2)
    {
        Vector3[] intersections = new Vector3[0];
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
        RaycastHit[] hits = Physics.RaycastAll(ray);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.CompareTag("Grid"))
            {
                hitPoint = new Vector3(MathF.Round(hits[i].point.x), currentLayer, MathF.Round(hits[i].point.z));
                if (hitPoint != lastHitPoint || justPressedF)
                    return true;
            }
        }
        return false;
    }
    private void SaveConveyorsPosition(bool removeLastPos)
    {
        Vector3[] newArray = new Vector3[currentBeltGroupPositions.Length + currentBeltPositions.Length];
        for (int i = 0; i < currentBeltGroupPositions.Length; i++)
            newArray[i] = currentBeltGroupPositions[i];
        for (int i = 0; currentBeltPositions.Length > i; i++)
            newArray[newArray.Length - currentBeltPositions.Length + i] = currentBeltPositions[i];

        if (removeLastPos)
            newArray = RemoveFromArray(newArray, newArray[newArray.Length - 1]);

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
    private Vector3[] RemoveFromArray(Vector3[] array, Vector3 ValueToRemove)
    {
        Vector3[] newArray = new Vector3[array.Length - 1];
        int offset = 0;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == ValueToRemove)
            {
                offset = 1;
                continue;
            }
            newArray[i - offset] = array[i];
        }
        return newArray;
    }
    private string GetId(Vector3[] posArray, string lastId, bool isLast)
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
        else if (lastId == string.Empty)
        {
            return "1F";
        }
        else if (!isLast)
        {
            for (int i = 0; i < lastId.Length; i++)
                if (char.IsLetter(lastId[i]))
                    return "1" + lastId[i];
        }
        else
        {
            for (int i = lastId.Length - 1; i > 0; i--)
                if (char.IsLetter(lastId[i]))
                    return "1" + lastId[i];
        }
        return " ";
    }
    private char GetLastBeltType()
    {
        string currentPlacedGroupId = GetId(currentBeltPositions, string.Empty, false);
        Vector3 endDir = GetEndDir(currentPlacedGroupId);
        return GetTypeOfBelt(endDir);
    }
    private string GenerateRandomString()
    {
        int randomInt = UnityEngine.Random.Range(0, 9999);
        return randomInt.ToString();
    }
    private void CheckForSplitter(string currentId)
    {
        bool removeCurrentSplitter = false;
        if (currentBeltPositions.Length == 1)
            removeCurrentSplitter = true;
        if (overlappedGroups.Length != 0 && overlappingPositions.Contains(currentBeltPositions[0]))
        {
            for (int i = 0; i < conveyorGroups.Count; i++)
            {
                if (overlappedGroups.Contains(conveyorGroups[i].beltGroupId))
                {
                    if (!removeCurrentSplitter && GetStartDir(currentId) == GetEndDir(conveyorGroups[i].beltGroupId))
                        removeCurrentSplitter = true;
                    else if (!removeCurrentSplitter && GetStartDir(currentId) * -1 == GetStartDir(conveyorGroups[i].beltGroupId))
                        removeCurrentSplitter = true;
                    if (!removeCurrentSplitter && conveyorGroups[i].conveyorsPos.Contains(currentBeltPositions[1]))
                        removeCurrentSplitter = true;

                    if (lastSplitterPos != currentBeltPositions[0] && !removeCurrentSplitter)
                        AddSplitter(conveyorGroups[i], currentBeltPositions[0], currentId);

                    for (int j = 0; j < conveyorGroups[i].splitters.Count; j++)
                    {
                        if (conveyorGroups[i].splitters[j].splitterPos == currentBeltPositions[0])
                        {
                            groupAttachedTo = conveyorGroups[i].beltGroupId;
                            if (removeCurrentSplitter && !conveyorGroups[i].splitters[j].isSetInStone)
                                DeleteSplitter(conveyorGroups[i].splitters);
                            else if (removeCurrentSplitter && conveyorGroups[i].splitters[j].isSetInStone)
                                ClearSecondOutput(conveyorGroups[i].splitters[j]);
                            else if (!removeCurrentSplitter && conveyorGroups[i].splitters[j].output1Dir != currentBeltPositions[1] - currentBeltPositions[0] && conveyorGroups[i].splitters[j].isSetInStone)
                                AddOutputToSplitter(conveyorGroups[i].splitters[j], currentBeltPositions[0], currentId);
                            DrawSplitters(conveyorGroups[i]);
                            break;
                        }
                    }
                }
            }
        }
    }
    private void AddSplitter(ConveyorGroup conveyorGroup, Vector3 position, string outputId)
    {
        ConveyorGroup.Splitter newSplitter = new ConveyorGroup.Splitter();
        newSplitter.splitterPos = position;
        newSplitter.output1GroupId = outputId;
        newSplitter.output1Dir = currentBeltPositions[1] - position;

        currentSplitter = newSplitter;
        conveyorGroup.splitters.Add(newSplitter);

        lastSplitterPos = newSplitter.splitterPos;
    }
    private void AddOutputToSplitter(ConveyorGroup.Splitter splitterToModify, Vector3 position, string output2Id)
    {
        splitterToModify.output2Dir = currentBeltPositions[1] - position;
        splitterToModify.output2GroupId = output2Id;
        splitterToModify.HasTwoOutputs = true;

        currentSplitter = splitterToModify;
    }
    private void ClearSecondOutput(ConveyorGroup.Splitter splitterToModify)
    {
        splitterToModify.output2Dir = Vector3.zero;
        splitterToModify.output2GroupId = "";
        splitterToModify.HasTwoOutputs = false;

        currentSplitter = null;
        groupAttachedTo = string.Empty;
    }
    private void DeleteSplitter(List<ConveyorGroup.Splitter> splitters)
    {
        splitters.Remove(splitters[splitters.Count - 1]);
        currentSplitter = null;
        lastSplitterPos = Vector3.negativeInfinity;
        groupAttachedTo = string.Empty;
    }
    private void DeleteConveyors()
    {
        lastHitPoint = hitPoint;
        Ray ray = new Ray(new Vector3(hitPoint.x, 4f, hitPoint.z), Vector3.down);
        RaycastHit[] raycastHits = Physics.RaycastAll(ray, 5f);
        for (int i = 0; i < raycastHits.Length; i++)
        {
            if (raycastHits[i].collider.CompareTag("BeltGroup"))
            {
                for (int j = 0; j < conveyorGroups.Count; j++)
                {
                    if (conveyorGroups[j].beltGroupId == raycastHits[i].collider.name)
                    {
                        if (conveyorGroups[j].conveyorsPos.Contains(hitPoint))
                        {
                            if (conveyorGroups[j].conveyorsPos[0] != hitPoint && conveyorGroups[j].conveyorsPos[conveyorGroups[j].conveyorsPos.Length - 1] != hitPoint)
                                DisassociateGroup(conveyorGroups[j]);
                            else if (conveyorGroups[j].conveyorsPos[0] == hitPoint || conveyorGroups[j].conveyorsPos[conveyorGroups[j].conveyorsPos.Length - 1] == hitPoint)
                                UpdateGroup(conveyorGroups[j]);
                        }
                    }
                }
            }
        }
    }
    private void DisassociateGroup(ConveyorGroup group)
    {
        ConveyorGroup newGroup = new ConveyorGroup();
        ConveyorGroup newBaseGroup = new ConveyorGroup();
        bool hasCrossed = false;
        for (int i = 0; i < group.conveyorsPos.Length; i++)
        {
            if (group.conveyorsPos[i] == hitPoint)
            {
                hasCrossed = true;
                continue;
            }

            if (hasCrossed)
                newGroup.conveyorsPos = Append(newGroup.conveyorsPos, group.conveyorsPos[i]);
            else
                newBaseGroup.conveyorsPos = Append(newBaseGroup.conveyorsPos, group.conveyorsPos[i]);
        }
        DeleteGroup(group);

        newBaseGroup.beltGroupId = GetId(newBaseGroup.conveyorsPos, group.beltGroupId, false) + "-" + GenerateRandomString();
        CreateTriggerBoxOnGroup(newBaseGroup);
        CheckForSplitters(group, newBaseGroup);
        RebindAttachedSplitter(group, newBaseGroup);
        newBaseGroup.groupAttachedTo = group.groupAttachedTo;
        conveyorGroups.Add(newBaseGroup);
        RedrawBelts(newBaseGroup);

        newGroup.beltGroupId = GetId(newGroup.conveyorsPos, group.beltGroupId, true) + "-" + GenerateRandomString();
        CreateTriggerBoxOnGroup(newGroup);
        CheckForSplitters(group, newGroup);
        conveyorGroups.Add(newGroup);
        RedrawBelts(newGroup);
    }
    private void UpdateGroup(ConveyorGroup group)
    {
        if (group.conveyorsPos.Length > 1)
        {
            ConveyorGroup newGroup = new ConveyorGroup();
            newGroup.conveyorsPos = RemoveFromArray(group.conveyorsPos, hitPoint);
            newGroup.beltGroupId = GetId(newGroup.conveyorsPos, group.beltGroupId, false) + "-" + GenerateRandomString();
            CreateTriggerBoxOnGroup(newGroup);
            CheckForSplitters(group, newGroup);
            RebindAttachedSplitter(group, newGroup);
            RebindGroupAttachedTo(newGroup);
            newGroup.groupAttachedTo = group.groupAttachedTo;
            conveyorGroups.Add(newGroup);
            RedrawBelts(newGroup);
        }
        DeleteGroup(group);
    }
    private void CheckForSplitters(ConveyorGroup originalGroup, ConveyorGroup newGroup)
    {
        for (int i = 0; i < originalGroup.splitters.Count; i++)
            if (newGroup.conveyorsPos.Contains(originalGroup.splitters[i].splitterPos))
                newGroup.splitters.Add(originalGroup.splitters[i]);
    }
    private void RebindAttachedSplitter(ConveyorGroup group, ConveyorGroup newGroup)
    {
        if (group.groupAttachedTo != string.Empty)
            for (int i = 0; i < conveyorGroups.Count; i++)
                if (conveyorGroups[i].beltGroupId == group.groupAttachedTo)
                    for (int j = 0; j < conveyorGroups[i].splitters.Count; j++)
                        if (conveyorGroups[i].splitters[j].splitterPos == newGroup.conveyorsPos[0])
                        {
                            if (conveyorGroups[i].splitters[j].output1GroupId == group.beltGroupId)
                                conveyorGroups[i].splitters[j].output1GroupId = newGroup.beltGroupId;
                            else if (conveyorGroups[i].splitters[j].output2GroupId == group.beltGroupId)
                                conveyorGroups[i].splitters[j].output2GroupId = newGroup.beltGroupId;
                        }
    }
    private void RebindGroupAttachedTo(ConveyorGroup group)
    {
        for (int i = 0; i < group.splitters.Count; i++)
            for (int j = 0; j < conveyorGroups.Count; j++)
                if (group.splitters[i].output1GroupId == conveyorGroups[j].beltGroupId || group.splitters[i].output2GroupId == conveyorGroups[j].beltGroupId)
                    conveyorGroups[j].groupAttachedTo = group.beltGroupId;
    }
    private ConveyorGroup MergeLinkedBeltGroups(ConveyorGroup currentGroup)
    {
        Vector3 startDir = GetStartDir(currentGroup.beltGroupId);
        Vector3 endDir = GetEndDir(currentGroup.beltGroupId);
        ConveyorGroup newGroup = null;

        ConveyorGroup intersectedGroupUnder = null;
        ConveyorGroup intersectedGroupOver = null;
        string intersectedGroupUnderName = string.Empty;
        string intersectedGroupOverName = string.Empty;

        Ray ray = new Ray(currentGroup.conveyorsPos[0] - startDir + (Vector3.up * 5), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.CompareTag("BeltGroup") && hits[i].collider.transform.position.y * 2 - 1 == currentGroup.conveyorsPos[0].y && GetStartDir(currentGroup.beltGroupId) == GetEndDir(hits[i].collider.name))
                intersectedGroupUnderName = hits[i].collider.name;
        }

        ray = new Ray(currentGroup.conveyorsPos[currentGroup.conveyorsPos.Length - 1] + endDir + (Vector3.up * 5), Vector3.down);
        hits = Physics.RaycastAll(ray);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.CompareTag("BeltGroup") && hits[i].collider.transform.position.y * 2 - 1 == currentGroup.conveyorsPos[currentGroup.conveyorsPos.Length - 1].y && GetEndDir(currentGroup.beltGroupId) == GetStartDir(hits[i].collider.name))
                intersectedGroupOverName = hits[i].collider.name;
        }
        for (int i = 0; i < conveyorGroups.Count; i++)
        {
            if (intersectedGroupOverName == conveyorGroups[i].beltGroupId)
                intersectedGroupOver = conveyorGroups[i];
            if (intersectedGroupUnderName == conveyorGroups[i].beltGroupId)
                intersectedGroupUnder = conveyorGroups[i];
        }
        if (intersectedGroupOver != null && intersectedGroupUnder != null)
        {
            newGroup = MergeGroup(intersectedGroupUnder, currentGroup);
            newGroup = MergeGroup(newGroup, intersectedGroupOver);

            DeleteGroup(currentGroup);
            DeleteGroup(intersectedGroupOver);
            DeleteGroup(intersectedGroupUnder);
            return newGroup;
        }
        else if (intersectedGroupOver != null && intersectedGroupUnder == null)
        {
            newGroup = MergeGroup(newGroup, intersectedGroupOver);

            DeleteGroup(currentGroup);
            DeleteGroup(intersectedGroupOver);
            return newGroup;
        }
        else if (intersectedGroupOver == null && intersectedGroupUnder != null)
        {
            newGroup = MergeGroup(intersectedGroupUnder, currentGroup);

            DeleteGroup(currentGroup);
            DeleteGroup(intersectedGroupUnder);
            return newGroup;
        }
        else
        {
            return currentGroup;
        }
    }
    private ConveyorGroup MergeGroup(ConveyorGroup group1, ConveyorGroup group2)
    {
        ConveyorGroup newGroup = new ConveyorGroup();
        for (int i = 0; i < group1.conveyorsPos.Length; i++)
            newGroup.conveyorsPos = Append(newGroup.conveyorsPos, group1.conveyorsPos[i]);
        for (int i = 0; i < group1.splitters.Count; i++)
            newGroup.splitters.Add(group1.splitters[i]);
        for (int i = 0; i < group2.conveyorsPos.Length; i++)
            if (!newGroup.conveyorsPos.Contains(group2.conveyorsPos[i]))
                newGroup.conveyorsPos = Append(newGroup.conveyorsPos, group2.conveyorsPos[i]);
        for (int i = 0; i < group2.splitters.Count; i++)
            newGroup.splitters.Add(group2.splitters[i]);

        List<GameObject> newBelts = new List<GameObject>();
        for (int i = 0; i < group1.conveyorGroupTransform.childCount; i++)
            newBelts.Add(group1.conveyorGroupTransform.GetChild(i).gameObject);
        newBelts.AddRange(allSelectedBelts);
        allSelectedBelts.Clear();
        allSelectedBelts = newBelts;

        newGroup.beltGroupId = GetId(newGroup.conveyorsPos, string.Empty, false) + "-" + GenerateRandomString();
        newGroup.groupAttachedTo = group1.groupAttachedTo;
        return newGroup;
    }
    private void DeleteOverlappingBelts()
    {
        for (int i = 0; i < conveyorGroups.Count; i++)
        {
            if (overlappedGroups.Contains(conveyorGroups[i].beltGroupId))
            {
                Vector3[] conveyorPosBuffer = new Vector3[conveyorGroups[i].conveyorsPos.Length];
                for (int j = 0; j < conveyorPosBuffer.Length; j++)
                    conveyorPosBuffer[j] = conveyorGroups[i].conveyorsPos[j];
                conveyorGroups[i].conveyorsPos = RemoveDuplicatedPos(conveyorGroups[i].conveyorsPos, overlappingPositions);
                DissasociateUnconectedGroup(conveyorGroups[i], conveyorPosBuffer);
            }
        }
    }
    private Vector3[] RemoveDuplicatedPos(Vector3[] groupPos, Vector3[] overlappingPos)
    {
        int[] posToRemove = new int[groupPos.Length];
        int newArrayLength = groupPos.Length;
        for (int i = 0; i < groupPos.Length; i++)
        {
            if (overlappingPos.Contains(groupPos[i]))
            {
                posToRemove[i] = 1;
                newArrayLength--;
            }
        }

        Vector3[] newPos = new Vector3[newArrayLength];
        int index = 0;
        for (int i = 0; i < posToRemove.Length; i++)
        {
            if (posToRemove[i] == 0)
            {
                newPos[index] = groupPos[i];
                index++;
            }
        }

        return newPos;
    }
    private void DissasociateUnconectedGroup(ConveyorGroup Group, Vector3[] groupOldPos) // TODO : take in consideration when the new group's position is zero or has been fully overlapped
    {
        Vector3[] newGroupPos = new Vector3[0];
        bool firstTime = true;
        for (int i = 0; i < groupOldPos.Length; i++)
        {
            if (Group.conveyorsPos.Contains(groupOldPos[i]))
                newGroupPos = Append(newGroupPos, groupOldPos[i]);
            else if (newGroupPos.Length > 0)
            {
                ConveyorGroup newGroup = new ConveyorGroup();
                newGroup.conveyorsPos = newGroupPos;
                if (firstTime)
                {
                    newGroup.groupAttachedTo = Group.groupAttachedTo;
                    firstTime = false;
                }
                for (int j = 0; j < Group.splitters.Count; j++)
                    if (newGroup.conveyorsPos.Contains(Group.splitters[j].splitterPos))
                        newGroup.splitters.Add(Group.splitters[j]);
                newGroup.beltGroupId = GetId(newGroup.conveyorsPos, Group.beltGroupId, false) + "-" + GenerateRandomString();
                CreateTriggerBoxOnGroup(newGroup);
                conveyorGroups.Add(newGroup);
                newGroupPos = new Vector3[0];
                RedrawBelts(newGroup);
            }
        }
        if (newGroupPos.Length > 0)
        {
            ConveyorGroup newGroup = new ConveyorGroup();
            newGroup.conveyorsPos = newGroupPos;
            if (firstTime)
            {
                newGroup.groupAttachedTo = Group.groupAttachedTo;
                firstTime = false;
            }
            for (int j = 0; j < Group.splitters.Count; j++)
                if (newGroup.conveyorsPos.Contains(Group.splitters[j].splitterPos))
                    newGroup.splitters.Add(Group.splitters[j]);
            newGroup.beltGroupId = GetId(newGroup.conveyorsPos, Group.beltGroupId, true) + "-" + GenerateRandomString();
            CreateTriggerBoxOnGroup(newGroup);
            conveyorGroups.Add(newGroup);
            groupOldPos = RemoveDuplicatedPos(groupOldPos, newGroupPos);
            newGroupPos = new Vector3[0];
            RedrawBelts(newGroup);
        }
        if (!firstTime)
            DeleteGroup(Group);
    }
    private void DeleteGroup(ConveyorGroup group)
    {
        conveyorGroups.Remove(group);
        for (int i = 0; i < gameObject.transform.childCount; i++)
            if (gameObject.transform.GetChild(i).name == group.beltGroupId)
                Destroy(gameObject.transform.GetChild(i).gameObject);
    }
    private Vector3 GetStartDir(string groupId)
    {
        char dir = ' ';
        for (int i = 0; i < groupId.Length; i++)
        {
            if (char.IsLetter(groupId[i]))
            {
                dir = groupId[i];
                break;
            }
        }
        return GetDirOfBelt(dir);
    }
    private Vector3 GetEndDir(string groupId)
    {
        char dir = ' ';
        for (int i = groupId.Length - 1; i > 0; i--)
        {
            if (char.IsLetter(groupId[i]))
            {
                dir = groupId[i];
                break;
            }
        }
        return GetDirOfBelt(dir);
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
    private Vector3 GetDirOfBelt(char type)
    {
        Vector3 dirOfBelt = Vector3.zero;
        if (type == 'F')
            dirOfBelt = new Vector3(0f, 0f, 1f);
        else if (type == 'B')
            dirOfBelt = new Vector3(0f, 0f, -1f);
        else if (type == 'R')
            dirOfBelt = new Vector3(1f, 0f, 0f);
        else if (type == 'L')
            dirOfBelt = new Vector3(-1f, 0f, 0f);
        else if (type == 'U')
            dirOfBelt = new Vector3(0f, 1f, 0f);
        else if (type == 'D')
            dirOfBelt = new Vector3(0f, -1f, 0f);
        return dirOfBelt;
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
        if (Input.GetKeyDown(KeyCode.C))
            justPressedC = true;
        else
            justPressedC = false;
    }
    private void DeleteCurrentSelectedBelts()
    {
        for (int i = 0; i < currentSelectedBelts.Count; i++)
            Destroy(currentSelectedBelts[i]);
        currentSelectedBelts.Clear();
    }
    private void DeleteAllSelectedBelts()
    {
        for (int i = 0; i < allSelectedBelts.Count; i++)
            Destroy(allSelectedBelts[i]);
        allSelectedBelts.Clear();
        lastBeltType = ' ';
    }
    private void DrawSelectedBeltGroup(string currentId, Vector3 startPos, char lockedOrientation)
    {
        int Yrotation = 0;
        string component = "";
        char lastType = ' ';
        int amount = 0;
        Vector3 currentPos = startPos;
        bool drawCorner = false;
        bool willDrawCorner = false;
        bool lockStartOrientation = true;

        bool containsSplitter = false;

        for (int i = 0; i < currentId.Length; i++)
        {
            if (char.IsDigit(currentId[i]))
                component += currentId[i];
            if (char.IsLetter(currentId[i]))
            {
                Vector3 currentDir = GetDirOfBelt(currentId[i]);
                amount = int.Parse(component);
                if (drawCorner)
                    willDrawCorner = true;
                if (i + 1 < currentId.Length)
                {
                    lastType = currentId[i];
                    drawCorner = true;
                    amount--;
                }
                if (currentSelectedBelts.Count != 0 && !containsSplitter)
                {
                    amount--;
                    currentPos += GetStartDir(currentId);
                    containsSplitter = true;
                }
                component = "";
                Yrotation = GetRotation(currentId[i]);
                for (int j = 0; j < amount; j++)
                {
                    if (lockStartOrientation && (lockedOrientation == 'F' || lockedOrientation == 'B' || lockedOrientation == 'L' || lockedOrientation == 'R') && GetDirOfBelt(lockedOrientation) != GetStartDir(currentId) && (GetStartDir(currentId) != GetDirOfBelt(lockedOrientation) * -1))
                    {
                        GameObject corner = Instantiate(cornerBelt, gameObject.transform);
                        corner.transform.position = currentPos;
                        corner.transform.eulerAngles = new Vector3(0, GetCornerRotation(lockedOrientation, currentId[i]).y, 0);
                        float newZScale = GetCornerRotation(lockedOrientation, currentId[i]).x;
                        corner.transform.localScale = new Vector3(newZScale, 1, 1);
                        ApplyMaterial(corner, selectedMaterial);
                        currentPos += currentDir;
                        currentSelectedBelts.Add(corner);
                        lockStartOrientation = false;
                        continue;
                    }
                    else if (willDrawCorner)
                    {
                        GameObject corner = Instantiate(cornerBelt, gameObject.transform);
                        corner.transform.position = currentPos;
                        corner.transform.eulerAngles = new Vector3(0, GetCornerRotation(lastType, currentId[i]).y, 0);
                        corner.transform.localScale = new Vector3(GetCornerRotation(lastType, currentId[i]).x, 1, 1);
                        ApplyMaterial(corner, selectedMaterial);
                        currentPos += currentDir;
                        currentSelectedBelts.Add(corner);
                        willDrawCorner = false;
                        continue;
                    }
                    GameObject belt = Instantiate(straightBelt, gameObject.transform);
                    belt.transform.position = currentPos;
                    belt.transform.eulerAngles = new Vector3(0, Yrotation, 0);
                    ApplyMaterial(belt, selectedMaterial);
                    currentPos += currentDir;
                    currentSelectedBelts.Add(belt);
                    lockStartOrientation = false;
                }
                containsSplitter = true;
            }
        }
    }
    private void SaveSelectedBelts(bool removeLastBelt)
    {
        if (currentSelectedBelts.Count > 0)
        {
            allSelectedBelts.AddRange(currentSelectedBelts);
            currentSelectedBelts.Clear();
            if (removeLastBelt)
            {
                Destroy(allSelectedBelts[allSelectedBelts.Count - 1]);
                allSelectedBelts.RemoveAt(allSelectedBelts.Count - 1);
            }
        }
    }
    private void ApplyMaterial(GameObject gameObject, Material material)
    {
        Transform meshTr = gameObject.transform.GetChild(0);
        meshTr.GetComponent<MeshRenderer>().material = material;
    }
    private void DrawFinalBelts(Transform parent)
    {
        Debug.Log(parent.name);
        for (int i = 0; i < allSelectedBelts.Count; i++)
        {
            GameObject currentBelt = Instantiate(allSelectedBelts[i], parent);
            currentBelt.transform.position = allSelectedBelts[i].transform.position;
            ApplyMaterial(currentBelt, normalMaterial);
        }
    }
    private void RedrawBelts(ConveyorGroup group)
    {
        //List<Vector3> splitterPos = new List<Vector3>(); // TODO : Account for splitter when Updating a group's belts
        //for (int i = 0; i < group.splitters.Count; i++)
        //    splitterPos.Add(group.splitters[i].splitterPos);

        string currentId = group.beltGroupId;
        int Yrotation = 0;
        string component = "";
        char lastType = ' ';
        int amount = 0;
        Vector3 currentPos = group.conveyorsPos[0];

        for (int i = 0; i < currentId.Length; i++)
        {
            if (char.IsDigit(currentId[i]))
                component += currentId[i];
            if (char.IsLetter(currentId[i]))
            {
                Vector3 currentDir = GetDirOfBelt(currentId[i]);
                amount = int.Parse(component);
                if (currentId[i + 1] != '-')
                    amount--;
                component = "";
                Yrotation = GetRotation(currentId[i]);
                for (int j = 0; j < amount; j++)
                {
                    if (lastType != ' ')
                    {
                        GameObject corner = Instantiate(cornerBelt, group.conveyorGroupTransform);
                        corner.transform.position = currentPos;
                        corner.transform.eulerAngles = new Vector3(0, GetCornerRotation(lastType, currentId[i]).y, 0);
                        corner.transform.localScale = new Vector3(GetCornerRotation(lastType, currentId[i]).x, 1, 1);
                        currentPos += currentDir;
                        lastType = ' ';
                        continue;
                    }
                    GameObject belt = Instantiate(straightBelt, group.conveyorGroupTransform);
                    belt.transform.position = currentPos;
                    belt.transform.eulerAngles = new Vector3(0, Yrotation, 0);
                    currentPos += currentDir;
                }
                lastType = currentId[i];
            }
        }
    }
    private void DrawSplitters(ConveyorGroup group)
    {
        Vector3 downDir = Vector3.zero;
        Vector3 upDir = Vector3.zero;
        for (int i = 0; i < group.splitters.Count; i++)
        {
            GameObject splitterToDraw;
            int splitterPosIndex = -1;
            DeleteAllBeltsAtPos(group.splitters[i].splitterPos);
            for (int j = 0; j < group.conveyorsPos.Length; j++)
            {
                if (group.conveyorsPos[j] == group.splitters[i].splitterPos)
                {
                    splitterPosIndex = j;
                    if (splitterPosIndex + 1 == group.conveyorsPos.Length)
                        upDir = GetEndDir(group.beltGroupId);
                    else
                        upDir = group.conveyorsPos[splitterPosIndex + 1] - group.conveyorsPos[splitterPosIndex];
                    if (j == 0)
                        downDir = GetStartDir(group.beltGroupId);
                    else
                        downDir = group.conveyorsPos[splitterPosIndex] - group.conveyorsPos[splitterPosIndex - 1];
                    splitterToDraw = FindSplitterObj(upDir, downDir, group.splitters[i]);
                    splitterToDraw = Instantiate(splitterToDraw);
                    splitterToDraw.transform.position = group.splitters[i].splitterPos;
                    ApplyMaterial(splitterToDraw, selectedMaterial);
                    currentSelectedBelts.Add(splitterToDraw);
                }
            }
        }
    }
    private GameObject FindSplitterObj(Vector3 upDir, Vector3 downDir, ConveyorGroup.Splitter splitter)
    {
        bool isStraightSplitter = false;
        if (upDir * -1 == downDir)
            isStraightSplitter = true;

        if (isStraightSplitter && !splitter.HasTwoOutputs)
        {
            if (splitter.output1Dir == Vector3.right || splitter.output1Dir == Vector3.forward)
                return leftSplitter;
            else
                return rightSplitter;
        }
        else if (isStraightSplitter && splitter.HasTwoOutputs)
        {
            return fullSplitter;
        }
        else if (!isStraightSplitter && !splitter.HasTwoOutputs)
        {
            if (upDir == splitter.output1Dir * -1)
                return cornerSplitter;
            else
            {
                if (upDir == Vector3.right || upDir == Vector3.forward)
                    return rightSplitter;
                else
                    return leftSplitter;
            }
        }
        return null;
    }
    private void DeleteAllBeltsAtPos(Vector3 pos)
    {
        Ray ray = new Ray(pos + Vector3.up / 2, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1f);
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].collider.CompareTag("Belt"))
                Destroy(hits[i].collider.gameObject);
    }
    private int GetRotation(char type)
    {
        int rotation = 0;
        if (type == 'F')
            rotation = 0;
        else if (type == 'B')
            rotation = 180;
        else if (type == 'R')
            rotation = 90;
        else if (type == 'L')
            rotation = 270;

        return rotation;
    }
    private Vector2 GetCornerRotation(char type1, char type2)
    {
        float rotation = 0;
        float scale = 1;

        if (type2 == 'F')
            rotation = 180;
        if (type2 == 'B')
            rotation = 0;
        if (type2 == 'L')
            rotation = 90;
        if (type2 == 'R')
            rotation = 270;

        if (type1 == 'F' && type2 == 'R')
            scale = 1;
        else if (type1 == 'F' && type2 == 'L')
            scale = -1;
        else if (type1 == 'B' && type2 == 'R')
            scale = -1;
        else if (type1 == 'B' && type2 == 'L')
            scale = 1;
        else if (type1 == 'R' && type2 == 'F')
            scale = -1;
        else if (type1 == 'R' && type2 == 'B')
            scale = 1;
        else if (type1 == 'L' && type2 == 'F')
            scale = 1;
        else if (type1 == 'L' && type2 == 'B')
            scale = -1;

        return new Vector2(scale, rotation);
    }
}