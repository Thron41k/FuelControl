using FuelControl.Domain.Entities;

namespace FuelControl.Infrastructure.Services.Models;

public sealed record FuelingMatchResult(
    FuelingRecord FuelingRecord,
    FuelingMatchCandidate Candidate);