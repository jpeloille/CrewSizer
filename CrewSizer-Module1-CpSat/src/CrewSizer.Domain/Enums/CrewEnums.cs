// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

namespace CrewSizer.Domain.Enums;

/// <summary>Catégorie de navigant.</summary>
public enum CrewCategory
{
    /// <summary>Personnel Navigant Technique (pilotes).</summary>
    PNT,

    /// <summary>Personnel Navigant Commercial (cabine).</summary>
    PNC
}

/// <summary>Rang du navigant dans sa catégorie.</summary>
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
