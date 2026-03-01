namespace CrewSizer.Domain.ValueObjects;

public class FonctionSol
{
    public string Nom { get; set; } = "";
    public int NbPersonnes { get; set; }
    public int JoursSolMois { get; set; }

    public int JoursPersonnelSol => NbPersonnes * JoursSolMois;
}
