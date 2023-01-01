using System;
using System.Collections.Generic;
using ManagedDoom.UserInput;
using UnityEngine;

namespace ManagedDoom.Unity
{
    public class UnityUserInput : IUserInput, IDisposable
    {

        private Config config;

        private bool[] weaponKeys;
        private int turnHeld;

        private bool mouseGrabbed;
        private float mouseX;
        private float mouseY;
        private float mousePrevX;
        private float mousePrevY;
        private float mouseDeltaX;
        private float mouseDeltaY;

        private List<KeyCode> keysPressed = new ();

        public UnityUserInput(Config config, UnityDoom doom, bool useMouse)
        {
            try
            {
                this.config = config;

                weaponKeys = new bool[7];
                turnHeld = 0;

                // if (useMouse)
                // {
                //     mouse = input.Mice[0];
                //     mouseGrabbed = false;
                // }
            }
            catch (Exception e)
            {
                Dispose();
                throw(e);
            }
        }

        public void BuildTicCmd(TicCmd cmd)
        {
            if (keysPressed.Count == 0)
            {
                cmd.Clear();
                return;
            }

            var keyForward =     IsPressed(keysPressed, config.key_forward);
            var keyBackward =    IsPressed(keysPressed, config.key_backward);
            var keyStrafeLeft =  IsPressed(keysPressed, config.key_strafeleft);
            var keyStrafeRight = IsPressed(keysPressed, config.key_straferight);
            var keyTurnLeft =    IsPressed(keysPressed, config.key_turnleft);
            var keyTurnRight =   IsPressed(keysPressed, config.key_turnright);
            var keyFire =        IsPressed(keysPressed, config.key_fire) || keysPressed.Contains(KeyCode.F);
            var keyUse =         IsPressed(keysPressed, config.key_use);
            var keyRun =         IsPressed(keysPressed, config.key_run);
            var keyStrafe =      IsPressed(keysPressed, config.key_strafe);

            weaponKeys[0] = keysPressed.Contains(KeyCode.Alpha1);
            weaponKeys[1] = keysPressed.Contains(KeyCode.Alpha2);
            weaponKeys[2] = keysPressed.Contains(KeyCode.Alpha3);
            weaponKeys[3] = keysPressed.Contains(KeyCode.Alpha4);
            weaponKeys[4] = keysPressed.Contains(KeyCode.Alpha5);
            weaponKeys[5] = keysPressed.Contains(KeyCode.Alpha6);
            weaponKeys[6] = keysPressed.Contains(KeyCode.Alpha7);

            cmd.Clear();

            var strafe = keyStrafe;
            var speed = keyRun ? 1 : 0;
            var forward = 0;
            var side = 0;

            if (config.game_alwaysrun)
            {
                speed = 1 - speed;
            }

            if (keyTurnLeft || keyTurnRight)
            {
                turnHeld++;
            }
            else
            {
                turnHeld = 0;
            }

            int turnSpeed;
            if (turnHeld < PlayerBehavior.SlowTurnTics)
            {
                turnSpeed = 2;
            }
            else
            {
                turnSpeed = speed;
            }

            if (strafe)
            {
                if (keyTurnRight)
                {
                    side += PlayerBehavior.SideMove[speed];
                }

                if (keyTurnLeft)
                {
                    side -= PlayerBehavior.SideMove[speed];
                }
            }
            else
            {
                if (keyTurnRight)
                {
                    cmd.AngleTurn -= (short)PlayerBehavior.AngleTurn[turnSpeed];
                }

                if (keyTurnLeft)
                {
                    cmd.AngleTurn += (short)PlayerBehavior.AngleTurn[turnSpeed];
                }
            }

            if (keyForward)
            {
                forward += PlayerBehavior.ForwardMove[speed];
            }

            if (keyBackward)
            {
                forward -= PlayerBehavior.ForwardMove[speed];
            }

            if (keyStrafeLeft)
            {
                side -= PlayerBehavior.SideMove[speed];
            }

            if (keyStrafeRight)
            {
                side += PlayerBehavior.SideMove[speed];
            }

            if (keyFire)
            {
                cmd.Buttons |= TicCmdButtons.Attack;
            }

            if (keyUse)
            {
                cmd.Buttons |= TicCmdButtons.Use;
            }

            // Check weapon keys.
            for (var i = 0; i < weaponKeys.Length; i++)
            {
                if (weaponKeys[i])
                {
                    cmd.Buttons |= TicCmdButtons.Change;
                    cmd.Buttons |= (byte)(i << TicCmdButtons.WeaponShift);
                    break;
                }
            }

            UpdateMouse();
            var ms = 0.5F * config.mouse_sensitivity;
            var mx = (int)MathF.Round(ms * mouseDeltaX);
            var my = (int)MathF.Round(ms * -mouseDeltaY);
            forward += my;
            if (strafe)
            {
                side += mx * 2;
            }
            else
            {
                cmd.AngleTurn -= (short)(mx * 0x8);
            }

            if (forward > PlayerBehavior.MaxMove)
            {
                forward = PlayerBehavior.MaxMove;
            }
            else if (forward < -PlayerBehavior.MaxMove)
            {
                forward = -PlayerBehavior.MaxMove;
            }

            if (side > PlayerBehavior.MaxMove)
            {
                side = PlayerBehavior.MaxMove;
            }
            else if (side < -PlayerBehavior.MaxMove)
            {
                side = -PlayerBehavior.MaxMove;
            }

            cmd.ForwardMove += (sbyte)forward;
            cmd.SideMove += (sbyte)side;
        }

        private bool IsPressed(List<KeyCode> keyboardKeys, KeyBinding keyBinding)
        {
            foreach (var key in keyBinding.Keys)
            {
                if (keyboardKeys.Contains(DoomToUnityKey(key)))
                {
                    return true;
                }
            }

            // if (mouseGrabbed)
            // {
            //     foreach (var mouseButton in keyBinding.MouseButtons)
            //     {
            //         if (mouse.IsButtonPressed((MouseButton)mouseButton))
            //         {
            //             return true;
            //         }
            //     }
            // }

            return false;
        }

        public void Reset()
        {
            // if (mouse == null)
            // {
            //     return;
            // }
            //
            // mouseX = mouse.Position.X;
            // mouseY = mouse.Position.Y;
            // mousePrevX = mouseX;
            // mousePrevY = mouseY;
            // mouseDeltaX = 0;
            // mouseDeltaY = 0;
        }

        public void GrabMouse()
        {
            // if (mouse == null)
            // {
            //     return;
            // }
            //
            // if (!mouseGrabbed)
            // {
            //     mouse.Cursor.CursorMode = CursorMode.Raw;
            //     mouseGrabbed = true;
            //     mouseX = mouse.Position.X;
            //     mouseY = mouse.Position.Y;
            //     mousePrevX = mouseX;
            //     mousePrevY = mouseY;
            //     mouseDeltaX = 0;
            //     mouseDeltaY = 0;
            // }
        }

        public void ReleaseMouse()
        {
            // if (mouse == null)
            // {
            //     return;
            // }
            //
            // if (mouseGrabbed)
            // {
            //     mouse.Cursor.CursorMode = CursorMode.Normal;
            //     mouse.Position = new Vector2(window.Size.X - 10, window.Size.Y - 10);
            //     mouseGrabbed = false;
            // }
        }

        private void UpdateMouse()
        {
            // if (mouse == null)
            // {
            //     return;
            // }
            //
            // if (mouseGrabbed)
            // {
            //     mousePrevX = mouseX;
            //     mousePrevY = mouseY;
            //     mouseX = mouse.Position.X;
            //     mouseY = mouse.Position.Y;
            //     mouseDeltaX = mouseX - mousePrevX;
            //     mouseDeltaY = mouseY - mousePrevY;
            // }
        }

        public void Dispose()
        {

        }
        
        public static KeyCode DoomToUnityKey(DoomKey key)
        {
            switch (key)
            {
                case DoomKey.A: return KeyCode.A;
                case DoomKey.B: return KeyCode.B;
                case DoomKey.C: return KeyCode.C;
                case DoomKey.D: return KeyCode.D;
                case DoomKey.E: return KeyCode.E;
                case DoomKey.F: return KeyCode.F;
                case DoomKey.G: return KeyCode.G;
                case DoomKey.H: return KeyCode.H;
                case DoomKey.I: return KeyCode.I;
                case DoomKey.J: return KeyCode.J;
                case DoomKey.K: return KeyCode.K;
                case DoomKey.L: return KeyCode.L;
                case DoomKey.M: return KeyCode.M;
                case DoomKey.N: return KeyCode.N;
                case DoomKey.O: return KeyCode.O;
                case DoomKey.P: return KeyCode.P;
                case DoomKey.Q: return KeyCode.Q;
                case DoomKey.R: return KeyCode.R;
                case DoomKey.S: return KeyCode.S;
                case DoomKey.T: return KeyCode.T;
                case DoomKey.U: return KeyCode.U;
                case DoomKey.V: return KeyCode.V;
                case DoomKey.W: return KeyCode.W;
                case DoomKey.X: return KeyCode.X;
                case DoomKey.Y: return KeyCode.Y;
                case DoomKey.Z: return KeyCode.Z;
                case DoomKey.Num0: return KeyCode.Alpha0;
                case DoomKey.Num1: return KeyCode.Alpha1;
                case DoomKey.Num2: return KeyCode.Alpha2;
                case DoomKey.Num3: return KeyCode.Alpha3;
                case DoomKey.Num4: return KeyCode.Alpha4;
                case DoomKey.Num5: return KeyCode.Alpha5;
                case DoomKey.Num6: return KeyCode.Alpha6;
                case DoomKey.Num7: return KeyCode.Alpha7;
                case DoomKey.Num8: return KeyCode.Alpha8;
                case DoomKey.Num9: return KeyCode.Alpha9;
                case DoomKey.Escape: return KeyCode.Escape;
                case DoomKey.LControl: return KeyCode.LeftControl;
                case DoomKey.LShift: return KeyCode.LeftShift;
                case DoomKey.LAlt: return KeyCode.LeftAlt;
                // case DoomKey.LSystem: return Key.LSystem;
                case DoomKey.RControl: return KeyCode.RightControl;
                case DoomKey.RShift: return KeyCode.RightShift;
                case DoomKey.RAlt: return KeyCode.RightAlt;
                // case DoomKey.RSystem: return Key.RSystem;
                case DoomKey.Menu: return KeyCode.Menu;
                case DoomKey.LBracket: return KeyCode.LeftBracket;
                case DoomKey.RBracket: return KeyCode.RightBracket;
                case DoomKey.Semicolon: return KeyCode.Semicolon;
                case DoomKey.Comma: return KeyCode.Comma;
                case DoomKey.Period: return KeyCode.Period;
                // case DoomKey.Quote: return Key.Quote;
                case DoomKey.Slash: return KeyCode.Slash;
                case DoomKey.Backslash: return KeyCode.Backslash;
                // case DoomKey.Tilde: return Key.Tilde;
                case DoomKey.Equal: return KeyCode.Equals;
                // case DoomKey.Hyphen: return Key.Hyphen;
                case DoomKey.Space: return KeyCode.Space;
                case DoomKey.Enter: return KeyCode.Return;
                case DoomKey.Backspace: return KeyCode.Backspace;
                case DoomKey.Tab: return KeyCode.Tab;
                case DoomKey.PageUp: return KeyCode.PageUp;
                case DoomKey.PageDown: return KeyCode.PageDown;
                case DoomKey.End: return KeyCode.End;
                case DoomKey.Home: return KeyCode.Home;
                case DoomKey.Insert: return KeyCode.Insert;
                case DoomKey.Delete: return KeyCode.Delete;
                case DoomKey.Add: return KeyCode.KeypadPlus;
                case DoomKey.Subtract: return KeyCode.KeypadMinus;
                case DoomKey.Multiply: return KeyCode.KeypadMultiply;
                case DoomKey.Divide: return KeyCode.KeypadDivide;
                case DoomKey.Left: return KeyCode.LeftArrow;
                case DoomKey.Right: return KeyCode.RightArrow;
                case DoomKey.Up: return KeyCode.UpArrow;
                case DoomKey.Down: return KeyCode.DownArrow;
                case DoomKey.Numpad0: return KeyCode.Keypad0;
                case DoomKey.Numpad1: return KeyCode.Keypad1;
                case DoomKey.Numpad2: return KeyCode.Keypad2;
                case DoomKey.Numpad3: return KeyCode.Keypad3;
                case DoomKey.Numpad4: return KeyCode.Keypad4;
                case DoomKey.Numpad5: return KeyCode.Keypad5;
                case DoomKey.Numpad6: return KeyCode.Keypad6;
                case DoomKey.Numpad7: return KeyCode.Keypad7;
                case DoomKey.Numpad8: return KeyCode.Keypad8;
                case DoomKey.Numpad9: return KeyCode.Keypad9;
                case DoomKey.F1: return KeyCode.F1;
                case DoomKey.F2: return KeyCode.F2;
                case DoomKey.F3: return KeyCode.F3;
                case DoomKey.F4: return KeyCode.F4;
                case DoomKey.F5: return KeyCode.F5;
                case DoomKey.F6: return KeyCode.F6;
                case DoomKey.F7: return KeyCode.F7;
                case DoomKey.F8: return KeyCode.F8;
                case DoomKey.F9: return KeyCode.F9;
                case DoomKey.F10: return KeyCode.F10;
                case DoomKey.F11: return KeyCode.F11;
                case DoomKey.F12: return KeyCode.F12;
                case DoomKey.F13: return KeyCode.F13;
                case DoomKey.F14: return KeyCode.F14;
                case DoomKey.F15: return KeyCode.F15;
                case DoomKey.Pause: return KeyCode.Pause;
                default: return KeyCode.None;
            }
        }

        public static DoomKey UnityToDoomKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Space: return DoomKey.Space;
                case KeyCode.Comma: return DoomKey.Comma;
                case KeyCode.Minus: return DoomKey.Subtract;
                case KeyCode.Period: return DoomKey.Period;
                case KeyCode.Slash: return DoomKey.Slash;
                case KeyCode.Alpha0: return DoomKey.Num0;
                case KeyCode.Alpha1: return DoomKey.Num1;
                case KeyCode.Alpha2: return DoomKey.Num2;
                case KeyCode.Alpha3: return DoomKey.Num3;
                case KeyCode.Alpha4: return DoomKey.Num4;
                case KeyCode.Alpha5: return DoomKey.Num5;
                case KeyCode.Alpha6: return DoomKey.Num6;
                case KeyCode.Alpha7: return DoomKey.Num7;
                case KeyCode.Alpha8: return DoomKey.Num8;
                case KeyCode.Alpha9: return DoomKey.Num9;
                case KeyCode.Semicolon: return DoomKey.Semicolon;
                case KeyCode.KeypadEquals: return DoomKey.Equal;
                case KeyCode.A: return DoomKey.A;
                case KeyCode.B: return DoomKey.B;
                case KeyCode.C: return DoomKey.C;
                case KeyCode.D: return DoomKey.D;
                case KeyCode.E: return DoomKey.E;
                case KeyCode.F: return DoomKey.F;
                case KeyCode.G: return DoomKey.G;
                case KeyCode.H: return DoomKey.H;
                case KeyCode.I: return DoomKey.I;
                case KeyCode.J: return DoomKey.J;
                case KeyCode.K: return DoomKey.K;
                case KeyCode.L: return DoomKey.L;
                case KeyCode.M: return DoomKey.M;
                case KeyCode.N: return DoomKey.N;
                case KeyCode.O: return DoomKey.O;
                case KeyCode.P: return DoomKey.P;
                case KeyCode.Q: return DoomKey.Q;
                case KeyCode.R: return DoomKey.R;
                case KeyCode.S: return DoomKey.S;
                case KeyCode.T: return DoomKey.T;
                case KeyCode.U: return DoomKey.U;
                case KeyCode.V: return DoomKey.V;
                case KeyCode.W: return DoomKey.W;
                case KeyCode.X: return DoomKey.X;
                case KeyCode.Y: return DoomKey.Y;
                case KeyCode.Z: return DoomKey.Z;
                case KeyCode.LeftBracket: return DoomKey.LBracket;
                case KeyCode.Backslash: return DoomKey.Backslash;
                case KeyCode.RightBracket: return DoomKey.RBracket;
                case KeyCode.Escape: return DoomKey.Escape;
                case KeyCode.Return: return DoomKey.Enter;
                case KeyCode.Tab: return DoomKey.Tab;
                case KeyCode.Backspace: return DoomKey.Backspace;
                case KeyCode.Insert: return DoomKey.Insert;
                case KeyCode.Delete: return DoomKey.Delete;
                case KeyCode.RightArrow: return DoomKey.Right;
                case KeyCode.LeftArrow: return DoomKey.Left;
                case KeyCode.DownArrow: return DoomKey.Down;
                case KeyCode.UpArrow: return DoomKey.Up;
                case KeyCode.PageUp: return DoomKey.PageUp;
                case KeyCode.PageDown: return DoomKey.PageDown;
                case KeyCode.Home: return DoomKey.Home;
                case KeyCode.End: return DoomKey.End;
                case KeyCode.Pause: return DoomKey.Pause;
                case KeyCode.F1: return DoomKey.F1;
                case KeyCode.F2: return DoomKey.F2;
                case KeyCode.F3: return DoomKey.F3;
                case KeyCode.F4: return DoomKey.F4;
                case KeyCode.F5: return DoomKey.F5;
                case KeyCode.F6: return DoomKey.F6;
                case KeyCode.F7: return DoomKey.F7;
                case KeyCode.F8: return DoomKey.F8;
                case KeyCode.F9: return DoomKey.F9;
                case KeyCode.F10: return DoomKey.F10;
                case KeyCode.F11: return DoomKey.F11;
                case KeyCode.F12: return DoomKey.F12;
                case KeyCode.F13: return DoomKey.F13;
                case KeyCode.F14: return DoomKey.F14;
                case KeyCode.F15: return DoomKey.F15;
                case KeyCode.Keypad0: return DoomKey.Numpad0;
                case KeyCode.Keypad1: return DoomKey.Numpad1;
                case KeyCode.Keypad2: return DoomKey.Numpad2;
                case KeyCode.Keypad3: return DoomKey.Numpad3;
                case KeyCode.Keypad4: return DoomKey.Numpad4;
                case KeyCode.Keypad5: return DoomKey.Numpad5;
                case KeyCode.Keypad6: return DoomKey.Numpad6;
                case KeyCode.Keypad7: return DoomKey.Numpad7;
                case KeyCode.Keypad8: return DoomKey.Numpad8;
                case KeyCode.Keypad9: return DoomKey.Numpad9;
                case KeyCode.KeypadDivide: return DoomKey.Divide;
                case KeyCode.KeypadMultiply: return DoomKey.Multiply;
                case KeyCode.KeypadMinus: return DoomKey.Subtract;
                case KeyCode.KeypadPlus: return DoomKey.Add;
                case KeyCode.KeypadEnter: return DoomKey.Enter;
                case KeyCode.LeftShift: return DoomKey.LShift;
                case KeyCode.LeftControl: return DoomKey.LControl;
                case KeyCode.LeftAlt: return DoomKey.LAlt;
                // case Key.SuperLeft: return DoomKey.SuperLeft;
                case KeyCode.RightShift: return DoomKey.RShift;
                case KeyCode.RightControl: return DoomKey.RControl;
                case KeyCode.RightAlt: return DoomKey.RAlt;
                // case Key.SuperRight: return DoomKey.SuperRight;
                case KeyCode.Menu: return DoomKey.Menu;
                default: return DoomKey.Unknown;
            }
        }

        public int MaxMouseSensitivity
        {
            get { return 15; }
        }

        public int MouseSensitivity
        {
            get { return config.mouse_sensitivity; }

            set { config.mouse_sensitivity = value; }
        }

        public List<KeyCode> KeysPressed
        {
            get => keysPressed;
            set => keysPressed = value;
        }
    }

}