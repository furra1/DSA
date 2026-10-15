using DSAManager.Backend.Domain.Enums;

namespace DSAManager.Backend.Domain.Entities;

public class Task
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }

    public TaskPriority Priority { get; set; }

    public TaskStatus Status { get; set; }

    public int AssignedUserId { get; set; }

    public int GroupId { get; set; }

    public int CreatedById { get; set; }
}