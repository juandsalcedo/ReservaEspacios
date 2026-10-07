using ReservaEspacios.Api.DTOs;

namespace ReservaEspacios.Api.Services;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationResponse>> GetUserReservations(int userId, CancellationToken cancellationToken = default);
    Task<ReservationResponse> GetReservation(int id, int userId, CancellationToken cancellationToken = default);
    Task<ReservationResponse> CreateReservation(int userId, CreateReservationRequest request, CancellationToken cancellationToken = default);
    Task<ReservationResponse> UpdateReservation(int id, int userId, UpdateReservationRequest request, CancellationToken cancellationToken = default);
    Task<ReservationResponse> CancelReservation(int id, int userId, CancellationToken cancellationToken = default);
    Task ValidateAvailability(int spaceId, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, int? reservaIdExcluir = null, CancellationToken cancellationToken = default);
}
