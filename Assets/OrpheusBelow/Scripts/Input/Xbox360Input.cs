using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OrpheusBelow
{
    /// <summary>
    /// Comprehensive Xbox 360 & XInput Gamepad controller provider.
    /// Layout:
    ///   - Movement: Left Stick / D-Pad (WASD)
    ///   - Aiming: Right Stick (360° twin-stick isometric aiming)
    ///   - J (Soft Lyre Attack): Left Trigger (LT) - tap vs charge & release
    ///   - K (Instant Hard Orphic Boom): Right Trigger (RT) - edge triggered
    ///   - F (Aulos Flute Stance): Y button (North)
    ///   - Dash: A button (South), B button (East), or RB (Right Shoulder)
    ///   - Look-Back [Hold Q]: LB (Left Shoulder)
    ///   - X button: NOTHING (unbound per user request)
    ///   - Pause / Back: Start / Select buttons
    /// </summary>
    public static class Xbox360Input
    {
        private static int _lastUpdateFrame = -1;
        private static bool _ltPressedThisFrame;
        private static bool _ltHeldThisFrame;
        private static bool _ltReleasedThisFrame;
        private static bool _rtPressedThisFrame;
        private static bool _rtHeldThisFrame;
        private static bool _rtReleasedThisFrame;
        private static bool _wasLtDown;
        private static bool _wasRtDown;

        public const float TriggerPressThreshold = 0.20f;
        public const float TriggerReleaseThreshold = 0.12f;

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

        private static void UpdateTriggerStates()
        {
            if (Application.isPlaying && Time.frameCount == _lastUpdateFrame) return;
            _lastUpdateFrame = Time.frameCount;

            var pad = GetActiveGamepad();
            float lt = 0f;
            float rt = 0f;

            if (pad != null)
            {
                lt = pad.leftTrigger.ReadValue();
                rt = pad.rightTrigger.ReadValue();
            }

            // Stateful hysteresis to guarantee crisp press & release detection on analog triggers
            bool isLtDown = _wasLtDown ? (lt >= TriggerReleaseThreshold) : (lt >= TriggerPressThreshold);
            bool isRtDown = _wasRtDown ? (rt >= TriggerReleaseThreshold) : (rt >= TriggerPressThreshold);

            _ltPressedThisFrame = isLtDown && !_wasLtDown;
            _ltHeldThisFrame = isLtDown;
            _ltReleasedThisFrame = !isLtDown && _wasLtDown;

            _rtPressedThisFrame = isRtDown && !_wasRtDown;
            _rtHeldThisFrame = isRtDown;
            _rtReleasedThisFrame = !isRtDown && _wasRtDown;

            _wasLtDown = isLtDown;
            _wasRtDown = isRtDown;
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
        /// J (Soft Lyre Attack): Left Trigger (LT) pressed this frame.
        /// Starts lyre tap or charge. (X button does nothing).
        /// </summary>
        public static bool SoftAttackPressed
        {
            get
            {
                UpdateTriggerStates();
                return _ltPressedThisFrame;
            }
        }

        /// <summary>
        /// J (Soft Lyre Attack): Left Trigger (LT) held down.
        /// Builds charge while pulled.
        /// </summary>
        public static bool SoftAttackHeld
        {
            get
            {
                UpdateTriggerStates();
                return _ltHeldThisFrame;
            }
        }

        /// <summary>
        /// J (Soft Lyre Attack): Left Trigger (LT) released this frame.
        /// Releases lyre note pulse.
        /// </summary>
        public static bool SoftAttackReleased
        {
            get
            {
                UpdateTriggerStates();
                return _ltReleasedThisFrame;
            }
        }

        /// <summary>
        /// K (Instant Hard Orphic Boom): Right Trigger (RT) pressed this frame.
        /// Edge-triggered: fires once per pull. (Y button mapped to F).
        /// </summary>
        public static bool HardBoomPressed
        {
            get
            {
                UpdateTriggerStates();
                return _rtPressedThisFrame;
            }
        }

        /// <summary>
        /// F (Aulos Flute Stance Toggle): Y button (North).
        /// </summary>
        public static bool FluteTogglePressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonNorth.wasPressedThisFrame;
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
        /// Interaction (e.g. Ambrosia Well): A button (South).
        /// X button is unbound.
        /// </summary>
        public static bool InteractPressed
        {
            get
            {
                var pad = GetActiveGamepad();
                if (pad == null) return false;
                return pad.buttonSouth.wasPressedThisFrame;
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
