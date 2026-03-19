using System.Collections.Generic;
using UnityEngine;

public class SImulation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Building> buildings;
    public int profit = 0;
    void Start()
    {
        
    }
    
    // Update is called once per frame
    void Update()
    {
           
    }
    public void NextStep()
    {
        Debug.Log("START profit: " + profit);

        foreach (Building b in buildings)
        {
            int value = b.Simulate();
            Debug.Log("Adding " + value);

            profit += value;

            Debug.Log("Intermediate profit: " + profit);
        }

        Debug.Log("FINAL profit: " + profit);
    }
}
