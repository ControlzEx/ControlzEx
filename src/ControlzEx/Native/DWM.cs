namespace ControlzEx.Native;

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