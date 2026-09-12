using Avalonia.Controls;

namespace PferdehofGUI;

/// <summary>Caps a window so it can never be larger than the screen it opened on. Call once
/// from the window's Opened handler (screen info isn't available before the window is attached
/// to a platform window).</summary>
public static class WindowSizing
{
    public static void ClampToScreen(Window window)
    {
        var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;
        if (screen is null) return;

        // WorkingArea is in physical pixels; convert to the window's own DIPs via RenderScaling.
        double scaling = window.RenderScaling <= 0 ? 1.0 : window.RenderScaling;
        double maxWidth = screen.WorkingArea.Width / scaling;
        double maxHeight = screen.WorkingArea.Height / scaling;

        window.MaxWidth = maxWidth;
        window.MaxHeight = maxHeight;

        if (window.Width > maxWidth) window.Width = maxWidth;
        if (window.Height > maxHeight) window.Height = maxHeight;
    }
}
