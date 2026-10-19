using System.Globalization;
using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Services;

public class EmailService : IEmailService
{
    private const string TimeZoneId = "America/Bogota";
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendReservationConfirmationAsync(
        string emailDestino,
        string nombreUsuario,
        string nombreEspacio,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin)
    {
        if (!EstaConfigurado())
        {
            _logger.LogWarning(
                "SMTP no está configurado (Smtp:Host y Smtp:FromEmail). Se omite el correo de confirmación para {Email}.",
                emailDestino);
            return;
        }

        if (!MailboxAddress.TryParse(emailDestino, out var destinatario))
        {
            _logger.LogWarning("No se envió la confirmación porque el correo {Email} no es válido.", emailDestino);
            return;
        }

        var usuario = Sanitize(nombreUsuario);
        var espacio = Sanitize(nombreEspacio);
        destinatario.Name = usuario;

        using var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        mensaje.To.Add(destinatario);
        mensaje.Subject = $"Confirmación de reserva: {espacio} — {fecha:yyyy-MM-dd}";

        var ics = BuildCalendar(emailDestino, usuario, espacio, fecha, horaInicio, horaFin);
        var icsBytes = Encoding.UTF8.GetBytes(ics);

        var html = new TextPart("html")
        {
            Text = BuildHtmlBody(usuario, espacio, fecha, horaInicio, horaFin)
        };

        var calendario = new MimePart("text", "calendar")
        {
            Content = new MimeContent(new MemoryStream(icsBytes)),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = "reserva.ics"
        };
        calendario.ContentType.Charset = "utf-8";
        calendario.ContentType.Name = "reserva.ics";
        calendario.ContentType.Parameters["method"] = "REQUEST";

        var cuerpo = new Multipart("mixed");
        cuerpo.Add(html);
        cuerpo.Add(calendario);
        mensaje.Body = cuerpo;

        using var client = new SmtpClient
        {
            Timeout = 20_000
        };

        await client.ConnectAsync(_settings.Host, _settings.Port, ResolveSocketOptions());

        if (!string.IsNullOrWhiteSpace(_settings.UserName))
            await client.AuthenticateAsync(_settings.UserName, _settings.Password);

        await client.SendAsync(mensaje);
        await client.DisconnectAsync(true);

        _logger.LogInformation(
            "Correo de confirmación enviado a {Email} para {Espacio} el {Fecha}.",
            emailDestino, espacio, fecha);
    }

    private bool EstaConfigurado()
    {
        return !string.IsNullOrWhiteSpace(_settings.Host)
            && _settings.Port > 0
            && !string.IsNullOrWhiteSpace(_settings.FromEmail);
    }

    private SecureSocketOptions ResolveSocketOptions()
    {
        if (!_settings.UseSsl)
            return SecureSocketOptions.None;

        return _settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private string BuildCalendar(
        string emailDestino,
        string nombreUsuario,
        string nombreEspacio,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin)
    {
        var inicio = fecha.ToDateTime(horaInicio).ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
        var fin = fecha.ToDateTime(horaFin).ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var uid = $"{Guid.NewGuid():N}@reservaespacios.cun";
        var summary = EscapeIcs($"Reserva de espacio: {nombreEspacio}");
        var description = EscapeIcs($"Reserva confirmada para {nombreUsuario} en {nombreEspacio}.");
        var location = EscapeIcs(nombreEspacio);
        var organizador = EscapeIcs(_settings.FromName);
        var asistente = EscapeIcs(nombreUsuario);

        var lineas = new[]
        {
            "BEGIN:VCALENDAR",
            "PRODID:-//Universidad CUN//ReservaEspacios//ES",
            "VERSION:2.0",
            "CALSCALE:GREGORIAN",
            "METHOD:REQUEST",
            "BEGIN:VTIMEZONE",
            $"TZID:{TimeZoneId}",
            $"X-LIC-LOCATION:{TimeZoneId}",
            "BEGIN:STANDARD",
            "TZOFFSETFROM:-0500",
            "TZOFFSETTO:-0500",
            "TZNAME:-05",
            "DTSTART:19700101T000000",
            "END:STANDARD",
            "END:VTIMEZONE",
            "BEGIN:VEVENT",
            $"UID:{uid}",
            $"DTSTAMP:{stamp}",
            $"DTSTART;TZID={TimeZoneId}:{inicio}",
            $"DTEND;TZID={TimeZoneId}:{fin}",
            $"SUMMARY:{summary}",
            $"DESCRIPTION:{description}",
            $"LOCATION:{location}",
            "STATUS:CONFIRMED",
            "SEQUENCE:0",
            $"ORGANIZER;CN={organizador}:mailto:{_settings.FromEmail}",
            $"ATTENDEE;CN={asistente};ROLE=REQ-PARTICIPANT;PARTSTAT=NEEDS-ACTION;RSVP=TRUE:mailto:{emailDestino}",
            "END:VEVENT",
            "END:VCALENDAR"
        };

        return string.Join("\r\n", lineas) + "\r\n";
    }

    private static string BuildHtmlBody(
        string nombreUsuario,
        string nombreEspacio,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin)
    {
        var cultura = CultureInfo.GetCultureInfo("es-CO");
        var fechaTexto = WebUtility.HtmlEncode(fecha.ToString("dddd d 'de' MMMM 'de' yyyy", cultura));
        var usuario = WebUtility.HtmlEncode(nombreUsuario);
        var espacio = WebUtility.HtmlEncode(nombreEspacio);
        var horario = WebUtility.HtmlEncode($"{horaInicio:HH:mm} - {horaFin:HH:mm}");

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <body style="margin:0;padding:24px;background:#f4f6f8;font-family:Arial,sans-serif;color:#1f2937;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;padding:32px;">
                <tr>
                  <td>
                    <p style="margin:0 0 8px;font-size:13px;letter-spacing:.04em;text-transform:uppercase;color:#0f766e;">ReservaEspacios · Universidad CUN</p>
                    <h1 style="margin:0 0 16px;font-size:22px;">Tu reserva quedó confirmada</h1>
                    <p style="margin:0 0 16px;line-height:1.5;">Hola {usuario}, registramos tu reserva en <strong>{espacio}</strong>.</p>
                    <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;background:#f8fafc;border-radius:8px;padding:16px;">
                      <tr><td style="padding:4px 16px;"><strong>Fecha:</strong> {fechaTexto}</td></tr>
                      <tr><td style="padding:4px 16px;"><strong>Horario:</strong> {horario}</td></tr>
                      <tr><td style="padding:4px 16px;"><strong>Espacio:</strong> {espacio}</td></tr>
                    </table>
                    <p style="margin:16px 0 0;line-height:1.5;">Adjuntamos una invitación <strong>.ics</strong> para agregarla a Google Calendar, Outlook o Apple Calendar.</p>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string EscapeIcs(string value)
    {
        return Sanitize(value)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal);
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }
}
