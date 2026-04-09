// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Entities;

/// <summary>Navigant (projection minimale pour le Module 1).</summary>
public class CrewMember
{
    public Guid Id { get; set; }
    public string Trigram { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public CrewCategory Category { get; set; }
    public CrewRank Rank { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsExaminer { get; set; }
    public int OfficeDaysPerWeek { get; set; }    // 0 = ligne pure, 3 = RDOV, 2 = RDFE
    public bool WeekendOffFixed { get; set; }      // true = repos sam+dim imposé
}

/// <summary>Bloc de vols (rotation de N vols sur une demi-journée).</summary>
public class FlightBlock
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string Period { get; set; } = string.Empty;
    public TimeOnly DpStart { get; set; }
    public TimeOnly DpEnd { get; set; }
    public TimeOnly FdpStart { get; set; }
    public TimeOnly FdpEnd { get; set; }
    public List<Flight> Flights { get; set; } = new();
}

/// <summary>Vol catalogue.</summary>
public class Flight
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Departure { get; set; } = string.Empty;
    public string Arrival { get; set; } = string.Empty;
    public int BlockTimeMinutes { get; set; }
}

/// <summary>Semaine type avec ses blocs associés.</summary>
public class WeekPattern
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public List<WeekPatternBlock> Blocks { get; set; } = new();
}

/// <summary>Association semaine type ↔ bloc pour un jour donné.</summary>
public class WeekPatternBlock
{
    public Guid WeekPatternId { get; set; }
    public Guid FlightBlockId { get; set; }
    public int DayOfWeek { get; set; }
    public int Sequence { get; set; }
}

/// <summary>Affectation calendrier : semaine ISO → semaine type.</summary>
public class CalendarAssignment
{
    public Guid Id { get; set; }
    public int IsoYear { get; set; }
    public int IsoWeek { get; set; }
    public Guid WeekPatternId { get; set; }
}

/// <summary>Demande de congé.</summary>
public class LeaveRequest
{
    public Guid Id { get; set; }
    public Guid CrewMemberId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

/// <summary>Jeu de règles FTL (EASA ORO.FTL).</summary>
public class FtlRuleSet
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "EASA ORO.FTL";
    public double MaxHdv28d { get; set; } = 100;
    public double MaxHdv90d { get; set; } = 280;
    public double MaxHdv12m { get; set; } = 900;
    public double MaxDuty7d { get; set; } = 60;
    public double MaxDuty14d { get; set; } = 110;
    public double MaxDuty28d { get; set; } = 190;
    public double MinRestHours { get; set; } = 12;
    public double ExtendedRestHours { get; set; } = 36;
    public double ExtendedRestPeriodHours { get; set; } = 168;
    public int MinDaysOffMonth { get; set; } = 8;

    // Repos spécifiques Air Calédonie / EASA ORO.FTL
    public int MaxConsecutiveWorkDays { get; set; } = 6;
    public int MinRestDaysPerPeriod { get; set; } = 2;
    public double WeeklyRestHours { get; set; } = 36;
    public int WeeklyRestNights { get; set; } = 2;

    // Repos mensuel week-end
    public bool MonthlyWeekendRestRequired { get; set; } = true;
    public int MonthlyWeekendRestDays { get; set; } = 3;
}
