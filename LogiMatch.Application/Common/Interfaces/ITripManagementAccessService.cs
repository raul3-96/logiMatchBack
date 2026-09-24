namespace LogiMatch.Application.Common.Interfaces;

public interface ITripManagementAccessService
{
    Task<List<Guid>> GetManageableTransporterProfileIdsAsync();
}