using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public interface IPlanRepository
    {
        Task<IEnumerable<Plan>> GetAllAsync();

        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetByIdWithFeaturesAsync(int id);

        Task<bool> UpdatePlansWithFeaturesAsync(Plan updatedPlan);
    }
}