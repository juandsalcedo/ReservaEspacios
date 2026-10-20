using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReservaEspacios.Api.Data;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Exceptions;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Services;

public class AuthService : IAuthService
{
    public const string MensajeCorreoUniversitario = "Solo se permiten correos universitarios (@cun.edu.co)";
    public const string MensajeRestablecimiento =
        "Si el correo está registrado, recibirás instrucciones para restablecer tu contraseña.";

    private const int ResetTokenMinutes = 30;

    private static readonly Regex CorreoUniversitario = new(
        @"^[^@\s]+@cun\.edu\.co$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly ReservaEspaciosContext _context;
    private readonly JwtOptions _jwt;
    private readonly AppOptions _app;
    private readonly SmtpSettings _smtp;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ReservaEspaciosContext context,
        IOptions<JwtOptions> jwtOptions,
        IOptions<AppOptions> appOptions,
        IOptions<SmtpSettings> smtpOptions,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwt = jwtOptions.Value;
        _app = appOptions.Value;
        _smtp = smtpOptions.Value;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<UserProfileResponse> Register(AuthRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var email = NormalizarCorreo(request.Email);
        ValidarPassword(request.Password);

        var nombre = request.Nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ApiException("El nombre es obligatorio.", StatusCodes.Status400BadRequest);
        if (nombre.Length > 120)
            throw new ApiException("El nombre no puede superar 120 caracteres.", StatusCodes.Status400BadRequest);

        var telefono = request.Telefono?.Trim() ?? string.Empty;
        var facultad = request.Facultad?.Trim() ?? string.Empty;
        if (telefono.Length > 30)
            throw new ApiException("El teléfono no puede superar 30 caracteres.", StatusCodes.Status400BadRequest);
        if (facultad.Length > 150)
            throw new ApiException("La facultad no puede superar 150 caracteres.", StatusCodes.Status400BadRequest);

        var existe = await _context.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (existe)
            throw new ApiException("Ya existe una cuenta con ese correo.", StatusCodes.Status409Conflict);

        var usuario = new User
        {
            Nombre = nombre,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = ResolverRolPublico(request.Rol),
            Telefono = telefono,
            Facultad = facultad,
            FechaRegistro = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.Users.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario {Email} registrado como {Rol}.", usuario.Email, usuario.Rol);
        return MapProfile(usuario);
    }

    public async Task<AuthResponse> Login(AuthRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var email = NormalizarCorreo(request.Email);

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ApiException("La contraseña es obligatoria.", StatusCodes.Status400BadRequest);

        var usuario = await _context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
        if (usuario is null || !PasswordCoincide(request.Password, usuario.PasswordHash))
            throw new ApiException("Correo o contraseña incorrectos.", StatusCodes.Status401Unauthorized);

        var expira = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes <= 0 ? 120 : _jwt.ExpiresMinutes);
        var token = CrearToken(usuario, expira);

        _logger.LogInformation("Inicio de sesión de {Email}.", usuario.Email);
        return new AuthResponse
        {
            Token = token,
            Expira = expira,
            UserId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
    }

    public bool RestablecimientoDisponible()
    {
        return !string.IsNullOrWhiteSpace(_smtp.Host)
            && _smtp.Port > 0
            && !string.IsNullOrWhiteSpace(_smtp.FromEmail);
    }

    public async Task<MessageResponse> SolicitarRestablecimiento(string? email, CancellationToken cancellationToken = default)
    {
        var normalizado = NormalizarCorreo(email);
        var usuario = await _context.Users.FirstOrDefaultAsync(user => user.Email == normalizado, cancellationToken);
        if (usuario is null)
            return new MessageResponse { Mensaje = MensajeRestablecimiento };

        var token = CrearTokenRestablecimiento();
        usuario.PasswordResetTokenHash = HashToken(token);
        usuario.PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(ResetTokenMinutes);
        await _context.SaveChangesAsync(cancellationToken);

        var baseUrl = string.IsNullOrWhiteSpace(_app.PublicUrl)
            ? "https://cun-book.vercel.app"
            : _app.PublicUrl.Trim().TrimEnd('/');
        var enlace = $"{baseUrl}/restablecer-contrasena?token={Uri.EscapeDataString(token)}";

        try
        {
            var enviado = await _emailService.SendPasswordResetAsync(usuario.Email, usuario.Nombre, enlace);
            if (!enviado)
            {
                usuario.PasswordResetTokenHash = null;
                usuario.PasswordResetTokenExpires = null;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            usuario.PasswordResetTokenHash = null;
            usuario.PasswordResetTokenExpires = null;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "No se pudo enviar el correo de restablecimiento a {Email}.", usuario.Email);
        }

        return new MessageResponse { Mensaje = MensajeRestablecimiento };
    }

    public async Task<MessageResponse> RestablecerContrasena(string? token, string? password, CancellationToken cancellationToken = default)
    {
        ValidarPassword(password);
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
            throw new ApiException("El enlace de restablecimiento no es válido o ya expiró.", StatusCodes.Status400BadRequest);

        var hash = HashToken(token.Trim());
        var ahora = DateTime.UtcNow;
        var nuevoHash = BCrypt.Net.BCrypt.HashPassword(password);
        var filas = await _context.Users
            .Where(user => user.PasswordResetTokenHash == hash
                && user.PasswordResetTokenExpires != null
                && user.PasswordResetTokenExpires > ahora)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.PasswordHash, nuevoHash)
                .SetProperty(user => user.PasswordResetTokenHash, (string?)null)
                .SetProperty(user => user.PasswordResetTokenExpires, (DateTime?)null), cancellationToken);

        if (filas == 0)
            throw new ApiException("El enlace de restablecimiento no es válido o ya expiró.", StatusCodes.Status400BadRequest);

        _logger.LogInformation("Se restableció una contraseña con un token de un solo uso.");
        return new MessageResponse { Mensaje = "Contraseña actualizada. Ya puedes iniciar sesión." };
    }

    public static string NormalizarCorreo(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ApiException("El correo es obligatorio.", StatusCodes.Status400BadRequest);

        var normalizado = email.Trim().ToLowerInvariant();
        if (!CorreoUniversitario.IsMatch(normalizado))
            throw new ApiException(MensajeCorreoUniversitario, StatusCodes.Status400BadRequest);

        return normalizado;
    }

    private string CrearToken(User usuario, DateTime expira)
    {
        if (string.IsNullOrWhiteSpace(_jwt.Key) || _jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

        var claims = new List<Claim>
        {
            new("sub", usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new("name", usuario.Nombre),
            new("role", usuario.Rol.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    internal static bool PasswordCoincide(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static RolUsuario ResolverRolPublico(string? rol)
    {
        if (string.IsNullOrWhiteSpace(rol) || rol.Equals("Estudiante", StringComparison.OrdinalIgnoreCase))
            return RolUsuario.Estudiante;
        if (rol.Equals("Docente", StringComparison.OrdinalIgnoreCase))
            return RolUsuario.Docente;
        throw new ApiException("El rol debe ser Estudiante o Docente.", StatusCodes.Status400BadRequest);
    }

    private static string CrearTokenRestablecimiento()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static void ValidarPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ApiException("La contraseña es obligatoria.", StatusCodes.Status400BadRequest);
        if (password.Length < 6)
            throw new ApiException("La contraseña debe tener al menos 6 caracteres.", StatusCodes.Status400BadRequest);
        if (password.Length > 72)
            throw new ApiException("La contraseña no puede superar 72 caracteres.", StatusCodes.Status400BadRequest);
    }

    internal static UserProfileResponse MapProfile(User usuario)
    {
        return new UserProfileResponse
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            Telefono = usuario.Telefono,
            Facultad = usuario.Facultad,
            FechaRegistro = usuario.FechaRegistro
        };
    }
}
