using UnityEngine;
using UnityEngine.InputSystem;


namespace Assets.Scripts.scrible.Input
{

    public class GamepadInputReader : InputReader
    {

        public Gamepad Gamepad;
        

        void Awake()
        {
            Gamepad = Gamepad.current;
        }

        public Vector2 GetLeftStick()
        {
            return Gamepad.leftStick.ReadValue();
        }

        public Vector2 GetRightStick()
        {
            return Gamepad.rightStick.ReadValue();
        }

        public float GetLeftTrigger()
        {
            return Gamepad.leftTrigger.ReadValue();
        }

        public float GetRightTrigger()
        {
            return Gamepad.rightTrigger.ReadValue();
        }

        public bool GetLeftShoulder()           //1
        {
            return Gamepad.leftShoulder.IsPressed();
        }
        public bool GetRightShoulder()          //2
        {
            return Gamepad.rightShoulder.IsPressed();
        }
        public bool GetLeftStickButton()        //3
        {
            return Gamepad.leftStickButton.IsPressed();
        }
        public bool GetRightStickButton()       //4
        {
            return Gamepad.rightStickButton.IsPressed();
        }
        public bool GetAButton()                //5
        {
            return Gamepad.aButton.IsPressed();
        }
        public bool GetBButton()                //6
        {
            return Gamepad.bButton.IsPressed();
        }
        public bool GetXButton()                //7
        {
            return Gamepad.xButton.IsPressed();
        }
        public bool GetYButton()                //8
        {
            return Gamepad.yButton.IsPressed();
        }
        public bool GetNorthButton()            //9
        {
            return Gamepad.dpad.up.IsPressed();
        }
        public bool GetEastButton()             //10
        {
            return Gamepad.dpad.right.IsPressed();
        }
        public bool GetSouthButton()            //11
        {
            return Gamepad.dpad.down.IsPressed();
        }
        public bool GetWestButton()             //12
        {
            return Gamepad.dpad.left.IsPressed();
        }
        public bool GetStartButton()            //13
        {
            return Gamepad.startButton.IsPressed();
        }

    }
}