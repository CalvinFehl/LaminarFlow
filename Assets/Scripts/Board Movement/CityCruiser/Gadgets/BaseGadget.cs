using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseGadget : MonoBehaviour
{
    public int assignedButton = 0;
    public bool buttonPressed = false;
    public bool wasPressed = false;

    void Start()
    {
        
    }
    public int AssignButton()
    {
        return assignedButton;
    }
}
