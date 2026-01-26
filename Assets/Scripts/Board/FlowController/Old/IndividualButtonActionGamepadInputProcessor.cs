using System;
using UnityEngine;

public class IndividualButtonActionGamepadInputProcessor : BaseGamepadInputProcessor
{
    public Action<Vector2> OnLeftStickUpdated;      // 1
    public Action<Vector2> OnRightStickUpdated;     // 2

    public Action<float> OnLeftTriggerUpdated;      // 3
    public Action<float> OnRightTriggerUpdated;     // 4

    public Action<bool> OnLeftStickButtonUpdated;   // 5
    public Action<bool> OnRightStickButtonUpdated;  // 6

    public Action<bool> OnLeftShoulderUpdated;      // 7
    public Action<bool> OnRightShoulderUpdated;     // 8

    public Action<bool> OnAButtonUpdated;           // 9
    public Action<bool> OnBButtonUpdated;           // 10
    public Action<bool> OnXButtonUpdated;           // 11
    public Action<bool> OnYButtonUpdated;           // 12

    public Action<bool> OnStartButtonUpdated;       // 13
    public Action<bool> OnSelectButtonUpdated;      // 14

    public Action<bool> OnNorthButtonUpdated;       // 15
    public Action<bool> OnEastButtonUpdated;        // 16
    public Action<bool> OnSouthButtonUpdated;       // 17
    public Action<bool> OnWestButtonUpdated;        // 18

    public override void ProcessGamepadInput(GamepadInput gamepadInput, float deltaTime)
    {
        OnLeftStickUpdated?.Invoke(gamepadInput.leftStick);
        OnRightStickUpdated?.Invoke(gamepadInput.rightStick);

        OnLeftTriggerUpdated?.Invoke(gamepadInput.leftTrigger);
        OnRightTriggerUpdated?.Invoke(gamepadInput.rightTrigger);

        OnLeftShoulderUpdated?.Invoke(gamepadInput.leftShoulder);
        OnRightShoulderUpdated?.Invoke(gamepadInput.rightShoulder);

        OnLeftStickButtonUpdated?.Invoke(gamepadInput.leftStickButton);
        OnRightStickButtonUpdated?.Invoke(gamepadInput.rightStickButton);

        OnAButtonUpdated?.Invoke(gamepadInput.aButton);
        OnBButtonUpdated?.Invoke(gamepadInput.bButton);
        OnXButtonUpdated?.Invoke(gamepadInput.xButton);
        OnYButtonUpdated?.Invoke(gamepadInput.yButton);

        OnNorthButtonUpdated?.Invoke(gamepadInput.northButton);
        OnEastButtonUpdated?.Invoke(gamepadInput.eastButton);
        OnSouthButtonUpdated?.Invoke(gamepadInput.southButton);
        OnWestButtonUpdated?.Invoke(gamepadInput.westButton);

        OnStartButtonUpdated?.Invoke(gamepadInput.startButton);
        OnSelectButtonUpdated?.Invoke(gamepadInput.selectButton);
    }
}
