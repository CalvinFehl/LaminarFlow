using UnityEngine;

public struct GamepadInput
{
    public Vector2 leftStick;       // 1
    public Vector2 rightStick;      // 2

    public float leftTrigger;       // 3
    public float rightTrigger;      // 4

    public bool leftShoulder;       // 5
    public bool rightShoulder;      // 6

    public bool leftStickButton;    // 7
    public bool rightStickButton;   // 8

    public bool aButton;            // 9
    public bool bButton;            // 10
    public bool xButton;            // 11
    public bool yButton;            // 12

    public bool northButton;        // 13
    public bool eastButton;         // 14
    public bool southButton;        // 15
    public bool westButton;         // 16
                                    
    public bool startButton;        // 17
    public bool selectButton;       // 18

    public void Reset()
    {
        leftStick = Vector2.zero;
        rightStick = Vector2.zero;
        leftTrigger = 0f;
        rightTrigger = 0f;

        leftShoulder = false;
        rightShoulder = false;
        leftStickButton = false;
        rightStickButton = false;

        aButton = false;
        bButton = false;
        xButton = false;
        yButton = false;

        northButton = false;
        eastButton = false;
        southButton = false;
        westButton = false;

        startButton = false;
        selectButton = false;
    }
}
