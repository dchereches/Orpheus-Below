using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OrpheusBelow
{
    /// <summary>
    /// Comprehensive Xbox 360 & XInput Gamepad controller provider.
    /// Provides unified stick movement, twin-stick 360-degree aiming, dodge-dash,
    /// soft lyre attack, instant hard booms, aulos flute stance, look-back blend,
    /// and full menu navigation support across all connected gamepads.
    /// </summary>
    public static class Xbox360Input
    {
        /// <summary>
        /// Finds the active Xbox 360 / XInput controller, searching Gamepad.current
        /// and scanning Gamepad.all for any connected controller with input activity.
        /// </summary>
        public static Gamepad GetActiveGamepad()
        {
            var current = Gamepad.current;
            if (current != null && (current.wasUpdatedThisFrame || HasInputActivity(current)))
            {
                return current;
            }

            var all = Gamepad.all;
            for (int i = 0; i < all.Count; i++)
            {
                var pad = all[i];
                if (pad != null && (pad.wasUpdatedThisFrame || HasInputActivity(pad)))
                {
                    return pad;
                }
            }

            return current ?? (all.Count > 0 ? all[0] : null);
        }

        private static bool HasInputActivity(Gamepad pad)
        {
            if (pad == null) return false;
            if (pad.leftStick.ReadValue().sqrMagnitude > 0.04f) return true;
            if (pad.rightStick.ReadValue().sqrMagnitude > 0.04f) return true;
            if (pad.dpad.ReadValue().sqrMagnitude > 0.04f) return true;
            if (pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.buttonNorth.isPressed) return true;
            if (pad.leftShoulder.isPressed || pad.rightShoulder.isPressed) return true;
            if (pad.leftTrigger.ReadValue() > 0.15f || pad.rightTrigger.ReadValue() > 0.15f) return true;
            if (pad.startButton.isPressed || pad.selectButton.isPressed) return true;
            return false;
        }

        /// <summary>
        /// Reads directional movement from Left Stick or D-Pad (clamped to unit length).
        /// </summary>
        public static Vector2 Move
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return Vector2.zero;

                Vector2 stick = pad.leftStick.ReadValue();
                Vector2 dpad = pad.dpad.ReadValue();

                Vector2 combined = stick.sqrMagnitude > 0.04f ? stick : dpad;
                return Vector2.ClampMagnitude(combined, 1f);
            }
        }

        /// <summary>
        /// Reads twin-stick aiming vector from Right Stick.
        /// Returns Vector2.zero if deadzone is not exceeded.
        /// </summary>
        public static Vector2 Aim
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return Vector2.zero;

                Vector2 right = pad.rightStick.ReadValue();
                return right.sqrMagnitude > 0.08f ? Vector2.ClampMagnitude(right, 1f) : Vector2.zero;
            }
        }

        /// <summary>
        /// Dash / Dodge: A button (South), B button (East), or Right Bumper (RB).
        /// </summary>
        public static bool DashPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonSouth.wasPressedThisFrame
                    || pad.buttonEast.wasPressedThisFrame
                    || pad.rightShoulder.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// Soft Lyre Attack: X button (West) or Right Bumper.
        /// </summary>
        public static bool SoftAttackPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonWest.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
            }
        }

        public static bool SoftAttackHeld
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonWest.isPressed || pad.rightShoulder.isPressed;
            }
        }

        public static bool SoftAttackReleased
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonWest.wasReleasedThisFrame || pad.rightShoulder.wasReleasedThisFrame;
            }
        }

        /// <summary>
        /// Instant Hard Boom: Y button (North) or Right Trigger (RT).
        /// </summary>
        public static bool HardBoomPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonNorth.wasPressedThisFrame
                    || pad.rightTrigger.wasPressedThisFrame
                    || pad.rightTrigger.ReadValue() > 0.55f;
            }
        }

        /// <summary>
        /// Flute / Aulos Stance Toggle: Left Trigger (LT) or Y button.
        /// </summary>
        public static bool FluteTogglePressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.leftTrigger.wasPressedThisFrame || pad.leftTrigger.ReadValue() > 0.55f;
            }
        }

        /// <summary>
        /// Look-Back at Eurydice (Hold Q): Left Shoulder (LB).
        /// </summary>
        public static bool LookBackHeld
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.leftShoulder.isPressed;
            }
        }

        /// <summary>
        /// Pause / Escape Menu: Start button or Back button.
        /// </summary>
        public static bool PausePressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// Menu / Dialogue Back: B button (East).
        /// </summary>
        public static bool BackPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonEast.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// Menu / Dialogue Confirm: A button (South).
        /// </summary>
        public static bool ConfirmPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonSouth.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// Help / Controls card toggle: Back button (Select).
        /// </summary>
        public static bool HelpPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.selectButton.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// Interaction (e.g. Ambrosia Well): A button, X button, or Y button.
        /// </summary>
        public static bool InteractPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonSouth.wasPressedThisFrame
                    || pad.buttonWest.wasPressedThisFrame
                    || pad.buttonNorth.wasPressedThisFrame
                    || pad.rightTrigger.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// Skip dialogue / cutscene hold button.
        /// </summary>
        public static ButtonControl GetSkipHoldButton()
        {
            var pad = GetActiveGamepad();
            if (pad == null) return null;
            if (pad.buttonEast.isPressed) return pad.buttonEast;
            if (pad.buttonSouth.isPressed) return pad.buttonSouth;
            if (pad.startButton.isPressed) return pad.startButton;
            return null;
        }
    }
}
