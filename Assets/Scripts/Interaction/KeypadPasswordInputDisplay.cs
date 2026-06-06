using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

public class KeypadPasswordInputDisplay : MonoBehaviour
{
    private const int MaxPasswordLength = 8;
    private const string CorrectPassword = "12345678";

#if ENABLE_INPUT_SYSTEM
    private static readonly Key[] TopRowDigitKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    private static readonly Key[] NumpadDigitKeys =
    {
        Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
        Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
    };
#endif

    private Text passwordText;
    private Text resultText;
    private string enteredDigits = string.Empty;
    private Action onPasswordAccepted;
    private bool isPasswordAccepted;

    public static KeypadPasswordInputDisplay Create(RectTransform parent)
    {
        GameObject displayObject = new GameObject("Password Input Display");
        displayObject.transform.SetParent(parent, false);

        RectTransform displayRect = displayObject.AddComponent<RectTransform>();
        displayRect.anchorMin = new Vector2(1f, 0.5f);
        displayRect.anchorMax = new Vector2(1f, 0.5f);
        displayRect.pivot = new Vector2(1f, 0.5f);
        displayRect.sizeDelta = new Vector2(400f, 72f);
        displayRect.anchoredPosition = new Vector2(-48f, 0f);

        Text text = displayObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.red;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;

        GameObject resultObject = new GameObject("Password Result");
        resultObject.transform.SetParent(displayObject.transform, false);

        RectTransform resultRect = resultObject.AddComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0f, 0f);
        resultRect.anchorMax = new Vector2(1f, 0f);
        resultRect.pivot = new Vector2(1f, 1f);
        resultRect.sizeDelta = new Vector2(0f, 48f);
        resultRect.anchoredPosition = new Vector2(0f, -8f);

        Text result = resultObject.AddComponent<Text>();
        result.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        result.fontSize = 28;
        result.alignment = TextAnchor.UpperRight;
        result.color = Color.red;
        result.raycastTarget = false;

        KeypadPasswordInputDisplay display = displayObject.AddComponent<KeypadPasswordInputDisplay>();
        display.passwordText = text;
        display.resultText = result;
        display.RefreshText();
        return display;
    }

    public void Initialize(Action passwordAccepted)
    {
        onPasswordAccepted = passwordAccepted;
    }

    public void ResetInput()
    {
        enteredDigits = string.Empty;
        isPasswordAccepted = false;
        SetResult(string.Empty);
        RefreshText();
    }

    void Update()
    {
        if (isPasswordAccepted)
        {
            return;
        }

        if (WasBackspacePressed())
        {
            RemoveLastDigit();
            return;
        }

        if (enteredDigits.Length < MaxPasswordLength && TryGetPressedDigit(out int digit))
        {
            enteredDigits += digit.ToString();
            RefreshText();

            if (enteredDigits.Length == MaxPasswordLength)
            {
                ValidatePassword();
            }
        }
    }

    private void ValidatePassword()
    {
        if (enteredDigits == CorrectPassword)
        {
            isPasswordAccepted = true;
            SetResult("pass");
            onPasswordAccepted?.Invoke();
            return;
        }

        enteredDigits = string.Empty;
        RefreshText();
        SetResult("wrong");
    }

    private void RemoveLastDigit()
    {
        if (enteredDigits.Length == 0)
        {
            return;
        }

        enteredDigits = enteredDigits.Substring(0, enteredDigits.Length - 1);
        RefreshText();
    }

    private void RefreshText()
    {
        if (passwordText != null)
        {
            passwordText.text = $"password: {enteredDigits}";
        }
    }

    private void SetResult(string result)
    {
        if (resultText != null)
        {
            resultText.text = result;
        }
    }

    private bool WasBackspacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Backspace);
#endif
    }

    private bool TryGetPressedDigit(out int digit)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            for (int i = 0; i < TopRowDigitKeys.Length; i++)
            {
                if (keyboard[TopRowDigitKeys[i]].wasPressedThisFrame
                    || keyboard[NumpadDigitKeys[i]].wasPressedThisFrame)
                {
                    digit = i;
                    return true;
                }
            }
        }
#else
        for (int i = 0; i <= 9; i++)
        {
            KeyCode topRowKey = (KeyCode)((int)KeyCode.Alpha0 + i);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad0 + i);
            if (Input.GetKeyDown(topRowKey) || Input.GetKeyDown(keypadKey))
            {
                digit = i;
                return true;
            }
        }
#endif

        digit = -1;
        return false;
    }
}
