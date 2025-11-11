using UnityEngine;
using UnityEngine.UIElements;

public class ChangeLayer : MonoBehaviour
{
    public Camera mainCam;

    public int currentLayer = 0;
    public bool hasChangedLayer = false;

    private bool hasPressedLayerUp = false;
    private bool hasPressedLayerDown = false;
    private bool hasObtainedLayerGridGameObject = false;

    private GameObject LayerGrid;
    private Vector3 LayerGridPos;

    const int MAX_HEIGHT = 2;

    private void Start()
    {

    }

    void Update()
    {
        if (!hasObtainedLayerGridGameObject)
        {
            LayerGrid = gameObject.transform.GetChild(0).gameObject;
            LayerGridPos = LayerGrid.transform.position;
            hasObtainedLayerGridGameObject = true;
        }

        CheckUsersInputs();

        if (currentLayer != MAX_HEIGHT && hasPressedLayerUp)
        {
            mainCam.transform.position += Vector3.up;  
            currentLayer++;
            LayerGrid.transform.position = new Vector3(LayerGridPos.x, currentLayer, LayerGridPos.z);
            hasChangedLayer = true;
        }

        if (currentLayer != 0 && hasPressedLayerDown)
        {
            mainCam.transform.position += Vector3.down;
            currentLayer--;
            LayerGrid.transform.position = new Vector3(LayerGridPos.x, currentLayer, LayerGridPos.z);
            hasChangedLayer = true;
        }


    }

    private void CheckUsersInputs()
    {
        if (Input.GetKeyDown(KeyCode.E))
            hasPressedLayerUp = true;
        else
            hasPressedLayerUp = false; 
        if (Input.GetKeyDown(KeyCode.Q))
            hasPressedLayerDown = true;
        else
            hasPressedLayerDown = false;
    }
}
