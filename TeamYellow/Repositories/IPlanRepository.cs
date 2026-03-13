using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public interface IPlanRepository
    {
        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetPlanById(int id);
    }
}