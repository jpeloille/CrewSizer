// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Domain.Entities;
using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Interfaces;

public interface ICrewRepository
{
    Task<IReadOnlyList<CrewMember>> GetByCategoryAsync(CrewCategory category, CancellationToken ct = default);
}

public interface IFlightBlockRepository
{
    Task<IReadOnlyList<FlightBlock>> GetAllAsync(CancellationToken ct = default);
}

public interface ICalendarAssignmentRepository
{
    Task<CalendarAssignment?> GetByWeekAsync(int isoYear, int isoWeek, CancellationToken ct = default);
}

public interface IWeekPatternRepository
{
    Task<WeekPattern?> GetByIdWithBlocksAsync(Guid id, CancellationToken ct = default);
}

public interface ILeaveRequestRepository
{
    Task<IReadOnlyList<LeaveRequest>> GetApprovedInRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default);
}

public interface IFtlRuleSetRepository
{
    Task<FtlRuleSet?> GetDefaultAsync(CancellationToken ct = default);
}
