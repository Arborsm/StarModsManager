namespace StarModsManager.Api;

public interface INotification
{
    void CloseAll();
    
    void Close(object? msg);
    
    void Show(object content, Severity severity,
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null);
    
    object? Show(string title, string message, Severity severity,
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null);
    
    object? Show(string message);
    
    object ShowLongMsg(string title, string msg, Severity severity, 
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null);
}

public enum Severity
{
    Informational,
    Success,
    Warning,
    Error
}