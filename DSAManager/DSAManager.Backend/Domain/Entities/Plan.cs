namespace DSAManager.Backend.Domain.Entities;

public class Plan
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Target { get; set; }
}
