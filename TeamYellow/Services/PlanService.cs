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
        /// Retrieves a single subscription plan by its primary key.
        /// </summary>
        /// <param name="id">The primary key of the plan to retrieve.</param>
        /// <returns>The matching <see cref="Plan"/>, or <c>null</c> if not found.</returns>
        public async Task<Plan?> GetPlanById(int id)
        {
            return await _planRepository.GetByIdWithFeaturesAsync(id);
        }
    }
}
