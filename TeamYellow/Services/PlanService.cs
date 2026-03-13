using TeamYellow.DTOs;
using TeamYellow.Models;
using TeamYellow.Repositories;

namespace TeamYellow.Services
{
    /// <summary>
    /// Service that implements plan management business logic by delegating
    /// data access operations to <see cref="IPlanRepository"/>.
    /// </summary>
    public class PlanService(IPlanRepository planRepository) : IPlanService
    {
        private readonly IPlanRepository _planRepository = planRepository;

        /// <summary>
        /// Retrieves all currently active subscription plans.
        /// </summary>
        /// <returns>A list of active <see cref="Plan"/> entities.</returns>
        public async Task<List<Plan>> GetActivePlans()
        {
            return await _planRepository.GetActivePlans();
        }

        /// <summary>
        /// Retrieves all subscription plans, including inactive ones.
        /// </summary>
        /// <returns>A list of all <see cref="Plan"/> entities.</returns>
        public async Task<List<Plan>> GetAllPlans()
        {
            return await _planRepository.GetAllPlans();
        }

        /// <summary>
        /// Retrieves a single subscription plan by its primary key.
        /// </summary>
        /// <param name="id">The primary key of the plan to retrieve.</param>
        /// <returns>The matching <see cref="Plan"/>, or <c>null</c> if not found.</returns>
        public async Task<Plan?> GetPlanById(int id)
        {
            return await _planRepository.GetPlanById(id);
        }

        /// <summary>
        /// Creates a new subscription plan from the provided DTO.
        /// </summary>
        /// <param name="addPlanDto">The DTO containing the new plan data.</param>
        /// <returns>The newly created <see cref="Plan"/> entity.</returns>
        public async Task<Plan> AddPlan(AddPlanDto addPlanDto)
        {
            return await _planRepository.AddPlan(addPlanDto);
        }

        /// <summary>
        /// Updates an existing subscription plan using the provided DTO.
        /// </summary>
        /// <param name="updatePlanDto">The DTO containing updated plan data.</param>
        /// <returns>The updated <see cref="Plan"/> entity.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no plan with the given ID exists.</exception>
        public async Task<Plan> UpdatePlan(UpdatePlanDto updatePlanDto)
        {
            return await _planRepository.UpdatePlan(updatePlanDto);
        }

        /// <summary>
        /// Soft-deletes the specified plan by marking it as inactive.
        /// </summary>
        /// <param name="id">The ID of the plan to delete.</param>
        /// <returns><c>true</c> if the plan was found and deactivated; <c>false</c> otherwise.</returns>
        public async Task<bool> DeletePlan(int id)
        {
            return await _planRepository.DeletePlan(id);
        }
    }
}
