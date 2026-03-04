using TeamYellow.DTOs;
using TeamYellow.Models;
using TeamYellow.Repositories;

namespace TeamYellow.Services
{
    public class PlanService(IPlanRepository planRepository) : IPlanService
    {
        private readonly IPlanRepository _planRepository = planRepository;

        public async Task<List<Plan>> GetActivePlans()
        {
            return await _planRepository.GetActivePlans();
        }

        public async Task<List<Plan>> GetAllPlans()
        {
            return await _planRepository.GetAllPlans();
        }

        public async Task<Plan?> GetPlanById(int id)
        {
            return await _planRepository.GetPlanById(id);
        }

        public async Task<Plan> AddPlan(AddPlanDto addPlanDto)
        {
            return await _planRepository.AddPlan(addPlanDto);
        }

        public async Task<Plan> UpdatePlan(UpdatePlanDto updatePlanDto)
        {
            return await _planRepository.UpdatePlan(updatePlanDto);
        }

        public async Task<bool> DeletePlan(int id)
        {
            return await _planRepository.DeletePlan(id);
        }
    }
}
