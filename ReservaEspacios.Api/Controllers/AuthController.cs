using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Services;

namespace ReservaEspacios.Api.Controllers;

[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] AuthRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var perfil = await _authService.Register(request, HttpContext.RequestAborted);
            return StatusCode(StatusCodes.Status201Created, perfil);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "registrar el usuario");
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var respuesta = await _authService.Login(request, HttpContext.RequestAborted);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "iniciar sesión");
        }
    }

    [HttpGet("password-reset-available")]
    [ProducesResponseType(typeof(PasswordResetAvailabilityResponse), StatusCodes.Status200OK)]
    public IActionResult PasswordResetAvailable()
    {
        return Ok(new PasswordResetAvailabilityResponse
        {
            Disponible = _authService.RestablecimientoDisponible()
        });
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var respuesta = await _authService.SolicitarRestablecimiento(request.Email, HttpContext.RequestAborted);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "solicitar el restablecimiento de contraseña");
        }
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest? request)
    {
        try
        {
            if (request is null)
                return BadRequest(new ErrorResponse { Error = "El cuerpo de la solicitud es obligatorio." });

            var respuesta = await _authService.RestablecerContrasena(request.Token, request.Password, HttpContext.RequestAborted);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger, "restablecer la contraseña");
        }
    }
}
