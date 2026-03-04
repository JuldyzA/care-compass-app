using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Services
{
    public interface IPlanService
    {
        Task<List<Plan>> GetActivePlans();

        Task<List<Plan>> GetAllPlans();

        Task<Plan?> GetPlanById(int id);

        Task<Plan> AddPlan(AddPlanDto addPlanDto);

        Task<Plan> UpdatePlan(UpdatePlanDto updatePlanDto);

        Task<bool> DeletePlan(int id);
    }
}
