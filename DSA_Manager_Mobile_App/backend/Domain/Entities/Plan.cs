namespace DSAManager.Backend.Domain.Entities;
public class Plan
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Name { get; set; } = "";
    public string PeriodType { get; set; } = "Month";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CreatedById { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Group? Group { get; set; }
    public User? CreatedBy { get; set; }
}
