using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

namespace ReservaEspacios.Api.Controllers;

public class AdminController : ApiControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IReservationService reservationService, ILogger<AdminController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    [HttpGet("api/admin/statistics")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _reservationService.GetDashboardStatisticsAsync(HttpContext.RequestAborted);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "consultar las estadísticas del dashboard");
        }
    }
}
