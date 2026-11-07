using Microsoft.AspNetCore.Mvc;
namespace DSAManager.API.Controllers;
[ApiController, Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", message = "DSAManager API работает", time = DateTime.UtcNow });
}
