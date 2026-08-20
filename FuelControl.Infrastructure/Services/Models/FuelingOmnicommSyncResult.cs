namespace FuelControl.Infrastructure.Services.Models;

public sealed record FuelingOmnicommSyncResult(
    int FuelingRecordsCount,
    int OmnicommEventsCount,
    int CreatedCount,
    int UpdatedCount,
    int UnlinkedCount);