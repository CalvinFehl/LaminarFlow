using UnityEngine;

public class GetGamepadParameter {
    /// <summary>
    /// 1 = Left Shoulder, 2 = Right Shoulder, 3 = Left Stick Button, 4 = Right Stick Button<br />
    /// 5 = A Button, 6 = B Button, 7 = X Button, 8 = Y Button<br />
    /// 9 = North Button, 10 = East Button, 11 = South Button, 12 = West Button<br />
    /// 13 = Start Button, 14 = Select Button<br />
    /// 15 = Left Trigger, 16 = Right Trigger<br />
    /// 17 = Left Stick X, 18 = Left Stick Y, 19 = Right Stick X, 20 = Right Stick Y
    /// </summary>
    public bool CheckButton(int assignedButton, GamepadInput currentInput)
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
            case 17:
                return Mathf.Abs(currentInput.leftStick.x) > 0.01f;
            case 18:
                return Mathf.Abs(currentInput.leftStick.y) > 0.01f;
            case 19:
                return Mathf.Abs(currentInput.rightStick.x) > 0.01f;
            case 20:
                return Mathf.Abs(currentInput.rightStick.y) > 0.01f;
        }
        return false;
    }
        
    public float CheckTrigger(bool usesLeftTrigger, GamepadInput currentInput)
    {
        return usesLeftTrigger ? currentInput.leftTrigger : currentInput.rightTrigger;
    }

    public Vector2 CheckStick(bool usesLeftStick, GamepadInput currentInput)
    {
        return usesLeftStick ? currentInput.leftStick : currentInput.rightStick;
    }

    /// <summary>
    /// 1 = Left Stick X, 2 = Left Stick Y, 3 = Right Stick X, 4 = Right Stick Y
    /// </summary>
    public float CheckAxis(int assignedAxis, GamepadInput currentInput)
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