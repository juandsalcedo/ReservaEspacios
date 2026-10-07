using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

namespace ReservaEspacios.Api.Controllers;

[Route("api/spaces")]
public class SpacesController : ApiControllerBase
{
    private readonly ISpaceService _spaceService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SpacesController> _logger;

    public SpacesController(ISpaceService spaceService, ICurrentUserService currentUser, ILogger<SpacesController> logger)
    {
        _spaceService = spaceService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SpaceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var spaces = await _spaceService.GetAllSpaces(HttpContext.RequestAborted);
            return Ok(spaces);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "consultar los espacios");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SpaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador del espacio no es válido." });

            var space = await _spaceService.GetSpaceById(id, HttpContext.RequestAborted);
            return Ok(space);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"consultar el espacio {id}");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(SpaceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSpaceRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var creado = await _spaceService.CreateSpace(_currentUser.GetUserId(), request, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "crear el espacio");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SpaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSpaceRequest? request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador del espacio no es válido." });
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var actualizado = await _spaceService.UpdateSpace(id, _currentUser.GetUserId(), request, HttpContext.RequestAborted);
            return Ok(actualizado);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"modificar el espacio {id}");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(SpaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Error = "El identificador del espacio no es válido." });

            var eliminado = await _spaceService.DeleteSpace(id, _currentUser.GetUserId(), HttpContext.RequestAborted);
            return Ok(eliminado);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, $"eliminar el espacio {id}");
        }
    }
}
