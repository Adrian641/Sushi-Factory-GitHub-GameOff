using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateGrid : MonoBehaviour
{
    public int gridSizeX = 20;
    public int gridSizeY = 20;

    public GameObject Grid;
    public GameObject tile1;
    public GameObject tile2;
    public GameObject layerGrid;

    void Start()
    {
        Quaternion rotation = Quaternion.Euler(0f, 0, 0);
        layerGrid.transform.localScale = new Vector3((float)gridSizeX / 10, 0.01f, (float)gridSizeY / 10);
        Instantiate(layerGrid, new Vector3((float)gridSizeX / 2 - 0.5f, 0f, (float)gridSizeY / 2 - 0.5f), rotation, Grid.transform);
        for (int i = 0; i < gridSizeX; i++)
        {
            for (int j = 0; j < gridSizeY; j++)
            {

                if (i % 2 == 0)
                {
                    if (j % 2 == 0)
                        Instantiate(tile1, new Vector3(j, 0.01f, i), rotation, Grid.transform);
                    else
                        Instantiate(tile2, new Vector3(j, 0.01f, i), rotation, Grid.transform);

                }
                else
                {
                    if (j % 2 == 0)
                        Instantiate(tile2, new Vector3(j, 0.01f, i), rotation, Grid.transform);
                    else
                        Instantiate(tile1, new Vector3(j, 0.01f, i), rotation, Grid.transform);
                }
            }
        }
    }
}
