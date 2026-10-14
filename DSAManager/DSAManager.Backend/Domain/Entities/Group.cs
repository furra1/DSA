namespace DSAManager.Backend.Domain.Entities;

public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int ManagerId { get; set; }
}

