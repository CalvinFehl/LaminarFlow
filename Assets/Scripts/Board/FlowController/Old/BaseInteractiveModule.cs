using System.Collections.Generic;
using UnityEngine;

public class BaseInteractiveModule : MonoBehaviour
{
    public List<BaseReceiver> Receivers;
}

public class ButtonCodeInteractiveModule : MonoBehaviour
{
    [SerializeField] private List<int> buttonCodes;

    protected bool checkButton(int assignedButton, GamepadInput currentInput)
    {
        switch (assignedButton)
        {
            case 1:
                return currentInput.leftShoulder;
            case 2:
                return currentInput.rightShoulder;
            case 3:
                return currentInput.leftStickButton;
            case 4:
                return currentInput.rightStickButton;
            case 5:
                return currentInput.aButton;
            case 6:
                return currentInput.bButton;
            case 7:
                return currentInput.xButton;
            case 8:
                return currentInput.yButton;
            case 9:
                return currentInput.northButton;
            case 10:
                return currentInput.eastButton;
            case 11:
                return currentInput.southButton;
            case 12:
                return currentInput.westButton;
            case 13:
                return currentInput.startButton;
            case 14:
                return currentInput.selectButton;
            case 15:
                return currentInput.leftTrigger > 0.01f;
            case 16:
                return currentInput.rightTrigger > 0.01f;
        }
        return false;
    }

    protected float checkTrigger(bool usesLeftTrigger, GamepadInput currentInput)
    {
        switch (usesLeftTrigger)
        {
            case true:
                return currentInput.leftTrigger;
            case false:
                return currentInput.rightTrigger;
        }
    }

    protected Vector2 checkStick(int assignedStick, GamepadInput currentInput)
    {
        switch (assignedStick)
        {
            case 1:
                return currentInput.leftStick;
            case 2:
                return currentInput.rightStick;
        }
        return Vector2.zero;
    }

    protected float checkAxis(int assignedAxis, GamepadInput currentInput)
    {
        switch (assignedAxis)
        {
            case 1:
                return currentInput.leftStick.x;
            case 2:
                return currentInput.leftStick.y;
            case 3:
                return currentInput.rightStick.x;
            case 4:
                return currentInput.rightStick.y;
        }
        return 0f;
    }
}