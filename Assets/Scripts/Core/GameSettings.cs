using System;
using UnityEngine;

/// <summary>
/// Player preferences that survive a restart. Field of view is horizontal, as
/// in the Godot version, so it means the same on any screen shape.
/// </summary>
public static class GameSettings
{
    public const float FovMin = 60f;
    public const float FovMax = 110f;
    public const float SensitivityMin = 0.3f;
    public const float SensitivityMax = 3f;

    public static event Action Changed;

    static float _fov = -1f;
    static float _sensitivity = -1f;
    static float _bob = -1f;

    /// Walking bob and landing dip strength, 0 (off) to 1.
    public static float CameraBob
    {
        get
        {
            if (_bob < 0f)
                _bob = PlayerPrefs.GetFloat("camera_bob", 1f);
            return _bob;
        }
        set
        {
            _bob = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("camera_bob", _bob);
            Changed?.Invoke();
        }
    }

    public static float HorizontalFov
    {
        get
        {
            if (_fov < 0f)
                _fov = PlayerPrefs.GetFloat("fov", 80f);
            return _fov;
        }
        set
        {
            _fov = Mathf.Clamp(value, FovMin, FovMax);
            PlayerPrefs.SetFloat("fov", _fov);
            Changed?.Invoke();
        }
    }

    public static float MouseSensitivity
    {
        get
        {
            if (_sensitivity < 0f)
                _sensitivity = PlayerPrefs.GetFloat("mouse_sensitivity", 1f);
            return _sensitivity;
        }
        set
        {
            _sensitivity = Mathf.Clamp(value, SensitivityMin, SensitivityMax);
            PlayerPrefs.SetFloat("mouse_sensitivity", _sensitivity);
            Changed?.Invoke();
        }
    }

    /// Show an English translation under customers' lines on calls. Separate from
    /// the interface language (which is English); off by default, because the
    /// languages on your CV are the point.
    public static bool TranslateCalls
    {
        get => PlayerPrefs.GetInt("translate_calls", 0) == 1;
        set
        {
            PlayerPrefs.SetInt("translate_calls", value ? 1 : 0);
            Changed?.Invoke();
        }
    }

    public static void Save() => PlayerPrefs.Save();
}
