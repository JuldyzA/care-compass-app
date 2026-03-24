using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Services
{
    /// <summary>
    /// Defines plan management operations for retrieving and updating subscription plans.
    /// </summary>
    public interface IPlanService
    {
        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetPlanById(int id);

        Task<bool> UpdatePlansWithFeaturesAsync(PlanVM vm);
    }
}
