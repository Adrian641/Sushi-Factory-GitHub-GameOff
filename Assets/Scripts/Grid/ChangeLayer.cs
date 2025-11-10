using UnityEngine;

public class ChangeLayer : MonoBehaviour
{
    private int currentLayer = 0;

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
            currentLayer++;
            LayerGrid.transform.position = new Vector3(LayerGridPos.x, currentLayer, LayerGridPos.z);
        }

        if (currentLayer != 0 && hasPressedLayerDown)
        {
            currentLayer--;
            LayerGrid.transform.position = new Vector3(LayerGridPos.x, currentLayer, LayerGridPos.z);
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
