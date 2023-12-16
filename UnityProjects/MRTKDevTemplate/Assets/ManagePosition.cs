using MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagePosition : MonoBehaviour
{
    public GameObject positionModel;
    [UnityEngine.InputSystem.Layouts.InputControl(usage = "PointerPosition")]
    private UnityEngine.Vector3 pointerPosition;
    [UnityEngine.InputSystem.Layouts.InputControl(usage = "PointerRotation")]
    private UnityEngine.Quaternion pointerRotation;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreatePosition()
    {
        Instantiate(positionModel, pointerPosition, pointerRotation);
    }

}
