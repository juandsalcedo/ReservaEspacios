using ReservaEspacios.Api.DTOs;

namespace ReservaEspacios.Api.Services;

public interface ISpaceService
{
    Task<IReadOnlyList<SpaceResponse>> GetAllSpaces(CancellationToken cancellationToken = default);
    Task<SpaceResponse> GetSpaceById(int id, CancellationToken cancellationToken = default);
    Task<SpaceResponse> CreateSpace(int userId, CreateSpaceRequest request, CancellationToken cancellationToken = default);
    Task<SpaceResponse> UpdateSpace(int id, int userId, UpdateSpaceRequest request, CancellationToken cancellationToken = default);
    Task<SpaceResponse> DeleteSpace(int id, int userId, CancellationToken cancellationToken = default);
}
