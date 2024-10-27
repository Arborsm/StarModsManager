namespace StarModsManager.Api.NexusMods.Limit;

public sealed class Throttle(int maxActions, TimeSpan maxPeriod)
{
    private readonly SemaphoreSlim 
        _throttleActions = new(maxActions, maxActions),
        _throttlePeriods = new(maxActions, maxActions);
    
    public Task Run(Func<Task> action) => Task.Run(() => Queue(action));
    
    public async Task<T> Queue<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        await this._throttleActions.WaitAsync(cancellationToken);
        Task? releaseTask = null;
        try
        {
            await this._throttlePeriods.WaitAsync(cancellationToken);

            releaseTask = Task.Delay(maxPeriod, cancellationToken).ContinueWith(_ => 
            {
                this._throttlePeriods.Release(1);
            }, cancellationToken);

            return await action();
        }
        finally
        {
            this._throttleActions.Release(1);

            if (releaseTask != null) await releaseTask;
        }
    }
        
    public async Task Queue(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await this._throttleActions.WaitAsync(cancellationToken);
        Task? releaseTask = null;
        try
        {
            await this._throttlePeriods.WaitAsync(cancellationToken);

            releaseTask = Task.Delay(maxPeriod, cancellationToken).ContinueWith(_ => 
            {
                this._throttlePeriods.Release(1);
            }, cancellationToken);

            await action();
        }
        finally
        {
            this._throttleActions.Release(1);

            if (releaseTask != null) await releaseTask;
        }
    }
}