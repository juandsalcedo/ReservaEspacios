using ReservaEspacios.Api.DTOs;

namespace ReservaEspacios.Api.Services;

public interface ISpaceHorariosService
{
    Task<IReadOnlyList<SpaceHorarioResponse>> GetAll(int? spaceId, CancellationToken cancellationToken = default);
    Task<SpaceHorarioResponse> GetById(int id, CancellationToken cancellationToken = default);
    Task<SpaceHorarioResponse> Create(int userId, CreateSpaceHorarioRequest request, CancellationToken cancellationToken = default);
    Task<SpaceHorarioResponse> Update(int id, int userId, UpdateSpaceHorarioRequest request, CancellationToken cancellationToken = default);
    Task<SpaceHorarioResponse> Delete(int id, int userId, CancellationToken cancellationToken = default);
}
