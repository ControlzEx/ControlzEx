namespace ControlzEx;

using ControlzEx.Helpers;

public static class FeatureSupport
{
    public static bool IsPopupBackdropSupported { get; } = OSVersionHelper.IsWindows10_OrGreater;

    public static bool IsWindowBackdropSupported { get; } = OSVersionHelper.IsWindows11_22H2_OrGreater;

    public static bool IsWindowCaptionColorSupported { get; } = OSVersionHelper.IsWindows11_OrGreater;

    public static bool IsWindowBorderColorSupported { get; } = OSVersionHelper.IsWindows11_OrGreater;
}