using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    /// <summary>
    /// Handles plan browsing and checkout workflows, including plan listing,
    /// discount application, and checkout preparation.
    /// </summary>
    [Authorize]
    public class PlanController : Controller
    {
        private readonly IPlanService _planService;
        private readonly CounsellorRepository _counsellorRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly DiscountRepository _discountRepository;
        private readonly UserProfileRepository _userProfileRepository;
        private readonly UserManager<IdentityUser> _userManager;

        /// <summary>
        /// Initializes a new instance of <see cref="PlanController"/>.
        /// </summary>
        /// <param name="planService">Service used to retrieve and manage plans.</param>
        /// <param name="counsellorRepository">Repository for counsellor data access.</param>
        /// <param name="subscriptionRepository">Repository for subscription data access.</param>
        /// <param name="discountRepository">Repository for discount data access.</param>
        /// <param name="userProfileRepository">Repository for user profile data access.</param>
        /// <param name="userManager">ASP.NET Identity user manager.</param>
        public PlanController(
            IPlanService planService,
            CounsellorRepository counsellorRepository,
            ISubscriptionRepository subscriptionRepository,
            DiscountRepository discountRepository,
            UserProfileRepository userProfileRepository,
            UserManager<IdentityUser> userManager)
        {
            _planService = planService;
            _counsellorRepository = counsellorRepository;
            _subscriptionRepository = subscriptionRepository;
            _discountRepository = discountRepository;
            _userProfileRepository = userProfileRepository;
            _userManager = userManager;
        }

        /// <summary>
        /// Displays a list of all active subscription plans.
        /// For authenticated users, injects the current active plan ID into
        /// <see cref="Controller.ViewData"/> and indicates whether the user
        /// has already used the free trial or has paid plan history so the view
        /// can adjust available actions.
        /// </summary>
        /// <returns>The plan listing view with a list of <see cref="PlanVM"/> objects.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userContext = await PopulateDashboardUserProfileAsync();

            var plans = await _planService.GetActivePlans();

            bool hasUsedFreeTrial = false;
            bool hasPaidPlanHistory = false;

            if (userContext?.Counsellor != null)
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(userContext.Counsellor.CounsellorId);
                ViewData["CurrentPlanId"] = subscription?.PlanId;

                hasUsedFreeTrial = await _subscriptionRepository.HasUsedFreeTrialAsync(userContext.Counsellor.CounsellorId);
                hasPaidPlanHistory = await _subscriptionRepository.HasPaidPlanHistoryAsync(userContext.Counsellor.CounsellorId);
            }

            ViewData["HasUsedFreeTrial"] = hasUsedFreeTrial;
            ViewData["HasPaidPlanHistory"] = hasPaidPlanHistory;

            if (TempData.ContainsKey("Message"))
                ViewData["Message"] = TempData["Message"];
            if (TempData.ContainsKey("MessageType"))
                ViewData["MessageType"] = TempData["MessageType"];

            return View(plans.Select(MapToPlanVM).ToList());
        }

        /// <summary>
        /// Displays the checkout page for the specified plan.
        /// Redirects back to the plan index with an appropriate message if the user
        /// is already subscribed to the requested plan, if a free counsellor attempts
        /// to re-open the free plan, or if a paid counsellor attempts to downgrade
        /// to the free plan. Free-trial eligibility is enforced later in the
        /// subscription workflow.
        /// </summary>
        /// <param name="id">The ID of the plan the user wants to check out.</param>
        /// <returns>
        /// The checkout view for the requested plan, or a redirect/not-found result
        /// if the plan is unavailable or the requested change is not allowed.
        /// </returns>
        [Authorize(Roles = "Registered_Visitor,Paid_Counselor,Free_Counselor")]
        public async Task<IActionResult> Checkout(int id, string? discountCode = null)
        {
            var userContext = await PopulateDashboardUserProfileAsync();

            var plan = await _planService.GetPlanById(id);

            if (plan == null || !plan.IsActive)
                return NotFound();

            if (User.IsInRole("Free_Counselor") && plan.Price == 0)
            {
                TempData["Message"] = "You are already subscribed to this plan.";
                TempData["MessageType"] = "info";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Paid_Counselor"))
            {
                if (plan.Price == 0)
                {
                    TempData["Message"] = "You cannot downgrade to the Free plan.";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction(nameof(Index));
                }

                if (userContext?.Counsellor != null)
                {
                    var subscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(userContext.Counsellor.CounsellorId);
                    if (subscription?.PlanId == id)
                    {
                        TempData["Message"] = "You are already subscribed to this plan.";
                        TempData["MessageType"] = "info";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            var vm = MapToCheckoutVM(plan);

            if (TempData["CheckoutMessage"] is string checkoutMessage &&
                !string.IsNullOrWhiteSpace(checkoutMessage))
            {
                vm.DiscountMessage = checkoutMessage;
            }

            if (plan.Price == 0m)
            {
                vm.FinalAmount = 0m;
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                vm.DiscountCode = discountCode.Trim().ToUpperInvariant();

                var discount = await _discountRepository.GetValidDiscountForPlanAsync(plan.PlanId, vm.DiscountCode);

                if (discount == null)
                {
                    vm.DiscountApplied = false;
                    vm.AppliedDiscountId = null;
                    vm.DiscountAmount = 0m;
                    vm.FinalAmount = plan.Price;

                    if (string.IsNullOrWhiteSpace(vm.DiscountMessage))
                        vm.DiscountMessage = "Invalid, expired, or ineligible discount code.";
                }
                else
                {
                    vm.AppliedDiscountId = discount.DiscountId;
                    vm.DiscountAmount = DiscountCalculator.CalculateDiscountAmount(plan.Price, discount);
                    vm.FinalAmount = decimal.Round(
                        Math.Max(0m, plan.Price - vm.DiscountAmount),
                        2,
                        MidpointRounding.AwayFromZero);
                    vm.DiscountApplied = true;
                    vm.DiscountMessage = "Discount code applied successfully.";
                }
            }

            return View(vm);
        }

        /// <summary>
        /// Adds profile display values used by the dashboard layout when this controller is reached from dashboard navigation.
        /// </summary>
        private async Task<PlanUserContext?> PopulateDashboardUserProfileAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);
            if (!string.IsNullOrWhiteSpace(userProfile?.ProfilePhotoUrl))
            {
                ViewData["UserProfilePicture"] = userProfile.ProfilePhotoUrl;
            }

            string? displayName = null;
            var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
            if (!string.IsNullOrWhiteSpace(counsellor?.DisplayName))
            {
                displayName = counsellor.DisplayName.Trim();
            }
            else
            {
                string firstName = userProfile?.FirstName?.Trim() ?? string.Empty;
                string lastName = userProfile?.LastName?.Trim() ?? string.Empty;
                displayName = $"{firstName} {lastName}".Trim();

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = await _userManager.GetUserNameAsync(user) ?? user.Email;
                }
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                ViewData["DisplayName"] = displayName;
            }

            return new PlanUserContext(user, counsellor);
        }

        private sealed record PlanUserContext(IdentityUser User, Counsellor? Counsellor);

        /// <summary>
        /// Redirects back to the checkout page with the submitted discount code so it can be validated and applied.
        /// </summary>
        /// <param name="vm">The checkout view model containing the selected plan and entered discount code.</param>
        /// <returns>A redirect to the checkout action for the selected plan.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Registered_Visitor,Paid_Counselor,Free_Counselor")]
        public IActionResult ApplyDiscount(CheckoutVM vm)
        {
            var normalizedCode = vm.DiscountCode?.Trim();

            return RedirectToAction(nameof(Checkout), new
            {
                id = vm.PlanId,
                discountCode = string.IsNullOrWhiteSpace(normalizedCode) ? null : normalizedCode.ToUpperInvariant()
            });
        }

        /// <summary>
        /// Maps a <see cref="Plan"/> domain model to a <see cref="PlanVM"/> view model.
        /// </summary>
        /// <param name="plan">The plan domain model to map.</param>
        /// <returns>A <see cref="PlanVM"/> populated from the given <paramref name="plan"/>.</returns>
        private static PlanVM MapToPlanVM(Plan plan) => new()
        {
            PlanId = plan.PlanId,
            PlanName = plan.PlanName,
            PlanDescription = plan.PlanDescription,
            Price = plan.Price,
            BillingType = plan.BillingType,
            IsActive = plan.IsActive,
            PlanFeatures = [.. plan.PlanFeatures.Select(f => new PlanFeatureVM
            {
                FeatureName = f.FeatureName,
                FeatureDescription = f.FeatureDescription
            })]
        };

        /// <summary>
        /// Maps a <see cref="Plan"/> domain model to a <see cref="CheckoutVM"/> view model.
        /// </summary>
        /// <param name="plan">The plan domain model to map.</param>
        /// <returns>A <see cref="CheckoutVM"/> populated from the given <paramref name="plan"/>.</returns>
        private static CheckoutVM MapToCheckoutVM(Plan plan) => new()
        {
            PlanId = plan.PlanId,
            PlanName = plan.PlanName,
            PlanDescription = plan.PlanDescription,
            BillingType = plan.BillingType,
            OriginalPrice = plan.Price,
            FinalAmount = plan.Price,
            PlanFeatures = [.. plan.PlanFeatures.Select(f => new PlanFeatureVM
            {
                FeatureName = f.FeatureName,
                FeatureDescription = f.FeatureDescription
            })]
        };
    }
}