using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Repositories
{
    public interface IPlanRepository
    {
        Task<IEnumerable<Plan>> GetAllAsync();

        Task<Plan?> GetById(int id);

        Task<bool> UpdateAsync(Plan entity);

        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetPlanById(int id);

        Task<Plan?> GetByIdWithFeaturesAsync(int id);

        Task<bool> UpdatePlansWithFeaturesAsync(PlanVM vm);
    }
}