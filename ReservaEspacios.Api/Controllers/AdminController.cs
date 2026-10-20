using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

namespace ReservaEspacios.Api.Controllers;

public class AdminController : ApiControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IReservationService reservationService,
        IUserService userService,
        ICurrentUserService currentUser,
        ILogger<AdminController> logger)
    {
        _reservationService = reservationService;
        _userService = userService;
        _currentUser = currentUser;
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

    [HttpGet("api/admin/reservations")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReservations()
    {
        try
        {
            var reservas = await _reservationService.GetAllReservations(_currentUser.GetUserId(), HttpContext.RequestAborted);
            return Ok(reservas);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "consultar todas las reservas");
        }
    }

    [HttpDelete("api/admin/reservations/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelReservation(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador de la reserva no es válido." });

            var cancelada = await _reservationService.CancelReservationAsAdmin(id, _currentUser.GetUserId(), HttpContext.RequestAborted);
            return Ok(cancelada);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"cancelar la reserva {id} como administrador");
        }
    }

    [HttpGet("api/admin/users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var usuarios = await _userService.ListUsers(_currentUser.GetUserId(), HttpContext.RequestAborted);
            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "consultar los usuarios");
        }
    }

    [HttpPut("api/admin/users/{id:int}/rol")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest? request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador del usuario no es válido." });
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var perfil = await _userService.UpdateRole(_currentUser.GetUserId(), id, request.Rol, HttpContext.RequestAborted);
            return Ok(perfil);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"cambiar el rol del usuario {id}");
        }
    }
}
