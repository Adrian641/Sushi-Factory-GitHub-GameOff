using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class LevelingSystem : MonoBehaviour
{
    private int level = 0;
    private int coins = 0;

    int algaeRaw = 0;
    int riceRaw = 0;
    int salmonRaw = 0;
    int tunaRaw = 0;
    int greenRaw = 0;
    int yellowRaw = 0;

    //int algaeChopped = 0;
    //int riceChopped = 0;
    int salmonChopped = 0;
    int tunaChopped = 0;
    int greenChopped = 0;
    int yellowChopped = 0;

    int salmonNigiri = 0;
    int tunaNigiri = 0;
    int yellowNigiri = 0;

    string fuckyou = "fuckyou";
    char myFavoriteNumber = '3';

    public enum sushisTypes
    {
        algaeRaw = 0,
        riceRaw,
        salmonRaw,
        tunaRaw,
    }
    public int[] sushiAmounts = new int[1];

    private void Start()
    {
        int[] numbers = { 3, 33, 3, 0, 99, -1, -90, 4, 7, 7, 8, 180, 260, 370, 360, 111, 999, 2, 0, 567, 63, 75, 340, 37, 73, 7, 7, 8, 10, 11, 43, 34, 54, 45, 7 };
        numbers = Sort(numbers);
        for (int i = 0; i < numbers.Length; i++)
            Debug.Log(numbers[i]);
    }

    private bool isPair(int number)
    {
        if (number % 2 == 0)
            return true;
        return false;
    }

    private int[] Sort(int[] array)
    {
        while (true)
        {
            bool hasSwitched = false;
            for (int i = 0; i < array.Length - 1; i++)
            {
                if (array[i] > array[i + 1])
                {
                    hasSwitched = true;
                    int mem = array[i];
                    array[i] = array[i + 1];
                    array[i + 1] = mem;
                }
            }
            if (!hasSwitched)
                break;
        }
        return array;
    }

    private void Update()
    {

        //sushiAmounts[(int)sushisTypes.algaeRaw]++;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Sushi"))
            if (collider.name[0] == '0')
                sushiAmounts[(int)sushisTypes.algaeRaw]++;
    }
}
