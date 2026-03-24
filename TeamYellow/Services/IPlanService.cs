using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Services
{
    /// <summary>
    /// Defines operations for creating and capturing PayPal checkout orders.
    /// </summary>
    public interface IPlanService
    {
        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetPlanById(int id);

        Task<bool> UpdatePlansWithFeaturesAsync(PlanVM vm);
    }
}
