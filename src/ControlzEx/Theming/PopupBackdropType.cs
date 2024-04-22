namespace ControlzEx.Theming;

using System;

[Obsolete("This class uses undocumented OS theming methods. Thus this might break at any time.")]
public enum PopupBackdropType
{
    /// <summary>
    /// None
    /// </summary>
    None = PopupBackdropManager.AccentState.ACCENT_DISABLED,

    /// <summary>
    /// todo
    /// </summary>
    Gradient = PopupBackdropManager.AccentState.ACCENT_ENABLE_GRADIENT,

    /// <summary>
    /// todo
    /// </summary>
    TransparentGradient = PopupBackdropManager.AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT,

    /// <summary>
    /// todo
    /// </summary>
    Blurbehind = PopupBackdropManager.AccentState.ACCENT_ENABLE_BLURBEHIND,

    /// <summary>
    /// todo
    /// </summary>
    AcrylicBlurbehind = PopupBackdropManager.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND
}