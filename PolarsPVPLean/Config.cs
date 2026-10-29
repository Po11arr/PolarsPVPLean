using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PolarsPVPLean.Settings;
using PolarsPVPLean.Settings.Elements;
using PolarsPVPLean.Settings.Tools;
using VRage.Input;

namespace PolarsPVPLean;

public class Config : INotifyPropertyChanged
{
    #region Options

    private bool enabled = true;
    private Binding leanLeft = new(MyKeys.Q);
    private Binding leanRight = new(MyKeys.E);
    private bool toggleMode;
    private float leanDistance = 0.5f;
    private float tiltAngle = 10f;

    #endregion

    #region User interface

    public readonly string Title = "Polar's PVP Lean";

    [Checkbox(description: "Lean on foot with the keys below")]
    public bool Enabled
    {
        get => enabled;
        set => SetField(ref enabled, value);
    }

    [Keybind(label: "Lean left", description: "Right click to unbind")]
    public Binding LeanLeft
    {
        get => leanLeft;
        set => SetField(ref leanLeft, value);
    }

    [Keybind(label: "Lean right", description: "Right click to unbind")]
    public Binding LeanRight
    {
        get => leanRight;
        set => SetField(ref leanRight, value);
    }

    [Checkbox(label: "Toggle lean", description: "Press once to lean, press again to stand straight. Off = hold to lean")]
    public bool ToggleMode
    {
        get => toggleMode;
        set => SetField(ref toggleMode, value);
    }

    [Slider(0.2f, 0.8f, 0.05f, SliderAttribute.SliderType.Float, label: "Lean distance (m)",
        description: "How far your character steps to the side")]
    public float LeanDistance
    {
        get => leanDistance;
        set => SetField(ref leanDistance, value);
    }

    [Slider(0f, 20f, 1f, SliderAttribute.SliderType.Float, label: "Camera tilt (degrees)",
        description: "First-person camera roll at full lean. 0 = no tilt")]
    public float TiltAngle
    {
        get => tiltAngle;
        set => SetField(ref tiltAngle, value);
    }

    #endregion

    /// <summary>Clamp values from a hand-edited config file.</summary>
    public void Validate()
    {
        leanDistance = Clamp(leanDistance, 0.2f, 0.8f, 0.5f);
        tiltAngle = Clamp(tiltAngle, 0f, 20f, 10f);
    }

    private static float Clamp(float value, float min, float max, float fallback)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return fallback;
        return Math.Max(min, Math.Min(max, value));
    }

    #region Property change notification boilerplate

    public static readonly Config Current = ConfigStorage.Load();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
