using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Controllers;

/// <summary>Addresses available for in-person interviews, for the "Generar turnos" form.</summary>
[ApiController]
[Route("api/admin/interview-locations")]
[Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Coordinator)}")]
public class InterviewLocationsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<string>> List() => Ok(InterviewLocations.All);
}
