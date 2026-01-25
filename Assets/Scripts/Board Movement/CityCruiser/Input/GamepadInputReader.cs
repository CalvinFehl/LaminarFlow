using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Assets.Scripts.scrible.Input
{

    public class GamepadInputReader : InputReader
    {

        private Gamepad gamepad;
        public float xSensitivity = 1f;
        public float ySensitivity = 1f;
        public float zSensitivity = 1f;

        public Vector4 RotInput;

        void Start()
        {
            gamepad = Gamepad.current;
        }

        void Update()
        {
            RotInput = new Vector4(GetLeftStick().y * xSensitivity, GetRightStick().x * ySensitivity, GetLeftStick().x * -zSensitivity, GetRightStick().y);
        }

        public Vector2 GetLeftStick()
        {
            return gamepad.leftStick.ReadValue();
        }

        public Vector2 GetRightStick()
        {
            return gamepad.rightStick.ReadValue();
        }

        public float GetLeftTrigger()
        {
            return gamepad.leftTrigger.ReadValue();
        }

        public float GetRightTrigger()
        {
            return gamepad.rightTrigger.ReadValue();
        }

        public bool GetLeftShoulder()           //1
        {
            return gamepad.leftShoulder.IsPressed();
        }
        public bool GetRightShoulder()          //2
        {
            return gamepad.rightShoulder.IsPressed();
        }
        public bool GetLeftStickButton()        //3
        {
            return gamepad.leftStickButton.IsPressed();
        }
        public bool GetRightStickButton()       //4
        {
            return gamepad.rightStickButton.IsPressed();
        }
        public bool GetAButton()                //5
        {
            return gamepad.aButton.IsPressed();
        }
        public bool GetBButton()                //6
        {
            return gamepad.bButton.IsPressed();
        }
        public bool GetXButton()                //7
        {
            return gamepad.xButton.IsPressed();
        }
        public bool GetYButton()                //8
        {
            return gamepad.yButton.IsPressed();
        }
        public bool GetNorthButton()            //9
        {
            return gamepad.dpad.up.IsPressed();
        }
        public bool GetEastButton()             //10
        {
            return gamepad.dpad.right.IsPressed();
        }
        public bool GetSouthButton()            //11
        {
            return gamepad.dpad.down.IsPressed();
        }
        public bool GetWestButton()             //12
        {
            return gamepad.dpad.left.IsPressed();
        }
        public bool GetStartButton()            //13
        {
            return gamepad.startButton.IsPressed();
        }

    }
}