namespace OpenJustice.Generator.Web.Pages.Cases;

/// <summary>
/// Static lookup data for dropdowns.
/// In a real application, this would be fetched from the API.
/// </summary>
public static class LookupData
{
    public static List<LookupItem> CrimeTypes { get; } = new()
    {
        new LookupItem { Id = 1, Name = "Homicídio" },
        new LookupItem { Id = 2, Name = "Feminicídio" },
        new LookupItem { Id = 3, Name = "Estupro" },
        new LookupItem { Id = 4, Name = "Estupro de Vulnerável" },
        new LookupItem { Id = 5, Name = "Pedofilia" },
        new LookupItem { Id = 6, Name = "Atentado ao Pudor" },
        new LookupItem { Id = 7, Name = "Tráfico de Pessoas" },
        new LookupItem { Id = 8, Name = "Trabalho Escravo" },
        new LookupItem { Id = 9, Name = "Tortura" },
        new LookupItem { Id = 10, Name = "Lesão Corporal" },
        new LookupItem { Id = 11, Name = "Ameaça" },
        new LookupItem { Id = 12, Name = "Calúnia/Difamação" },
        new LookupItem { Id = 13, Name = "Violência Doméstica" },
        new LookupItem { Id = 14, Name = "Outros" }
    };

    public static List<LookupItem> CaseTypes { get; } = new()
    {
        new LookupItem { Id = 1, Name = "Crime Comum" },
        new LookupItem { Id = 2, Name = "Crime Hediondo" },
        new LookupItem { Id = 3, Name = "Crime Organizado" },
        new LookupItem { Id = 4, Name = "Crime Político" },
        new LookupItem { Id = 5, Name = "Violação de Direitos Humanos" },
        new LookupItem { Id = 6, Name = "Outros" }
    };

    public static List<LookupItem> JudicialStatuses { get; } = new()
    {
        new LookupItem { Id = 1, Name = "Investigação" },
        new LookupItem { Id = 2, Name = "Inquérito Policial" },
        new LookupItem { Id = 3, Name = "Processo em Andamento" },
        new LookupItem { Id = 4, Name = "Denúncia Recebida" },
        new LookupItem { Id = 5, Name = "Condenação" },
        new LookupItem { Id = 6, Name = "Absolvição" },
        new LookupItem { Id = 7, Name = "Arquivamento" },
        new LookupItem { Id = 8, Name = "Suspensão do Processo" },
        new LookupItem { Id = 9, Name = "Transação Penal" },
        new LookupItem { Id = 10, Name = "Suspensão Condicional" }
    };
}

public class LookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
