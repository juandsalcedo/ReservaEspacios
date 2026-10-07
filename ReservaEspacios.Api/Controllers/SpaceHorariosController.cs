using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

namespace ReservaEspacios.Api.Controllers;

[Route("api/space-horarios")]
public class SpaceHorariosController : ApiControllerBase
{
    private readonly ISpaceHorariosService _horariosService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SpaceHorariosController> _logger;

    public SpaceHorariosController(
        ISpaceHorariosService horariosService,
        ICurrentUserService currentUser,
        ILogger<SpaceHorariosController> logger)
    {
        _horariosService = horariosService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SpaceHorarioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? spaceId)
    {
        try
        {
            var horarios = await _horariosService.GetAll(spaceId, HttpContext.RequestAborted);
            return Ok(horarios);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "consultar los horarios");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SpaceHorarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador del horario no es válido." });

            var horario = await _horariosService.GetById(id, HttpContext.RequestAborted);
            return Ok(horario);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"consultar el horario {id}");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(SpaceHorarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSpaceHorarioRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var creado = await _horariosService.Create(_currentUser.GetUserId(), request, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "crear el horario");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SpaceHorarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSpaceHorarioRequest? request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador del horario no es válido." });
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var actualizado = await _horariosService.Update(id, _currentUser.GetUserId(), request, HttpContext.RequestAborted);
            return Ok(actualizado);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"modificar el horario {id}");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(SpaceHorarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador del horario no es válido." });

            var eliminado = await _horariosService.Delete(id, _currentUser.GetUserId(), HttpContext.RequestAborted);
            return Ok(eliminado);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"eliminar el horario {id}");
        }
    }
}
