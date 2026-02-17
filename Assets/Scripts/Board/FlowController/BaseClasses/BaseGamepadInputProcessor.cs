using UnityEngine;

public class BaseGamepadInputProcessor : MonoBehaviour
{
    public virtual void ProcessGamepadInput(GamepadInput gamepadInput, float deltaTime) { }
}
