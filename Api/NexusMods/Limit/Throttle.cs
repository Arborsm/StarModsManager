namespace StarModsManager.Api.NexusMods.Limit;

public sealed class Throttle(int maxActions, TimeSpan maxPeriod)
{
    private readonly SemaphoreSlim _throttleActions = new(maxActions, maxActions), _throttlePeriods = new(maxActions, maxActions);

        public async Task<T> Queue<T>(Func<Task<T>> action)
        {
            await this._throttleActions.WaitAsync();
            Task? releaseTask = null;
            try
            {
                await this._throttlePeriods.WaitAsync();

                releaseTask = Task.Delay(maxPeriod).ContinueWith(_ => 
                {
                    this._throttlePeriods.Release(1);
                });

                return await action();
            }
            finally
            {
                this._throttleActions.Release(1);

                if (releaseTask != null) await releaseTask;
            }
        }
    }