using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    private static readonly Regex CorreoUniversitario = new(
        @"^[^@\s]+@cun\.edu\.co$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly ReservaEspaciosContext _context;
    private readonly JwtOptions _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ReservaEspaciosContext context, IOptions<JwtOptions> jwtOptions, ILogger<AuthService> logger)
    {
        _context = context;
        _jwt = jwtOptions.Value;
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
            Rol = RolUsuario.Estudiante,
            Telefono = telefono,
            Facultad = facultad,
            FechaRegistro = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.Users.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario {Email} registrado como estudiante.", usuario.Email);
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
