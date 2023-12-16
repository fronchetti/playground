using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeObjectSize : MonoBehaviour
{
    public GameObject cup;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void IncreaseCupSize()
    {
        cup.transform.localScale *= 2;
    }

    public void ReduceCupSize()
    {
        cup.transform.localScale *= 0.5f;
    }
}
