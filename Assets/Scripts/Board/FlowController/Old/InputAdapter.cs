using UnityEngine;

public class InputAdapter : MonoBehaviour
{
    [SerializeField] private IndividualButtonActionGamepadInputProcessor inputProcessor;

    [SerializeField] private BaseInteractiveModule targetModule;

    private void OnEnable()
    {
        Bind();
    }

    private void OnDisable()
    {
        Bind(true);
    }

    private void Bind(bool isUnsubscribing = false)
    {
        if (inputProcessor != null && targetModule?.Receivers != null)
        {
            foreach (BaseReceiver receiver in targetModule.Receivers)
            {
                // Vector2InputReceiver
                if (receiver is Vector2InputReceiver vector2Receiver)
                {
                    if (receiver.buttonName.Contains("eft") || receiver.buttonName.Contains("L"))
                    {
                        if (isUnsubscribing) { inputProcessor.OnLeftStickUpdated -= (Vector2 value) => { vector2Receiver.value = value; }; }
                        else { inputProcessor.OnLeftStickUpdated += (Vector2 value) => { vector2Receiver.value = value; }; }
                    }
                    else if (receiver.buttonName.Contains("ight") || receiver.buttonName.Contains("R"))
                    {
                        if (isUnsubscribing) { inputProcessor.OnRightStickUpdated -= (Vector2 value) => { vector2Receiver.value = value; }; }
                        else { inputProcessor.OnRightStickUpdated += (Vector2 value) => { vector2Receiver.value = value; }; }
                    }
                }

                // FloatInputReceiver
                else if (receiver is FloatInputReceiver floatReceiver)
                {
                    if (receiver.buttonName.Contains("eft") || receiver.buttonName.Contains("L"))
                    {
                        // Left Trigger (L2) and Left Stick.[x/y/mag] >0.1f
                        if (receiver.buttonName.Contains("rigger") || receiver.buttonName.Contains("2"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnLeftTriggerUpdated -= (float value) => { floatReceiver.value = value; }; }
                            else { inputProcessor.OnLeftTriggerUpdated += (float value) => { floatReceiver.value = value; }; }
                        }
                        else if (receiver.buttonName.Contains("X") || receiver.buttonName.Contains("orizontal"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnLeftStickUpdated -= (Vector2 value) => { floatReceiver.value = value.x; }; }
                            else { inputProcessor.OnLeftStickUpdated += (Vector2 value) => { floatReceiver.value = value.x; }; }
                        }
                        else if (receiver.buttonName.Contains("Y") || receiver.buttonName.Contains("ertical"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnLeftStickUpdated -= (Vector2 value) => { floatReceiver.value = value.y; }; }
                            else { inputProcessor.OnLeftStickUpdated += (Vector2 value) => { floatReceiver.value = value.y; }; }
                        }
                        else
                        {
                            if (isUnsubscribing) { inputProcessor.OnLeftStickUpdated -= (Vector2 value) => { floatReceiver.value = value.magnitude; }; }
                            else { inputProcessor.OnLeftStickUpdated += (Vector2 value) => { floatReceiver.value = value.magnitude; }; }
                        }
                    }
                    else if (receiver.buttonName.Contains("ight") || receiver.buttonName.Contains("R"))
                    {
                        // Right Trigger (R2) and Right Stick.[x/y/mag] >0.1f
                        if (receiver.buttonName.Contains("rigger") || receiver.buttonName.Contains("2"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnRightTriggerUpdated -= (float value) => { floatReceiver.value = value; }; }
                            else { inputProcessor.OnRightTriggerUpdated += (float value) => { floatReceiver.value = value; }; }
                        }
                        else if (receiver.buttonName.Contains("X") || receiver.buttonName.Contains("orizontal"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnRightStickUpdated -= (Vector2 value) => { floatReceiver.value = value.x; }; }
                            else { inputProcessor.OnRightStickUpdated += (Vector2 value) => { floatReceiver.value = value.x; }; }
                        }
                        else if (receiver.buttonName.Contains("Y") || receiver.buttonName.Contains("ertical"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnRightStickUpdated -= (Vector2 value) => { floatReceiver.value = value.y; }; }
                            else { inputProcessor.OnRightStickUpdated += (Vector2 value) => { floatReceiver.value = value.y; }; }
                        }
                        else
                        {
                            if (isUnsubscribing) { inputProcessor.OnRightStickUpdated -= (Vector2 value) => { floatReceiver.value = value.magnitude; }; }
                            else { inputProcessor.OnRightStickUpdated += (Vector2 value) => { floatReceiver.value = value.magnitude; }; }
                        }
                    }
                }

                // BoolInputReceiver
                else if (receiver is BoolInputReceiver boolReceiver)
                {
                    if (!receiver.buttonName.ToLower().Contains("pad"))
                    {
                        if (receiver.buttonName.Contains("eft") || receiver.buttonName.Contains("L"))
                        {
                            // Left Shoulder (L1)
                            if (receiver.buttonName.Contains("houlder") || receiver.buttonName.Contains("1"))
                            {
                                if (isUnsubscribing) { inputProcessor.OnLeftShoulderUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                                else { inputProcessor.OnLeftShoulderUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                            }
                            // Left Trigger (L2)
                            else if (receiver.buttonName.Contains("rigger") || receiver.buttonName.Contains("2"))
                            {
                                if (isUnsubscribing) { inputProcessor.OnLeftTriggerUpdated -= (float value) => { boolReceiver.isPressed = value > 0.1f; }; }
                                else { inputProcessor.OnLeftTriggerUpdated += (float value) => { boolReceiver.isPressed = value > 0.1f; }; }
                            }
                            // Left Stick (L3 and >0.1f)
                            else
                            {
                                if (receiver.buttonName.Contains("utton") || receiver.buttonName.Contains("3"))
                                {
                                    if (isUnsubscribing) { inputProcessor.OnLeftStickButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                                    else { inputProcessor.OnLeftStickButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                                }
                                else if (receiver.buttonName.Contains("X") || receiver.buttonName.Contains("orizontal"))
                                {
                                    if (isUnsubscribing) { inputProcessor.OnLeftStickUpdated -= (Vector2 value) => { boolReceiver.isPressed = value.x > 0.1f; }; }
                                    else { inputProcessor.OnLeftStickUpdated += (Vector2 value) => { boolReceiver.isPressed = value.x > 0.1f; }; }
                                }
                                else if (receiver.buttonName.Contains("Y") || receiver.buttonName.Contains("ertical"))
                                {
                                    if (isUnsubscribing) { inputProcessor.OnLeftStickUpdated -= (Vector2 value) => { boolReceiver.isPressed = value.y > 0.1f; }; }
                                    else
                                    { inputProcessor.OnLeftStickUpdated += (Vector2 value) => { boolReceiver.isPressed = value.y > 0.1f; }; }
                                }
                                else
                                {
                                    if (isUnsubscribing) { inputProcessor.OnLeftStickUpdated -= (Vector2 value) => { boolReceiver.isPressed = value.magnitude > 0.1f; }; }
                                    else { inputProcessor.OnLeftStickUpdated += (Vector2 value) => { boolReceiver.isPressed = value.magnitude > 0.1f; }; }
                                }
                            }
                        }

                        else if (receiver.buttonName.Contains("ight") || receiver.buttonName.Contains("R"))
                        {
                            // Right Shoulder (R1)
                            if (receiver.buttonName.Contains("houlder") || receiver.buttonName.Contains("1"))
                            {
                                if (isUnsubscribing) { inputProcessor.OnRightShoulderUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                                else { inputProcessor.OnRightShoulderUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                            }
                            // Right Trigger (R2)
                            else if (receiver.buttonName.Contains("rigger") || receiver.buttonName.Contains("2"))
                            {
                                if (isUnsubscribing) { inputProcessor.OnRightTriggerUpdated -= (float value) => { boolReceiver.isPressed = value > 0.1f; }; }
                                else { inputProcessor.OnRightTriggerUpdated += (float value) => { boolReceiver.isPressed = value > 0.1f; }; }
                            }
                            // Right Stick (R3 and >0.1f)
                            else
                            {
                                if (receiver.buttonName.Contains("utton") || receiver.buttonName.Contains("3"))
                                {
                                    if (isUnsubscribing) { inputProcessor.OnRightStickButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                                    else { inputProcessor.OnRightStickButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                                }
                                else if (receiver.buttonName.Contains("X") || receiver.buttonName.Contains("orizontal"))
                                {
                                    if (isUnsubscribing) { inputProcessor.OnRightStickUpdated -= (Vector2 value) => { boolReceiver.isPressed = value.x > 0.1f; }; }
                                    else { inputProcessor.OnRightStickUpdated += (Vector2 value) => { boolReceiver.isPressed = value.x > 0.1f; }; }
                                }
                                else if (receiver.buttonName.Contains("Y") || receiver.buttonName.Contains("ertical"))
                                {
                                    if (isUnsubscribing) { inputProcessor.OnRightStickUpdated -= (Vector2 value) => { boolReceiver.isPressed = value.y > 0.1f; }; }
                                    else { inputProcessor.OnRightStickUpdated += (Vector2 value) => { boolReceiver.isPressed = value.y > 0.1f; }; }
                                }
                                else
                                {
                                    if (isUnsubscribing) { inputProcessor.OnRightStickUpdated -= (Vector2 value) => { boolReceiver.isPressed = value.magnitude > 0.1f; }; }
                                    else { inputProcessor.OnRightStickUpdated += (Vector2 value) => { boolReceiver.isPressed = value.magnitude > 0.1f; }; }
                                }
                            }
                        }

                        // Action Buttons
                        else if (receiver.buttonName.Contains("A") || receiver.buttonName.Contains("ross"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnAButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnAButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("B") || receiver.buttonName.Contains("ircle"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnAButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnBButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("X") || receiver.buttonName.Contains("quare"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnAButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnXButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("Y") || receiver.buttonName.Contains("riangle"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnAButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnYButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }

                        // Directional Buttons
                        else if (receiver.buttonName.Contains("orth"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnNorthButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnNorthButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("ast"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnEastButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnEastButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("outh"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnSouthButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnSouthButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("est"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnWestButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnWestButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }

                        // Start/Select Buttons
                        else if (receiver.buttonName.Contains("tart"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnStartButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnStartButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("elect") || receiver.buttonName.Contains("hare"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnSelectButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnSelectButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                    }

                    // D-Pad Buttons
                    else
                    {
                        if (receiver.buttonName.ToLower().Contains("up"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnNorthButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnNorthButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("ight"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnEastButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnEastButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("own"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnSouthButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnSouthButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                        else if (receiver.buttonName.Contains("eft"))
                        {
                            if (isUnsubscribing) { inputProcessor.OnWestButtonUpdated -= (bool value) => { boolReceiver.isPressed = value; }; }
                            else { inputProcessor.OnWestButtonUpdated += (bool value) => { boolReceiver.isPressed = value; }; }
                        }
                    }
                }
            }
        }
    }
}
