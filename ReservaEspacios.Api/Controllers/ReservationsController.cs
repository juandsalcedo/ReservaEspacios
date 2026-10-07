using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

namespace ReservaEspacios.Api.Controllers;

[Route("api/reservations")]
public class ReservationsController : ApiControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(
        IReservationService reservationService,
        ICurrentUserService currentUser,
        ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = _currentUser.GetUserId();
            var reservas = await _reservationService.GetUserReservations(userId, HttpContext.RequestAborted);
            return Ok(reservas);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "consultar las reservas");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador de la reserva no es válido." });

            var userId = _currentUser.GetUserId();
            var reserva = await _reservationService.GetReservation(id, userId, HttpContext.RequestAborted);
            return Ok(reserva);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"consultar la reserva {id}");
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var userId = _currentUser.GetUserId();
            var creada = await _reservationService.CreateReservation(userId, request, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetById), new { id = creada.Id }, creada);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "crear la reserva");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservationRequest? request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador de la reserva no es válido." });

            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var userId = _currentUser.GetUserId();
            var actualizada = await _reservationService.UpdateReservation(id, userId, request, HttpContext.RequestAborted);
            return Ok(actualizada);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"modificar la reserva {id}");
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador de la reserva no es válido." });

            var userId = _currentUser.GetUserId();
            var cancelada = await _reservationService.CancelReservation(id, userId, HttpContext.RequestAborted);
            return Ok(cancelada);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"cancelar la reserva {id}");
        }
    }
}
