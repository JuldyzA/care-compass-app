using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Helpers;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository for reading and writing user login/logout audit logs (UserLogs table).
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
        /// Returns a paginated, sortable, and filterable list of user logs, sorted by most recent first.
        /// Used for the admin user logs screen.
        /// Uses AsNoTracking() because this is read-only for display.
        /// </summary>
        public async Task<IEnumerable<UserLogVM>> GetAllAsync(string? emailFilter = null, string? abandonedFilter = null, DateTime? startDate = null, DateTime? endDate = null, string? sortOrder = null, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<UserLogVM> query = _context.UserLogs
                                       .AsNoTracking()
                                       .Select(ul => new UserLogVM
                                       {
                                           Email = (ul.User != null) ? (ul.User.Email ?? ul.User.UserName ?? "(no email)") : "(user missing)",
                                           LogInTime = ul.LogInTime,
                                           LogOutTime = ul.LogOutTime,
                                           Abandoned = ul.Abandoned
                                       });

            if (!string.IsNullOrWhiteSpace(emailFilter))
            {
                string trimmedEmail = emailFilter.Trim();
                query = query.Where(u => u.Email.Contains(trimmedEmail));
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
                DateTime start = startDate.Value.Date;
                query = query.Where(u => u.LogInTime >= start);
            }

            if (endDate.HasValue)
            {
                DateTime endExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(u => u.LogInTime < endExclusive);
            }

            switch (sortOrder)
            {
                case "email_desc":
                    query = query.OrderByDescending(u => u.Email)
                        .ThenByDescending(u => u.LogInTime);
                    break;
                case "email_asc":
                    query = query.OrderBy(u => u.Email)
                        .ThenBy(u => u.LogInTime);
                    break;
                case "login_desc":
                    query = query.OrderByDescending(u => u.LogInTime)
                        .ThenByDescending(u => u.Email);
                    break;
                case "login_asc":
                    query = query.OrderBy(u => u.LogInTime)
                        .ThenBy(u => u.Email);
                    break;
                default:
                    query = query.OrderByDescending(u => u.LogInTime)
                        .ThenByDescending(u => u.Email);
                    break;
            }

            return await PaginatedList<UserLogVM>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <summary>
        /// Returns the most recent "active" (not yet logged out) session for a user, if any.
        /// Active means LogOutTime is null.
        /// </summary>
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
        /// Starts a new session log row for the user with LogInTime = UtcNow.
        /// </summary>
        public async Task<bool> StartLogAsync(string userId)
        {
            UserLog userLog = new UserLog
            {
                UserId = userId,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to add UserLog for userId={UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Ends a session log by setting LogOutTime = UtcNow and Abandoned = false.
        /// </summary>
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing UserLog {LogId}", logId);
                return false;
            }
        }

        /// <summary>
        /// Abandoned-session policy helper:
        /// If there are any open sessions for this user (LogOutTime == null),
        /// close all of them as abandoned by setting:
        /// - Abandoned = true
        /// - LogOutTime = closedAt (DateTime.UtcNow)
        /// </summary>
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing dangling logs for userId={UserId}", userId);
                return false;
            }
        }

    }
}
