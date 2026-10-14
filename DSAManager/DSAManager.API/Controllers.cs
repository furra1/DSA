using Microsoft.AspNetCore.Mvc;

namespace DSAManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            message = "DSAManager API работает",
            time = DateTime.UtcNow
        });
    }
}