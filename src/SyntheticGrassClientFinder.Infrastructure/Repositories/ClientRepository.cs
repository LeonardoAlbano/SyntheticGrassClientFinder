using Microsoft.EntityFrameworkCore;
using SyntheticGrassClientFinder.Domain.Entities;
using SyntheticGrassClientFinder.Domain.Repositories;
using SyntheticGrassClientFinder.Domain.ValueObjects;
using SyntheticGrassClientFinder.Infrastructure.DataAccess;

namespace SyntheticGrassClientFinder.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly SyntheticGrassDbContext _context;

    public ClientRepository(SyntheticGrassDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clients.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Client>> GetByLocationAsync(Location center, double radiusKm, CancellationToken cancellationToken = default)
    {
        var clients = await _context.Clients.ToListAsync(cancellationToken);
        return clients.Where(c => c.Location.DistanceTo(center) <= radiusKm);
    }

    public async Task<IEnumerable<Client>> GetByTypeAsync(ClientType type, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.Where(c => c.Type == type).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Client>> GetByStatusAsync(ClientStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.Where(c => c.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Client>> GetByCityAsync(string city, string state, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .Where(c => c.Address.City.ToLower() == city.ToLower() && c.Address.State.ToUpper() == state.ToUpper())
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Client> clients, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddRangeAsync(clients, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ClientId id, CancellationToken cancellationToken = default)
    {
        var client = await GetByIdAsync(id, cancellationToken);
        if (client != null)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string name, Address address, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.AnyAsync(c => 
            c.Name.ToLower() == name.ToLower() && 
            c.Address.Street.ToLower() == address.Street.ToLower() &&
            c.Address.City.ToLower() == address.City.ToLower(), 
            cancellationToken);
    }

    public async Task<int> CountByStatusAsync(ClientStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.CountAsync(c => c.Status == status, cancellationToken);
    }
}
