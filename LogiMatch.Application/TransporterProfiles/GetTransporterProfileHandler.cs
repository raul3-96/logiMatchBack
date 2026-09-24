using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.TransporterProfiles;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransporterProfiles;

public class GetTransporterProfileHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetTransporterProfileHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<DetailedTransporterProfileDto?> Handle(Guid id)
    {
        var transporterProfile = await _dbContext.TransporterProfiles
            .FirstOrDefaultAsync(x => x.Id == id);

        if (transporterProfile == null)
            throw new NotFoundException(
                "The specified transporter profile does not exist.");

        // Si el usuario autenticado es el propietario, mostrar datos detallados
        var currentUserId = _currentUserService.UserId;
        if (transporterProfile.UserId == currentUserId)
        {
            var detailedProfile = await HandleDetailed(id);
            return detailedProfile;
        }

        return new DetailedTransporterProfileDto{
                Id = transporterProfile.Id,
                FirstName = _dbContext.Users
                            .Where(u => u.Id == transporterProfile.UserId)
                            .Select(u => u.FirstName)
                            .FirstOrDefault() ?? string.Empty,
                LastName = _dbContext.Users
                            .Where(u => u.Id == transporterProfile.UserId)
                            .Select(u => u.LastName)
                            .FirstOrDefault() ?? string.Empty,
                CompanyName = transporterProfile.CompanyId == null
                            ? null
                            : _dbContext.Companies
                                .Where(c => c.Id == transporterProfile.CompanyId)
                                .Select(c => c.Name)
                                .FirstOrDefault(),
                CreatedAt = transporterProfile.CreatedAt,

            };
    }

    public async Task<DetailedTransporterProfileDto?> HandleDetailed(Guid id)
    {
        return await _dbContext.TransporterProfiles
            .Where(x => x.Id == id)
            .Select(tp => new DetailedTransporterProfileDto
            {
                Id = tp.Id,
                UserId = tp.UserId,
                FirstName = _dbContext.Users
                    .Where(u => u.Id == tp.UserId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault() ?? string.Empty,
                LastName = _dbContext.Users
                    .Where(u => u.Id == tp.UserId)
                    .Select(u => u.LastName)
                    .FirstOrDefault() ?? string.Empty,
                Email = _dbContext.Users
                    .Where(u => u.Id == tp.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefault() ?? string.Empty,
                Phone = _dbContext.Users
                    .Where(u => u.Id == tp.UserId)
                    .Select(u => u.Phone)
                    .FirstOrDefault(),
                CompanyName = tp.CompanyId == null
                    ? null
                    : _dbContext.Companies
                        .Where(c => c.Id == tp.CompanyId)
                        .Select(c => c.Name)
                        .FirstOrDefault(),
                TaxId = tp.CompanyId == null
                    ? null
                    : _dbContext.Companies
                        .Where(c => c.Id == tp.CompanyId)
                        .Select(c => c.TaxId)
                        .FirstOrDefault(),
                CreatedAt = tp.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<PublicTransporterProfileDto>> HandleAll(
        Guid? userId = null,
        Guid? companyId = null)
    {
        var query = _dbContext.TransporterProfiles.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId.Value);
        }

        return await query
            .Select(tp => new PublicTransporterProfileDto
            {
                Id = tp.Id,
                FirstName = _dbContext.Users
                    .Where(u => u.Id == tp.UserId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault() ?? string.Empty,
                LastName = _dbContext.Users
                    .Where(u => u.Id == tp.UserId)
                    .Select(u => u.LastName)
                    .FirstOrDefault() ?? string.Empty,
                CompanyName = tp.CompanyId == null
                    ? null
                    : _dbContext.Companies
                        .Where(c => c.Id == tp.CompanyId)
                        .Select(c => c.Name)
                        .FirstOrDefault(),
                CreatedAt = tp.CreatedAt
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}