namespace StarModsManager.Api;

public interface INotification
{
    void Show(string title, string message, Severity type);
    
    void Show(string message);
}

public enum Severity
{
    /// <summary>
    /// Communicates that the InfoBar is displaying general information that requires
    /// the user's attention. For assistive technologies, they will follow the
    /// behavior set in the Processing_All constant.
    /// </summary>
    Informational,
    /// <summary>
    /// Communicates that the InfoBar is displaying information regarding a long-running
    /// and/or background task that has completed successfully. For assistive technologies,
    /// they will follow the behavior set in the Processing_All constant.
    /// </summary>
    Success,
    /// <summary>
    /// Communicates that the InfoBar is displaying information regarding a condition that
    /// might cause a problem in the future. For assistive technologies, they will follow
    /// the behavior set in the NotificationProcessing_ImportantAll constan
    /// </summary>
    Warning,
    /// <summary>
    /// Communicates that the InfoBar is displaying information regarding an error or problem
    /// that has occurred. For assistive technologies, they will follow the behavior set in the
    /// NotificationProcessing_ImportantAll constant.
    /// </summary>
    Error,
}