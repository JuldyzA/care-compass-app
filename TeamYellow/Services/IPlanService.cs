using TeamYellow.Models;

namespace TeamYellow.Services
{
    public interface IPlanService
    {
        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetPlanById(int id);
    }
}
