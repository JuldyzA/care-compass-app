using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Defines data access operations for <see cref="Plan"/> entities.
    /// </summary>
    public interface IPlanRepository
    {
        Task<IEnumerable<Plan>> GetAllAsync();

        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetByIdWithFeaturesAsync(int id);

        Task<bool> UpdatePlansWithFeaturesAsync(Plan updatedPlan);
    }
}