using DSAManager.Backend.Domain.Enums;
using DsaTaskStatus = DSAManager.Backend.Domain.Enums.TaskStatus;
namespace DSAManager.Backend.Domain.Entities;
public class TaskItem
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int? ProgressTarget { get; set; }
    public int ProgressCompleted { get; set; }
    public DateTime Deadline { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DsaTaskStatus Status { get; set; } = DsaTaskStatus.New;
    public int? ProductId { get; set; }
    public int? GroupId { get; set; }
    public int? AssignedUserId { get; set; }
    public int CreatedById { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Group? Group { get; set; }
    public Product? Product { get; set; }
    public User? AssignedUser { get; set; }
    public User? CreatedBy { get; set; }
}
