using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Services
{
    public interface IPlanService
    {
        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetPlanById(int id);

        Task<bool> UpdatePlansWithFeaturesAsync(PlanVM vm);
    }
}
