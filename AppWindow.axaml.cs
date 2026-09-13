using Avalonia.Controls;

namespace PferdehofGUI;

public partial class AppWindow : Window
{
    public Navigator Navigator { get; }

    public AppWindow()
    {
        InitializeComponent();
        Navigator = new Navigator(ContentHost, BackButton, TitleText);
        Opened += (_, _) => WindowSizing.ClampToScreen(this);
        Navigator.Reset(new StartupView(Navigator), "Setup");
    }
}
