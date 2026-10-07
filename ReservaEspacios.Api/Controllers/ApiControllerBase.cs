using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Exceptions;

namespace ReservaEspacios.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleException(Exception ex, ILogger logger, string operacion)
    {
        if (ex is ApiException apiEx)
        {
            logger.LogWarning("Error de negocio al {Operacion}: {Mensaje}", operacion, apiEx.Message);
            return StatusCode(apiEx.StatusCode, new ErrorResponse { Error = apiEx.Message });
        }

        logger.LogError(ex, "Error inesperado al {Operacion}.", operacion);
        return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
        {
            Error = "Ocurrió un error interno al procesar la solicitud."
        });
    }
}
