namespace DSAManager.Backend.Domain.Entities;
public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? Manager { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
}
