namespace DSAManager.Backend.Domain.Entities;
public class TaskComment
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public int UserId { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public TaskItem? Task { get; set; }
    public User? User { get; set; }
}
