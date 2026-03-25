using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Repositories;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    /// <summary>
    /// Handles subscription workflows, including PayPal checkout, success and failure callbacks,
    /// cancellation handling, and counsellor role assignment.
    /// </summary>
    [Authorize(Roles = "Registered_Visitor,Paid_Counselor,Free_Counselor")]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly CounsellorRepository _counsellorRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly UserProfileRepository _userProfileRepository;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<SubscriptionController> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="SubscriptionController"/>.
        /// </summary>
        /// <param name="subscriptionService">Service handling subscription business logic.</param>
        /// <param name="userManager">ASP.NET Identity user manager.</param>
        /// <param name="counsellorRepository">Repository for counsellor data access.</param>
        /// <param name="subscriptionRepository">Repository for subscription data access.</param>
        /// <param name="userProfileRepository">Repository for user profile data access.</param>
        /// <param name="signInManager">ASP.NET Identity sign-in manager used to refresh user claims.</param>
        /// <param name="logger">Logger for recording error and diagnostic information.</param>
        public SubscriptionController(
            ISubscriptionService subscriptionService,
            UserManager<IdentityUser> userManager,
            CounsellorRepository counsellorRepository,
            ISubscriptionRepository subscriptionRepository,
            UserProfileRepository userProfileRepository,
            SignInManager<IdentityUser> signInManager,
            ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _userManager = userManager;
            _counsellorRepository = counsellorRepository;
            _subscriptionRepository = subscriptionRepository;
            _userProfileRepository = userProfileRepository;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Initiates the subscription process for the specified plan.
        /// Creates a counsellor profile if one does not yet exist for the current user.
        /// True free plans and paid plans discounted to zero are completed immediately without PayPal.
        /// Paid plans with a remaining balance create a PayPal order and redirect the user
        /// to the PayPal approval page.
        /// </summary>
        /// <param name="planId">The ID of the plan the user wants to subscribe to.</param>
        /// <param name="discountCode">Optional discount code applied during checkout.</param>
        /// <returns>
        /// Redirects to the PayPal approval URL for paid plans requiring payment,
        /// returns a success view for zero-amount flows,
        /// or returns an error result for invalid requests.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int planId, string? discountCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var returnUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
            var cancelUrl = Url.Action("Cancel", "Subscription", null, Request.Scheme);

            if (returnUrl is null || cancelUrl is null)
                return BadRequest("Unable to generate PayPal redirect URLs.");

            try
            {
                var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);

                if (counsellor != null)
                {
                    var existingSubscription =
                        await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellor.CounsellorId);

                    if (existingSubscription?.PlanId == planId)
                    {
                        TempData["Message"] = "You are already subscribed to this plan.";
                        TempData["MessageType"] = "info";
                        return RedirectToAction("Index", "Plan");
                    }
                }

                var checkout =
                    await _subscriptionService.CreatePayPalOrder(planId, discountCode, returnUrl, cancelUrl);

                if (!checkout.RequiresPayPal)
                {
                    counsellor = await EnsureCounsellorAsync(user.Id, user.UserName, user.Email, counsellor);

                    var payerName = counsellor.DisplayName;

                    SubscriptionResult result;
                    string successMessage;
                    string roleToAssign;

                    if (checkout.IsActualFreePlan)
                    {
                        result = await _subscriptionService.SubscribeFree(
                            counsellor.CounsellorId,
                            payerName,
                            planId);

                        successMessage = result == SubscriptionResult.PlanChanged
                            ? "Your plan has been updated successfully."
                            : "You have successfully subscribed to the Free plan.";

                        roleToAssign = "Free_Counselor";
                    }
                    else
                    {
                        result = await _subscriptionService.SubscribeDiscountedZeroAmount(
                            counsellor.CounsellorId,
                            payerName,
                            planId,
                            checkout.DiscountId);

                        successMessage = result == SubscriptionResult.PlanChanged
                            ? "Your plan has been updated successfully."
                            : "Your subscription has been activated successfully.";

                        roleToAssign = "Paid_Counselor";
                    }

                    if (result == SubscriptionResult.AlreadySubscribed)
                    {
                        TempData["Message"] = "You are already subscribed to this plan.";
                        TempData["MessageType"] = "info";
                        return RedirectToAction("Index", "Plan");
                    }

                    try
                    {
                        await AssignCounsellorRoleAsync(user.Id, roleToAssign);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to assign {Role} role to user {UserId}.",
                            roleToAssign,
                            user.Id);

                        TempData["Message"] =
                            "Subscription was created, but updating your account role failed. Please contact support or try again.";
                        TempData["MessageType"] = "danger";
                        return RedirectToAction("Index", "Plan");
                    }

                    var subscription =
                        await _subscriptionRepository.GetActiveSubscriptionWithPlanByCounsellorId(counsellor.CounsellorId);

                    var vm = new SubscriptionSuccessVM
                    {
                        Message = successMessage,
                        SubscriptionId = subscription?.SubscriptionId ?? 0,
                        PlanName = subscription?.Plan?.PlanName ?? string.Empty,
                        CycleStart = subscription?.CycleStart ?? DateTime.UtcNow,
                        CycleEnd = subscription?.CycleEnd ?? DateTime.UtcNow,
                        DisplayName = counsellor.DisplayName ?? string.Empty,
                        Amount = subscription?.PaymentTransaction?.Amount ?? 0,
                        Currency = subscription?.PaymentTransaction?.Currency ?? "CAD",
                        PaidAt = subscription?.PaymentTransaction?.PaidAt ?? DateTime.MinValue,
                        ProviderOrderId = subscription?.PaymentTransaction?.ProviderOrderId ?? string.Empty

                    };

                    return View("Success", vm);
                }

                return Redirect(checkout.ApprovalUrl!);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex,
                    "Key not found for user email {Email}. Redirecting to plan selection.",
                    user.Email);

                TempData["Message"] =
                    "We couldn't find the requested subscription information. Please select a plan again.";
                TempData["MessageType"] = "danger";
                return RedirectToAction("Index", "Plan");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid subscription attempt for user {UserId} and plan {PlanId}.",
                    user.Id,
                    planId);

                TempData["CheckoutMessage"] = "We were unable to process the selected plan. Please review your selection and try again.";
                return RedirectToAction("Checkout", "Plan", new { id = planId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing subscription payment for user {UserId} and plan {PlanId}.",
                    user.Id,
                    planId);

                TempData["Message"] =
                    "An unexpected error occurred while processing your subscription. Please try again.";
                TempData["MessageType"] = "danger";
                return RedirectToAction("Index", "Plan");
            }
        }

        /// <summary>
        /// Handles the PayPal payment return callback after the user approves the order.
        /// Captures the payment, creates the subscription record, and assigns the <c>Paid_Counselor</c> role.
        /// </summary>
        /// <param name="orderId">
        /// The PayPal order token returned as a query string parameter named <c>token</c>
        /// from the PayPal approval redirect.
        /// </param>
        /// <returns>
        /// The success view with a confirmation message, or a redirect to the plan page with an error
        /// if the payment token is missing, the counsellor profile is not found, or the payment fails.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Success([FromQuery(Name = "token")] string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                TempData["Message"] = "Payment token is missing. Please try again.";
                TempData["MessageType"] = "danger";
                return RedirectToAction("Index", "Plan");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
                counsellor = await EnsureCounsellorAsync(user.Id, user.UserName, user.Email, counsellor);

                var payerName = counsellor.DisplayName;

                var result = await _subscriptionService.CompletePayPalSubscription(
                    orderId,
                    counsellor.CounsellorId,
                    payerName);

                if (result == SubscriptionResult.AlreadySubscribed)
                {
                    TempData["Message"] = "You are already subscribed to this plan.";
                    TempData["MessageType"] = "info";
                    return RedirectToAction("Index", "Plan");
                }

                try
                {
                    await AssignCounsellorRoleAsync(user.Id, "Paid_Counselor");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to assign Paid_Counselor role to user {UserId} after PayPal subscription.",
                        user.Id);

                    TempData["Message"] =
                        "Your payment was completed, but updating your account role failed. Please contact support or try signing in again.";
                    TempData["MessageType"] = "danger";
                    return RedirectToAction("Index", "Plan");
                }

                var subscription =
                    await _subscriptionRepository.GetActiveSubscriptionWithPlanByCounsellorId(counsellor.CounsellorId);

                var vm = new SubscriptionSuccessVM
                {
                    Message = result == SubscriptionResult.PlanChanged
                        ? "Your plan has been updated successfully."
                        : "Thank you for your subscription. Your plan is now active.",
                    SubscriptionId = subscription?.SubscriptionId ?? 0,
                    PlanName = subscription?.Plan?.PlanName ?? string.Empty,
                    CycleStart = subscription?.CycleStart ?? DateTime.UtcNow,
                    CycleEnd = subscription?.CycleEnd ?? DateTime.UtcNow,
                    DisplayName = counsellor.DisplayName ?? string.Empty,
                    Amount = subscription?.PaymentTransaction?.Amount ?? 0,
                    Currency = subscription?.PaymentTransaction?.Currency ?? "CAD",
                    PaidAt = subscription?.PaymentTransaction?.PaidAt ?? DateTime.MinValue,
                    ProviderOrderId = subscription?.PaymentTransaction?.ProviderOrderId ?? string.Empty
                };

                return View("Success", vm);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Payment could not be completed for order {OrderId}.", orderId);
                TempData["Message"] = "Your payment could not be completed. Please try again or contact support if the problem persists.";
                TempData["MessageType"] = "warning";
                return RedirectToAction(nameof(Failed));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing subscription for order {OrderId}.", orderId);
                TempData["Message"] = "Your payment could not be completed. Please try again.";
                TempData["MessageType"] = "danger";
                return RedirectToAction(nameof(Failed));
            }
        }

        /// <summary>
        /// Handles the PayPal cancellation callback when the user cancels the payment flow.
        /// </summary>
        /// <returns>The cancellation view informing the user that no charge was made.</returns>
        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }

        /// <summary>
        /// Displays the payment failure page after an unsuccessful subscription attempt.
        /// </summary>
        /// <returns>The failure view.</returns>
        [HttpGet]
        public IActionResult Failed()
        {
            return View();
        }

        /// <summary>
        /// Removes any existing counsellor-related roles from the user and assigns the specified target role.
        /// Refreshes the user's sign-in cookie so the new role takes effect immediately.
        /// </summary>
        /// <param name="userId">The ID of the user whose roles should be updated.</param>
        /// <param name="targetRole">
        /// The role to assign to the user. Expected values are <c>Free_Counselor</c> or <c>Paid_Counselor</c>.
        /// </param>
        private async Task AssignCounsellorRoleAsync(string userId, string targetRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning(
                    "User {UserId} not found while assigning role {TargetRole}.",
                    userId,
                    targetRole);

                throw new InvalidOperationException("User not found while assigning counsellor role.");
            }

            var rolesToRemove = new[] { "Registered_Visitor", "Free_Counselor", "Paid_Counselor" };
            var currentRoles = await _userManager.GetRolesAsync(user);
            var toRemove = currentRoles.Intersect(rolesToRemove).ToList();

            if (toRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);

                if (!removeResult.Succeeded)
                {
                    var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to remove roles {Roles} from user {UserId}. Errors: {Errors}",
                        string.Join(", ", toRemove),
                        userId,
                        errors);

                    throw new InvalidOperationException(
                        $"Failed to remove existing counsellor roles: {errors}");
                }
            }

            if (!await _userManager.IsInRoleAsync(user, targetRole))
            {
                var addResult = await _userManager.AddToRoleAsync(user, targetRole);

                if (!addResult.Succeeded)
                {
                    var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to add role {TargetRole} to user {UserId}. Errors: {Errors}",
                        targetRole,
                        userId,
                        errors);

                    throw new InvalidOperationException($"Failed to assign role {targetRole}: {errors}");
                }
            }

            await _signInManager.RefreshSignInAsync(user);
        }

        /// <summary>
        /// Returns the existing counsellor profile for the user, or creates a new counsellor record if one does not exist.
        /// The display name is built from the user's profile when available, and falls back to the user name or email.
        /// </summary>
        /// <param name="userId">The identity user ID.</param>
        /// <param name="userName">The user's user name.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="existingCounsellor">The existing counsellor record, if already loaded.</param>
        /// <returns>The existing or newly created counsellor record.</returns>
        private async Task<Models.Counsellor> EnsureCounsellorAsync(
            string userId,
            string? userName,
            string? email,
            Models.Counsellor? existingCounsellor)
        {
            if (existingCounsellor != null)
            {
                return existingCounsellor;
            }

            string licenceId;
            var random = new Random();

            do
            {
                licenceId = $"{(char)('A' + random.Next(0, 26))}{random.Next(100000, 1000000)}";
            }
            while (await _counsellorRepository.LicenceIdExistsAsync(licenceId));

            var profile = await _userProfileRepository.GetByUserIdAsync(userId);

            var displayName = string.Join(" ", new[]
            {
                profile?.FirstName,
                profile?.LastName
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = userName ?? email ?? "Unknown";
            }

            return await _counsellorRepository.CreateAsync(new Models.Counsellor
            {
                UserId = userId,
                DisplayName = displayName,
                PractitionerLicenceId = licenceId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}