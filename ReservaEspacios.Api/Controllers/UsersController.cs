using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

namespace ReservaEspacios.Api.Controllers;

[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ICurrentUserService currentUser, ILogger<UsersController> logger)
    {
        _userService = userService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var perfil = await _userService.GetProfile(_currentUser.GetUserId(), HttpContext.RequestAborted);
            return Ok(perfil);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "consultar el perfil");
        }
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var perfil = await _userService.UpdateProfile(_currentUser.GetUserId(), request, HttpContext.RequestAborted);
            return Ok(perfil);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "actualizar el perfil");
        }
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            await _userService.ChangePassword(_currentUser.GetUserId(), request, HttpContext.RequestAborted);
            return Ok(new MessageResponse { Mensaje = "Contraseña actualizada." });
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "cambiar la contraseña");
        }
    }
}
