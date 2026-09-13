using System.Collections.Generic;
using Avalonia.Controls;

namespace PferdehofGUI;

/// <summary>Drives the single content area in AppWindow. Keeps a stack of previously shown
/// views so Back can return to exactly where the user was, including any loaded data still
/// held by that view instance (nothing is re-created on Back).</summary>
public class Navigator
{
    private readonly ContentControl _host;
    private readonly Button _backButton;
    private readonly TextBlock _titleText;
    private readonly Stack<(Control View, string Title)> _history = new();

    public Navigator(ContentControl host, Button backButton, TextBlock titleText)
    {
        _host = host;
        _backButton = backButton;
        _titleText = titleText;
        _backButton.Click += (_, _) => GoBack();
    }

    /// <summary>Replaces the entire navigation stack with this view - use for transitions that
    /// shouldn't be reachable via Back (Startup -> Main Menu, Switch Player).</summary>
    public void Reset(Control view, string title)
    {
        _history.Clear();
        _history.Push((view, title));
        Show();
    }

    /// <summary>Pushes a new view on top of the current one - Back returns to whatever was
    /// showing before, with its state intact.</summary>
    public void Push(Control view, string title)
    {
        _history.Push((view, title));
        Show();
    }

    public void GoBack()
    {
        if (_history.Count <= 1) return;
        _history.Pop();
        Show();
    }

    private void Show()
    {
        var (view, title) = _history.Peek();
        _host.Content = view;
        _titleText.Text = title;
        _backButton.IsVisible = _history.Count > 1;
    }
}
