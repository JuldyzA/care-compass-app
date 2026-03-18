using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public interface IPlanRepository
    {
        Task<IEnumerable<Plan>> GetAllAsync();

        Task<Plan?> GetById(int id);

        Task<bool> UpdateAsync(Plan entity);

        Task<List<Plan>> GetActivePlans();

        Task<Plan?> GetPlanById(int id);
    }
}