using SyntheticGrassClientFinder.Domain.Entities;
using SyntheticGrassClientFinder.Domain.ValueObjects;

namespace SyntheticGrassClientFinder.Domain.Repositories;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Client>> GetByLocationAsync(Location center, double radiusKm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Client>> GetByTypeAsync(ClientType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Client>> GetByStatusAsync(ClientStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Client>> GetByCityAsync(string city, string state, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Client> clients, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task DeleteAsync(ClientId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, Address address, CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(ClientStatus status, CancellationToken cancellationToken = default);
}