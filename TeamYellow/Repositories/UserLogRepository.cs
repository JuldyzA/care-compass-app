using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository for reading and writing user login and logout audit logs (UserLogs table).
    /// </summary>
    public class UserLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserLogRepository> _logger;

        public UserLogRepository(ApplicationDbContext context, ILogger<UserLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Returns a paginated, sortable, and filterable list of user logs for display.
        /// </summary>
        /// <param name="emailFilter">An optional email filter.</param>
        /// <param name="abandonedFilter">An optional abandoned-session filter.</param>
        /// <param name="startDate">An optional inclusive start date filter.</param>
        /// <param name="endDate">An optional inclusive end date filter.</param>
        /// <param name="sortOrder">An optional sort order.</param>
        /// <param name="pageNumber">The requested page number.</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>A paginated list of user log view models.</returns>
        public async Task<PaginatedList<UserLogVM>> GetAllAsync(string? emailFilter = null, string? abandonedFilter = null, DateTime? startDate = null, DateTime? endDate = null, string? sortOrder = null, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<UserLogVM> query = _context.UserLogs
                                       .AsNoTracking()
                                       .Select(ul => new UserLogVM
                                       {
                                           LogId = ul.LogId,
                                           Email = ul.User != null ? (ul.User.Email ?? ul.User.UserName ?? "(no email)") : 
                                                (ul.UserEmailSnapshot != null && ul.UserEmailSnapshot != ""
                                                ? ul.UserEmailSnapshot + " (deleted)" : "(deleted account)"),
                                           LogInTime = ul.LogInTime,
                                           LogOutTime = ul.LogOutTime,
                                           Abandoned = ul.Abandoned
                                       });

            if (!string.IsNullOrWhiteSpace(emailFilter))
            {
                string trimmedEmail = emailFilter.Trim();
                query = query.Where(u => (u.Email ?? string.Empty).Contains(trimmedEmail));
            }

            if (!string.IsNullOrWhiteSpace(abandonedFilter))
            {
                if (abandonedFilter == "yes")
                {
                    query = query.Where(ul => ul.Abandoned);
                }
                else if (abandonedFilter == "no")
                {
                    query = query.Where(ul => !ul.Abandoned);
                }
            }

            if (startDate.HasValue)
            {
                DateTime localStart = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Local);
                DateTime startUtc = localStart.ToUniversalTime();
                query = query.Where(u => u.LogInTime >= startUtc);
            }

            if (endDate.HasValue)
            {
                DateTime localEndExclusive = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Local);
                DateTime endUtcExclusive = localEndExclusive.ToUniversalTime();
                query = query.Where(u => u.LogInTime < endUtcExclusive);
            }

            switch (sortOrder)
            {
                case "email_desc":
                    query = query.OrderByDescending(u => u.Email)
                        .ThenByDescending(u => u.LogInTime)
                        .ThenByDescending(u => u.LogId);
                    break;
                case "email_asc":
                    query = query.OrderBy(u => u.Email)
                        .ThenBy(u => u.LogInTime)
                        .ThenBy(u => u.LogId);
                    break;
                case "login_desc":
                    query = query.OrderByDescending(u => u.LogInTime)
                        .ThenByDescending(u => u.Email)
                        .ThenByDescending(u => u.LogId);
                    break;
                case "login_asc":
                    query = query.OrderBy(u => u.LogInTime)
                        .ThenBy(u => u.Email)
                        .ThenBy(u => u.LogId);
                    break;
                default:
                    query = query.OrderByDescending(u => u.LogInTime)
                        .ThenByDescending(u => u.Email)
                        .ThenByDescending(u => u.LogId);
                    break;
            }

            return await PaginatedList<UserLogVM>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <summary>
        /// Returns the most recent active session log for the specified user, if one exists.
        /// </summary>
        /// <param name="userId">The identity user identifier.</param>
        /// <returns>The active user log, or <c>null</c> if none exists.</returns>
        public async Task<UserLog?> GetActiveLogAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await _context.UserLogs
                           .AsNoTracking()
                           .OrderByDescending(ul => ul.LogInTime)
                           .ThenByDescending(ul => ul.LogId)
                           .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LogOutTime == null);
        }

        /// <summary>
        /// Starts a new session log for the specified user.
        /// </summary>
        /// <param name="userId">The identity user identifier.</param>
        /// <param name="userEmailSnapshot">The email snapshot to preserve for historical display.</param>
        /// <returns><c>true</c> if the log was created successfully; otherwise <c>false</c>.</returns>
        public async Task<bool> StartLogAsync(string userId, string? userEmailSnapshot)
        {
            UserLog userLog = new UserLog
            {
                UserId = userId,
                UserEmailSnapshot = userEmailSnapshot,
                LogInTime = DateTime.UtcNow,
                LogOutTime = null,
                Abandoned = false
            };
            try
            {
                _context.UserLogs.Add(userLog);
                await _context.SaveChangesAsync();
                _logger.LogInformation("UserLog added successfully for userId={UserId}", userId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding UserLog for userId={UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding UserLog for userId={UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Ends an active session log by setting the logout time and abandoned status.
        /// </summary>
        /// <param name="logId">The log identifier.</param>
        /// <returns><c>true</c> if the log was closed successfully; otherwise <c>false</c>.</returns>
        public async Task<bool> EndLogAsync(int logId)
        {
            UserLog? userLog = await _context.UserLogs.FirstOrDefaultAsync(ul => ul.LogId == logId);

            if (userLog == null)
            {
                _logger.LogWarning("No UserLog found for id {LogId}", logId);
                return false;
            }

            if (userLog.LogOutTime.HasValue)
            {
                _logger.LogInformation("UserLog {LogId} already closed at {LogOutTime}", logId, userLog.LogOutTime.Value);
                return false;
            }

            userLog.LogOutTime = DateTime.UtcNow;
            userLog.Abandoned = false;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("UserLog {LogId} closed successfully for userId={UserId}.", logId, userLog.UserId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while closing UserLog {LogId}", logId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while closing UserLog {LogId}", logId);
                return false;
            }
        }

        /// <summary>
        /// Closes any open session logs for the specified user as abandoned sessions.
        /// </summary>
        /// <param name="userId">The identity user identifier.</param>
        /// <returns><c>true</c> if one or more dangling logs were closed; otherwise <c>false</c>.</returns>
        public async Task<bool> CloseDanglingLogsIfAnyAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var danglingLogs = await _context.UserLogs
                                   .Where(ul => (ul.UserId == userId) && (ul.LogOutTime == null))
                                   .OrderByDescending(ul => ul.LogInTime)
                                   .ThenByDescending(ul => ul.LogId)
                                   .ToListAsync();

            if (!danglingLogs.Any())
            {
                _logger.LogInformation("No dangling logs for userId={UserId}", userId);
                return false;
            }

            var closedAt = DateTime.UtcNow;

            foreach (var log in danglingLogs)
            {
                log.Abandoned = true;
                log.LogOutTime = closedAt;
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Closed {Count} dangling log(s) for userId={UserId}", danglingLogs.Count, userId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while closing dangling logs for userId={UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while closing dangling logs for userId={UserId}", userId);
                return false;
            }
        }

    }
}
