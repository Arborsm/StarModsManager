using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using StarModsManager.Api;

namespace StarModsManager.lib;

public class Notification : INotification
{
    public static readonly Notification Instance = new();
    private static void Show(string message, string title,
        InfoBarSeverity severity)
    {
        WeakReferenceMessenger.Default.Send(new NotificationMessage
        {
            Title = title,
            Message = message,
            Severity = severity
        });
    }

    public void Show(string title, string message, Severity severity)
    {
        var type = severity switch
        {
            Severity.Informational => InfoBarSeverity.Informational,
            Severity.Warning => InfoBarSeverity.Warning,
            Severity.Error => InfoBarSeverity.Error,
            Severity.Success => InfoBarSeverity.Success,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };
        Show(message, title, type);
    }

    public void Show(string message)
    {
        Show(message, "Info", InfoBarSeverity.Informational);
    }
}