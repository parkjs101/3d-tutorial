using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

public class KeypadPasswordInputDisplay : MonoBehaviour
{
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
    private KeypadPasswordController passwordController;
    private Action onPasswordAccepted;

    public void Initialize(string correctPassword, Action passwordAccepted)
    {
        if (string.IsNullOrEmpty(correctPassword))
        {
            Debug.LogError("Keypad password cannot be empty.", this);
            return;
        }

        BuildView();
        passwordController = new KeypadPasswordController(correctPassword);
        onPasswordAccepted = passwordAccepted;
        RefreshText();
    }

    public void ResetInput()
    {
        if (passwordController == null)
        {
            return;
        }

        passwordController.Reset();
        SetResult(string.Empty);
        RefreshText();
    }

    private void SubmitDigit(int digit)
    {
        if (passwordController == null)
        {
            return;
        }

        KeypadPasswordController.SubmissionResult result = passwordController.SubmitDigit(digit);
        RefreshText();

        if (result == KeypadPasswordController.SubmissionResult.Accepted)
        {
            SetResult("pass");
            onPasswordAccepted?.Invoke();
        }
        else if (result == KeypadPasswordController.SubmissionResult.Rejected)
        {
            SetResult("wrong");
        }
    }

    private void RemoveLastDigit()
    {
        if (passwordController == null)
        {
            return;
        }

        passwordController.RemoveLastDigit();
        RefreshText();
    }

    void Update()
    {
        if (WasBackspacePressed())
        {
            RemoveLastDigit();
            return;
        }

        if (TryGetPressedDigit(out int digit))
        {
            SubmitDigit(digit);
        }
    }

    private void BuildView()
    {
        if (passwordText != null)
        {
            return;
        }

        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = new Vector2(1f, 0.5f);
        root.anchorMax = new Vector2(1f, 0.5f);
        root.pivot = new Vector2(1f, 0.5f);
        root.sizeDelta = new Vector2(400f, 88f);
        root.anchoredPosition = new Vector2(-48f, 0f);

        passwordText = CreateText("Password Text", root, new Vector2(0f, 20f), 30);
        resultText = CreateText("Password Result", root, new Vector2(0f, -20f), 28);
    }

    private Text CreateText(string objectName, Transform parent, Vector2 position, int fontSize)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(400f, 40f);
        rect.anchoredPosition = position;

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.red;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName);
        rectObject.layer = gameObject.layer;
        rectObject.transform.SetParent(parent, false);
        return rectObject.AddComponent<RectTransform>();
    }

    private void RefreshText()
    {
        if (passwordText != null && passwordController != null)
        {
            passwordText.text = $"password: {passwordController.EnteredDigits}";
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
