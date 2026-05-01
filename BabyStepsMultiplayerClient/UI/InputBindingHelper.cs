using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    internal static class InputBindingHelper
    {
        private static readonly bool CaptureLoggingEnabled = true;
        private const string RewiredActionPrefix = "RWACTION:";
        private const string RewiredButtonPrefix = "RWBTN:";
        private const string RewiredAxisPrefix = "RWAXIS:";
        private const float AxisThreshold = 0.5f;
        private const int MaxRawRewiredButtonId = 1023;

        private static readonly Dictionary<string, bool> _previousState = new Dictionary<string, bool>();
        private static readonly int[] _inputActionIds = BuildInputActionIdList();
        private static readonly KeyCode[] _joystickButtonKeyCodes = BuildJoystickButtonKeyCodes();

        public static bool IsInputHeldForCapture()
        {
            if (Input.anyKey)
                return true;

            // Some mapped Rewired actions can report held continuously; only
            // gate on physical joystick button KeyCodes to avoid deadlocking capture.
            for (int i = 0; i < _joystickButtonKeyCodes.Length; i++)
            {
                if (Input.GetKey(_joystickButtonKeyCodes[i]))
                    return true;
            }

            return false;
        }

        public static bool TryCapturePressedBinding(out string binding, out string displayName)
        {
            for (int i = 0; i < _joystickButtonKeyCodes.Length; i++)
            {
                var keyCode = _joystickButtonKeyCodes[i];
                if (!Input.GetKeyDown(keyCode))
                    continue;

                binding = keyCode.ToString();
                displayName = keyCode.ToString();
                LogCapture("joystick-keycode", binding, displayName);
                return true;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (keyCode == KeyCode.None)
                    continue;
                if (IsJoystickKeyCode(keyCode))
                    continue;
                if (!Input.GetKeyDown(keyCode))
                    continue;

                binding = keyCode.ToString();
                displayName = keyCode.ToString();
                LogCapture("keyboard", binding, displayName);
                return true;
            }

            var rwPlayer = Menu.me?.rwPlayer;
            if (rwPlayer != null)
            {
                for (int i = 0; i < _inputActionIds.Length; i++)
                {
                    int actionId = _inputActionIds[i];
                    if (!rwPlayer.GetButtonDown(actionId))
                        continue;

                    if (!TryGetInputActionName(actionId, out string actionName))
                        actionName = "Action_" + actionId;

                    if (IsAxisLikeAction(actionId, actionName))
                        continue;

                    binding = RewiredActionPrefix + actionName;
                    displayName = FormatActionDisplayName(actionName);
                    LogCapture("rewired-action", binding, displayName);
                    return true;
                }

                if (TryCaptureAxisDown(out binding, out displayName))
                    return true;

                for (int rawButtonId = 0; rawButtonId <= MaxRawRewiredButtonId; rawButtonId++)
                {
                    if (!rwPlayer.GetButtonDown(rawButtonId))
                        continue;

                    binding = RewiredButtonPrefix + rawButtonId;
                    displayName = GetRewiredButtonDisplayName(rawButtonId);
                    LogCapture("rewired-raw-button", binding, displayName);
                    return true;
                }
            }

            binding = null;
            displayName = null;
            return false;
        }

        public static bool IsPressed(string binding)
        {
            if (TryParseKeyCode(binding, out var keyCode))
                return Input.GetKey(keyCode);

            if (TryParseRewiredButton(binding, out int actionId))
                return Menu.me?.rwPlayer?.GetButton(actionId) == true;

            if (TryParseRewiredAction(binding, out string actionName)
                && TryGetInputActionId(actionName, out int actionNameId))
            {
                return Menu.me?.rwPlayer?.GetButton(actionNameId) == true;
            }

            if (TryParseRewiredAxis(binding, out int axisId, out int direction))
            {
                var rwPlayer = Menu.me?.rwPlayer;
                if (rwPlayer == null)
                    return false;

                float axis = rwPlayer.GetAxis(axisId);
                return direction > 0 ? axis > AxisThreshold : axis < -AxisThreshold;
            }

            return false;
        }

        public static bool IsDown(string binding)
        {
            if (TryParseKeyCode(binding, out var keyCode))
                return Input.GetKeyDown(keyCode);

            if (TryParseRewiredButton(binding, out int actionId))
                return Menu.me?.rwPlayer?.GetButtonDown(actionId) == true;

            if (TryParseRewiredAction(binding, out string actionName)
                && TryGetInputActionId(actionName, out int actionNameId))
            {
                return Menu.me?.rwPlayer?.GetButtonDown(actionNameId) == true;
            }

            if (TryParseRewiredAxis(binding, out _, out _))
            {
                bool current = IsPressed(binding);
                _previousState.TryGetValue(binding, out bool previous);
                _previousState[binding] = current;
                return current && !previous;
            }

            return false;
        }

        public static bool IsControllerBinding(string binding)
        {
            if (string.IsNullOrWhiteSpace(binding))
                return false;

            if (TryParseKeyCode(binding, out var keyCode))
                return IsJoystickKeyCode(keyCode);

            if (binding.StartsWith(RewiredActionPrefix, StringComparison.Ordinal)
                || binding.StartsWith(RewiredButtonPrefix, StringComparison.Ordinal)
                || binding.StartsWith(RewiredAxisPrefix, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        public static string GetDisplayName(string binding)
        {
            if (TryParseRewiredAction(binding, out string actionName))
                return FormatActionDisplayName(actionName);

            if (TryParseRewiredButton(binding, out int actionId))
                return GetRewiredButtonDisplayName(actionId);

            if (TryParseRewiredAxis(binding, out int axisId, out int direction))
            {
                if (axisId == (int)InputActions.UIVertical)
                    return direction > 0 ? "AXIS Up" : "AXIS Down";

                if (axisId == (int)InputActions.UIHorizontal)
                    return direction > 0 ? "AXIS Right" : "AXIS Left";
            }

            return binding;
        }

        private static bool TryParseKeyCode(string binding, out KeyCode keyCode)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                keyCode = KeyCode.None;
                return false;
            }

            if (!Enum.TryParse(binding, true, out keyCode))
                return false;

            return keyCode != KeyCode.None;
        }

        private static bool TryParseRewiredButton(string binding, out int actionId)
        {
            actionId = -1;
            if (string.IsNullOrWhiteSpace(binding) || !binding.StartsWith(RewiredButtonPrefix, StringComparison.Ordinal))
                return false;

            return int.TryParse(binding.Substring(RewiredButtonPrefix.Length), out actionId);
        }

        private static bool TryParseRewiredAction(string binding, out string actionName)
        {
            actionName = null;
            if (string.IsNullOrWhiteSpace(binding) || !binding.StartsWith(RewiredActionPrefix, StringComparison.Ordinal))
                return false;

            actionName = binding.Substring(RewiredActionPrefix.Length);
            return !string.IsNullOrWhiteSpace(actionName);
        }

        private static bool TryParseRewiredAxis(string binding, out int axisId, out int direction)
        {
            axisId = -1;
            direction = 0;
            if (string.IsNullOrWhiteSpace(binding) || !binding.StartsWith(RewiredAxisPrefix, StringComparison.Ordinal))
                return false;

            string payload = binding.Substring(RewiredAxisPrefix.Length);
            var parts = payload.Split(':');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out axisId))
                return false;

            if (parts[1] == "+")
            {
                direction = 1;
                return true;
            }

            if (parts[1] == "-")
            {
                direction = -1;
                return true;
            }

            return false;
        }

        private static bool TryCaptureAxisDown(out string binding, out string displayName)
        {
            var rwPlayer = Menu.me?.rwPlayer;
            if (rwPlayer == null)
            {
                binding = null;
                displayName = null;
                return false;
            }

            float x = rwPlayer.GetAxis((int)InputActions.UIHorizontal);
            float xp = rwPlayer.GetAxisPrev((int)InputActions.UIHorizontal);
            float y = rwPlayer.GetAxis((int)InputActions.UIVertical);
            float yp = rwPlayer.GetAxisPrev((int)InputActions.UIVertical);

            if (y > AxisThreshold && yp <= AxisThreshold)
            {
                binding = RewiredAxisPrefix + (int)InputActions.UIVertical + ":+";
                displayName = "AXIS Up";
                LogCapture("rewired-axis", binding, displayName);
                return true;
            }

            if (y < -AxisThreshold && yp >= -AxisThreshold)
            {
                binding = RewiredAxisPrefix + (int)InputActions.UIVertical + ":-";
                displayName = "AXIS Down";
                LogCapture("rewired-axis", binding, displayName);
                return true;
            }

            if (x > AxisThreshold && xp <= AxisThreshold)
            {
                binding = RewiredAxisPrefix + (int)InputActions.UIHorizontal + ":+";
                displayName = "AXIS Right";
                LogCapture("rewired-axis", binding, displayName);
                return true;
            }

            if (x < -AxisThreshold && xp >= -AxisThreshold)
            {
                binding = RewiredAxisPrefix + (int)InputActions.UIHorizontal + ":-";
                displayName = "AXIS Left";
                LogCapture("rewired-axis", binding, displayName);
                return true;
            }

            binding = null;
            displayName = null;
            return false;
        }

        private static string GetRewiredButtonDisplayName(int actionId)
        {
            if (Enum.IsDefined(typeof(InputActions), actionId))
                return ((InputActions)actionId).ToString();

            return "Controller Button " + actionId;
        }

        private static int[] BuildInputActionIdList()
        {
            var values = Enum.GetValues(typeof(InputActions));
            var ids = new List<int>(values.Length);
            var seen = new HashSet<int>();

            for (int i = 0; i < values.Length; i++)
            {
                int id = (int)values.GetValue(i);
                if (seen.Add(id))
                    ids.Add(id);
            }

            ids.Sort();
            return ids.ToArray();
        }

        private static bool TryGetInputActionName(int actionId, out string actionName)
        {
            actionName = null;
            if (!Enum.IsDefined(typeof(InputActions), actionId))
                return false;

            actionName = ((InputActions)actionId).ToString();
            return !string.IsNullOrWhiteSpace(actionName);
        }

        private static bool TryGetInputActionId(string actionName, out int actionId)
        {
            actionId = -1;
            if (string.IsNullOrWhiteSpace(actionName))
                return false;

            if (!Enum.TryParse(actionName, true, out InputActions parsed))
                return false;

            actionId = (int)parsed;
            return true;
        }

        private static bool IsAxisLikeAction(int actionId, string actionName)
        {
            if (actionId == (int)InputActions.UIHorizontal || actionId == (int)InputActions.UIVertical)
                return true;

            if (string.IsNullOrWhiteSpace(actionName))
                return false;

            return actionName.IndexOf("Horizontal", StringComparison.OrdinalIgnoreCase) >= 0
                || actionName.IndexOf("Vertical", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static KeyCode[] BuildJoystickButtonKeyCodes()
        {
            var list = new List<KeyCode>();

            for (int button = 0; button <= 19; button++)
            {
                if (Enum.TryParse("JoystickButton" + button, out KeyCode anyJoystick))
                    list.Add(anyJoystick);
            }

            for (int joystick = 1; joystick <= 8; joystick++)
            {
                for (int button = 0; button <= 19; button++)
                {
                    if (Enum.TryParse("Joystick" + joystick + "Button" + button, out KeyCode specificJoystick))
                        list.Add(specificJoystick);
                }
            }

            return list.ToArray();
        }

        private static bool IsJoystickKeyCode(KeyCode keyCode)
        {
            string name = keyCode.ToString();
            return name.IndexOf("Joystick", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatActionDisplayName(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return actionName;

            if (actionName.StartsWith("UI", StringComparison.OrdinalIgnoreCase))
                return "DPAD " + actionName.Substring(2);

            return actionName;
        }

        private static void LogCapture(string source, string binding, string displayName)
        {
            if (!CaptureLoggingEnabled)
                return;

            Core.DebugMsg($"[BindCapture] source={source} binding={binding} display={displayName}");
        }
    }
}
