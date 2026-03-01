namespace CrewSizer.Domain.ValueObjects;

/// <summary>Limites temps de service EU-OPS</summary>
public class LimitesTempsService
{
    /// <summary>Max 60h de service sur 7 jours consécutifs</summary>
    public double Max7j { get; set; } = 60;

    /// <summary>Max 110h de service sur 14 jours consécutifs</summary>
    public double Max14j { get; set; } = 110;

    /// <summary>Max 190h de service sur 28 jours consécutifs</summary>
    public double Max28j { get; set; } = 190;
}
