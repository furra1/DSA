using System.Security.Claims;
using DSAManager.Backend.Domain.Entities;
using DSAManager.Backend.Domain.Enums;
using DSAManager.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DsaTaskStatus = DSAManager.Backend.Domain.Enums.TaskStatus;

namespace DSAManager.API.Controllers;

[ApiController, Route("api"), Authorize]
public class WorkspaceController(AppDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var dashboardTasks = db.Tasks.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager")) dashboardTasks = dashboardTasks.Where(x=>x.AssignedUserId==CurrentUserId);
        var total = await dashboardTasks.CountAsync(ct);
        var completed = await dashboardTasks.CountAsync(x => x.Status == DsaTaskStatus.Completed, ct);
        var active = await dashboardTasks.CountAsync(x => x.Status == DsaTaskStatus.InProgress, ct);
        var overdue = await dashboardTasks.CountAsync(x => x.Deadline < DateTime.UtcNow && x.Status != DsaTaskStatus.Completed && x.Status != DsaTaskStatus.Cancelled, ct);
        return Ok(new { totalTasks = total, inProgress = active, completed, overdue, completionPercent = total == 0 ? 0 : (int)Math.Round(completed * 100d / total), users = await db.Users.CountAsync(x => x.IsActive, ct), products = await db.Products.CountAsync(x => x.IsActive, ct) });
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Users(CancellationToken ct) => Ok(await db.Users.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.LastName).Select(x=>new { x.Id, x.FirstName, x.LastName, x.Email, x.EmployeeNumber, role=x.Role.ToString(), x.GroupId, x.IsActive }).ToListAsync(ct));

    [HttpGet("groups")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Groups(CancellationToken ct) => Ok(await db.Groups.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new {x.Id,x.Name,x.Description,x.ManagerId,x.IsActive, members=x.Users.Count}).ToListAsync(ct));

    [HttpPost("groups")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateGroup(GroupInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new {message="Укажите название группы."});
        var entity = new Group { Name=input.Name.Trim(), Description=input.Description?.Trim(), ManagerId=input.ManagerId, IsActive=true, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow };
        db.Groups.Add(entity); await db.SaveChangesAsync(ct); return Created($"/api/groups/{entity.Id}", new {entity.Id,entity.Name,entity.Description,entity.ManagerId,entity.IsActive});
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products(CancellationToken ct) => Ok(await db.Products.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new {x.Id,x.Name,x.Description,x.IsActive}).ToListAsync(ct));

    [HttpPost("products")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateProduct(ProductInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new {message="Укажите название продукта."});
        var entity=new Product {Name=input.Name.Trim(),Description=input.Description?.Trim(),IsActive=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
        db.Products.Add(entity); await db.SaveChangesAsync(ct); return Created($"/api/products/{entity.Id}",new {entity.Id,entity.Name,entity.Description,entity.IsActive});
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> Tasks([FromQuery]string? status, CancellationToken ct)
    {
        var q=db.Tasks.AsNoTracking().Include(x=>x.AssignedUser).Include(x=>x.Product).Include(x=>x.Group).AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager")) q=q.Where(x=>x.AssignedUserId==CurrentUserId);
        if(!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DsaTaskStatus>(status,true,out var parsed)) q=q.Where(x=>x.Status==parsed);
        var items=await q.OrderBy(x=>x.Deadline).Select(x=>new {x.Id,x.Title,x.Description,x.Deadline,priority=x.Priority.ToString(),status=x.Status.ToString(),x.ProductId,productName=x.Product==null?null:x.Product.Name,x.GroupId,groupName=x.Group==null?null:x.Group.Name,x.AssignedUserId,assignee=x.AssignedUser==null?null:x.AssignedUser.FirstName+" "+x.AssignedUser.LastName,x.ProgressTarget,x.ProgressCompleted,x.CreatedAt}).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("tasks")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateTask(TaskInput input, CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.Title)) return BadRequest(new {message="Укажите название задачи."});
        if (input.ProgressTarget.HasValue && input.ProgressTarget.Value <= 0) return BadRequest(new {message="Целевой объём должен быть больше нуля."});
        if (input.AssignedUserId.HasValue && !await db.Users.AnyAsync(x=>x.Id==input.AssignedUserId.Value && x.IsActive,ct)) return BadRequest(new {message="Выбранный сотрудник не найден или неактивен."});
        if(!Enum.TryParse<TaskPriority>(input.Priority,true,out var priority)) priority=TaskPriority.Medium;
        var entity=new TaskItem {Title=input.Title.Trim(),Description=input.Description?.Trim(),Deadline=input.Deadline,Priority=priority,Status=DsaTaskStatus.New,ProductId=input.ProductId,GroupId=input.GroupId,AssignedUserId=input.AssignedUserId,ProgressTarget=input.ProgressTarget,ProgressCompleted=0,CreatedById=CurrentUserId,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
        db.Tasks.Add(entity); await db.SaveChangesAsync(ct); return Created($"/api/tasks/{entity.Id}",new {entity.Id,entity.Title,status=entity.Status.ToString()});
    }

    [HttpPatch("tasks/{id:long}/status")]
    public async Task<IActionResult> UpdateTaskStatus(long id, TaskStatusInput input, CancellationToken ct)
    {
        if(!Enum.TryParse<DsaTaskStatus>(input.Status,true,out var status)) return BadRequest(new {message="Неизвестный статус."});
        var task=await db.Tasks.FirstOrDefaultAsync(x=>x.Id==id,ct); if(task is null)return NotFound();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && task.AssignedUserId != CurrentUserId) return Forbid();
        task.Status=status; task.UpdatedAt=DateTime.UtcNow; if(status==DsaTaskStatus.InProgress && task.StartedAt is null)task.StartedAt=DateTime.UtcNow; if(status==DsaTaskStatus.Completed)task.CompletedAt=DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return Ok(new {task.Id,status=task.Status.ToString()});
    }

    [HttpPatch("tasks/{id:long}/progress")]
    public async Task<IActionResult> UpdateTaskProgress(long id, TaskProgressInput input, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if (task is null) return NotFound();
        if (task.AssignedUserId != CurrentUserId && !User.IsInRole("Admin") && !User.IsInRole("Manager")) return Forbid();
        if (input.Completed < 0 || (task.ProgressTarget.HasValue && input.Completed > task.ProgressTarget.Value))
            return BadRequest(new {message="Выполнение должно быть от нуля до целевого объёма."});
        task.ProgressCompleted = input.Completed;
        if (task.ProgressTarget.HasValue && task.ProgressCompleted >= task.ProgressTarget.Value)
        {
            task.Status = DsaTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (task.ProgressCompleted > 0 && task.Status == DsaTaskStatus.New)
        {
            task.Status = DsaTaskStatus.InProgress;
            task.StartedAt ??= DateTime.UtcNow;
        }
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new {task.Id, task.ProgressTarget, task.ProgressCompleted, status=task.Status.ToString(), task.UpdatedAt});
    }

    [HttpGet("plans")]
    public async Task<IActionResult> Plans(CancellationToken ct) => Ok(await db.Plans.AsNoTracking().Include(x=>x.Group).OrderByDescending(x=>x.StartDate).Select(x=>new {x.Id,x.Name,x.PeriodType,x.StartDate,x.EndDate,x.Status,x.GroupId,groupName=x.Group==null?null:x.Group.Name}).ToListAsync(ct));

    [HttpPost("plans")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreatePlan(PlanInput input, CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.Name)||input.EndDate<input.StartDate)return BadRequest(new {message="Проверьте название и даты плана."});
        var entity=new Plan {Name=input.Name.Trim(),GroupId=input.GroupId,PeriodType=input.PeriodType,StartDate=input.StartDate,EndDate=input.EndDate,CreatedById=CurrentUserId,Status="Draft",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
        db.Plans.Add(entity); await db.SaveChangesAsync(ct); return Created($"/api/plans/{entity.Id}",new {entity.Id,entity.Name,entity.Status});
    }
    [HttpGet("profile")]
    public async Task<IActionResult> MyProfile(CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(x=>x.Group).FirstOrDefaultAsync(x=>x.Id==CurrentUserId,ct);
        if (user is null) return NotFound();
        var tasks = await db.Tasks.AsNoTracking().Where(x=>x.AssignedUserId==user.Id).Include(x=>x.Group).Include(x=>x.Product)
            .OrderBy(x=>x.Deadline).Select(x=>new {x.Id,x.Title,x.Description,x.Deadline,priority=x.Priority.ToString(),status=x.Status.ToString(),
                assignee=x.AssignedUser==null?null:x.AssignedUser.FirstName+" "+x.AssignedUser.LastName,
                groupName=x.Group==null?null:x.Group.Name,productName=x.Product==null?null:x.Product.Name,x.ProgressTarget,x.ProgressCompleted,x.CompletedAt}).ToListAsync(ct);
        return Ok(new {user.Id,user.FirstName,user.LastName,user.Email,user.EmployeeNumber,role=user.Role.ToString(),groupName=user.Group==null?null:user.Group.Name,tasks,
            taskSummary=new {total=tasks.Count,completed=tasks.Count(x=>x.status=="Completed"),inProgress=tasks.Count(x=>x.status=="InProgress"),overdue=tasks.Count(x=>x.Deadline<DateTime.UtcNow&&x.status!="Completed"&&x.status!="Cancelled")}});
    }

    [HttpGet("admin/users/{userId:int}/details")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> EmployeeDetails(int userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(x=>x.Group).FirstOrDefaultAsync(x=>x.Id==userId,ct);
        if (user is null) return NotFound(new {message="Сотрудник не найден."});
        var tasks = await db.Tasks.AsNoTracking().Where(x=>x.AssignedUserId==userId)
            .Include(x=>x.Group).Include(x=>x.Product).OrderBy(x=>x.Deadline)
            .Select(x=>new {x.Id,x.Title,x.Description,x.Deadline,priority=x.Priority.ToString(),status=x.Status.ToString(),
                assignee=x.AssignedUser==null?null:x.AssignedUser.FirstName+" "+x.AssignedUser.LastName,
                groupName=x.Group==null?null:x.Group.Name,productName=x.Product==null?null:x.Product.Name,
                x.ProgressTarget,x.ProgressCompleted,x.CreatedAt,x.StartedAt,x.CompletedAt}).ToListAsync(ct);
        return Ok(new {user.Id,user.FirstName,user.LastName,user.Email,user.EmployeeNumber,role=user.Role.ToString(),
            group=user.Group==null?null:new {user.Group.Id,user.Group.Name},tasks,
            taskSummary=new {total=tasks.Count,completed=tasks.Count(x=>x.status=="Completed"),
                inProgress=tasks.Count(x=>x.status=="InProgress"),overdue=tasks.Count(x=>x.Deadline<DateTime.UtcNow&&x.status!="Completed"&&x.status!="Cancelled")}});
    }

    [HttpGet("admin/users/{userId:int}/export.xlsx")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ExportUserExcel(int userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.Group)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return NotFound(new { message = "Сотрудник не найден." });

        var completed = await db.Tasks.AsNoTracking()
            .Where(x => x.AssignedUserId == userId)
            .Include(x => x.Product).Include(x => x.Group)
            .OrderByDescending(x => x.CompletedAt)
            .Select(x => new { x.Id, x.Title, x.Description, x.Priority, x.Status, x.Deadline, x.CompletedAt, x.ProgressTarget, x.ProgressCompleted,
                Product = x.Product == null ? "" : x.Product.Name,
                Group = x.Group == null ? "" : x.Group.Name })
            .ToListAsync(ct);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var profile = workbook.Worksheets.Add("Сотрудник");
        profile.Cell(1,1).Value = "Профиль сотрудника";
        profile.Cell(2,1).Value = "Имя"; profile.Cell(2,2).Value = user.FirstName;
        profile.Cell(3,1).Value = "Фамилия"; profile.Cell(3,2).Value = user.LastName;
        profile.Cell(4,1).Value = "Email"; profile.Cell(4,2).Value = user.Email;
        profile.Cell(5,1).Value = "Табельный номер"; profile.Cell(5,2).Value = user.EmployeeNumber;
        profile.Cell(6,1).Value = "Роль"; profile.Cell(6,2).Value = user.Role.ToString();
        profile.Cell(7,1).Value = "Группа"; profile.Cell(7,2).Value = user.Group?.Name ?? "";
        profile.Cell(8,1).Value = "Активен"; profile.Cell(8,2).Value = user.IsActive ? "Да" : "Нет";
        profile.Cell(10,1).Value = "Всего назначено задач"; profile.Cell(10,2).Value = completed.Count;
        profile.Columns().AdjustToContents();
        var usedRange = profile.RangeUsed();
if (usedRange != null)
{
    usedRange.Rows().First().Style.Font.Bold = true;
}

        var sheet = workbook.Worksheets.Add("Задачи сотрудника");
        var headers = new[] { "ID", "Задача", "Описание", "Приоритет", "Статус", "Продукт", "Группа", "Цель", "Выполнено", "% выполнения", "Срок", "Дата выполнения" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < completed.Count; i++)
        {
            var x = completed[i]; var r = i + 2;
            sheet.Cell(r,1).Value = x.Id; sheet.Cell(r,2).Value = x.Title;
            sheet.Cell(r,3).Value = x.Description ?? ""; sheet.Cell(r,4).Value = x.Priority.ToString();
            sheet.Cell(r,5).Value = x.Status.ToString(); sheet.Cell(r,6).Value = x.Product;
            sheet.Cell(r,7).Value = x.Group; sheet.Cell(r,8).Value = x.ProgressTarget ?? 0;
            sheet.Cell(r,9).Value = x.ProgressCompleted;
            sheet.Cell(r,10).Value = x.ProgressTarget.HasValue && x.ProgressTarget.Value > 0 ? Math.Round(100d*x.ProgressCompleted/x.ProgressTarget.Value,1) : 0;
            sheet.Cell(r,11).Value = x.Deadline;
            if (x.CompletedAt.HasValue) sheet.Cell(r,12).Value = x.CompletedAt.Value;
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        var safeName = $"employee_{user.Id}_task_progress_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", safeName);
    }
}

public sealed record GroupInput(string Name,string? Description,int? ManagerId);
public sealed record ProductInput(string Name,string? Description);
public sealed record TaskInput(string Title,string? Description,DateTime Deadline,string Priority="Medium",int? ProductId=null,int? GroupId=null,int? AssignedUserId=null,int? ProgressTarget=null);
public sealed record TaskProgressInput(int Completed);
public sealed record TaskStatusInput(string Status);
public sealed record PlanInput(string Name,int GroupId,string PeriodType,DateTime StartDate,DateTime EndDate);
