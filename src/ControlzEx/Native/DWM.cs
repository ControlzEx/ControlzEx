namespace ControlzEx.Native;

using System;

internal enum DWMSBT : uint
{
    /// <summary>
    /// Automatically selects backdrop effect.
    /// </summary>
    DWMSBT_AUTO = 0,

    /// <summary>
    /// Turns off the backdrop effect.
    /// </summary>
    DWMSBT_DISABLE = 1,

    /// <summary>
    /// Sets Mica effect with generated wallpaper tint.
    /// </summary>
    DWMSBT_MAINWINDOW = 2,

    /// <summary>
    /// Sets acrlic effect.
    /// </summary>
    DWMSBT_TRANSIENTWINDOW = 3,

    /// <summary>
    /// Sets blurred wallpaper effect, like Mica without tint.
    /// </summary>
    DWMSBT_TABBEDWINDOW = 4
}

internal static class DWMAttributeValues
{
    public const int False = 0x00;
    public const int True = 0x01;

#pragma warning disable SA1310
    public const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;
    public const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;
#pragma warning restore SA1310
}