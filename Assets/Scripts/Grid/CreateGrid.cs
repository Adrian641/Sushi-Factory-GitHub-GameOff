using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateGrid : MonoBehaviour
{
    public int gridSizeX = 17;
    public int gridSizeY = 16;

    public GameObject Grid;
    public GameObject tile1;
    public GameObject tile2;

    void Start()
    {
        for (int i = 0; i < gridSizeX; i++)
        {
            for (int j = 0; j < gridSizeY; j++)
            {
                Quaternion rotation = Quaternion.Euler(0f, 0, 0);

                if (i % 2 == 0)
                {
                    if (j % 2 == 0)
                        Instantiate(tile1, new Vector3(j, -0.01f, i), rotation, Grid.transform);
                    else
                        Instantiate(tile2, new Vector3(j, -0.01f, i), rotation, Grid.transform);

                }
                else
                {
                    if (j % 2 == 0)
                        Instantiate(tile2, new Vector3(j, -0.01f, i), rotation, Grid.transform);
                    else
                        Instantiate(tile1, new Vector3(j, -0.01f, i), rotation, Grid.transform);
                }
            }
        }
    }
}
