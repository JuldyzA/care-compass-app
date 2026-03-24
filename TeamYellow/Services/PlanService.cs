using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

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

        /// <summary>
        /// Updates a plan and its related features using the submitted view model.
        /// </summary>
        /// <param name="vm">The view model containing the updated plan data.</param>
        /// <returns><c>true</c> if the update succeeds; otherwise <c>false</c>.</returns>
        public async Task<bool> UpdatePlansWithFeaturesAsync(PlanVM vm)
        {
            var plan = new Plan
            {
                PlanId = vm.PlanId,
                PlanName = (vm.PlanName ?? string.Empty).Trim(),
                PlanDescription = (vm.PlanDescription ?? string.Empty).Trim(),
                Price = vm.Price,
                IsActive = vm.IsActive,
                PlanFeatures = vm.PlanFeatures
                    .Select(f => new PlanFeature
                    {
                        PlanFeatureId = f.PlanFeatureId,
                        FeatureName = f.FeatureName?.Trim() ?? string.Empty,
                        FeatureDescription = f.FeatureDescription?.Trim() ?? string.Empty
                    })
                    .ToList()
            };

            return await _planRepository.UpdatePlansWithFeaturesAsync(plan);
        }
    }
}
