namespace Ims.Api.Features.Distribution;

internal sealed class DistributionScheduler(IServiceScopeFactory scopes, ILogger<DistributionScheduler> log) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IDistributionService>().TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception e) { log.LogWarning(e, "Distribution tick failed"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
