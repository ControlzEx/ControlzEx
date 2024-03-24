namespace ControlzEx.Theming;

using ControlzEx.Native;

/// <summary>
/// Mirrors values of DWM_SYSTEMBACKDROP_TYPE
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type
/// </remarks>
public enum BackdropType
{
    /// <summary>
    /// No backdrop effect.
    /// </summary>
    None = (int)DWMSBT.DWMSBT_DISABLE,

    /// <summary>
    /// Sets <c>DWMWA_SYSTEMBACKDROP_TYPE</c> to <see langword="0"></see>.
    /// </summary>
    Auto = (int)DWMSBT.DWMSBT_AUTO,

    /// <summary>
    /// Windows 11 Mica effect.
    /// </summary>
    Mica = (int)DWMSBT.DWMSBT_MAINWINDOW,

    /// <summary>
    /// Windows Acrylic effect.
    /// </summary>
    Acrylic = (int)DWMSBT.DWMSBT_TRANSIENTWINDOW,

    /// <summary>
    /// Windows 11 wallpaper blur effect.
    /// </summary>
    Tabbed = (int)DWMSBT.DWMSBT_TABBEDWINDOW
}