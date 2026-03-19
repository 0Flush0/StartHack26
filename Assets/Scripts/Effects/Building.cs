using UnityEngine;

public class Building : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private int profit;
    public int Simulate()
    {
        Debug.Log("returning " + profit);
        return profit;
    }
}
