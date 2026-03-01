namespace CrewSizer.Domain.Enums;

/// <summary>Rang du navigant dans sa catégorie (utilisé par le solver CP-SAT).</summary>
public enum CrewRank
{
    /// <summary>Commandant de Bord.</summary>
    CDB,

    /// <summary>Officier Pilote de Ligne (copilote).</summary>
    OPL,

    /// <summary>Chef de Cabine.</summary>
    CC,

    /// <summary>Personnel Navigant Commercial.</summary>
    PNC,

    /// <summary>Responsable PNC (double-casquette LRS).</summary>
    RPN
}
