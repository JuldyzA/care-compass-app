using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
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
        /// Returns all user logs as view models, sorted by most recent first.
        /// Uses AsNoTracking() because this is read-only for display.
        /// </summary>
        public IEnumerable<UserLogVM> GetAll()
        {
            return _context.UserLogs
                           .AsNoTracking()
                           .OrderByDescending(ul => ul.LogInTime)
                           .ThenByDescending(ul => ul.LogId)
                           .Select(ul => new UserLogVM
                           {
                               Email = (ul.User != null) ? (ul.User.Email ?? ul.User.UserName ?? "(no email)") : "(user missing)",
                               LogInTime = ul.LogInTime,
                               LogOutTime = ul.LogOutTime,
                               Abandoned = ul.Abandoned
                           })
                           .ToList();
        }

        /// <summary>
        /// Returns the most recent "active" (not yet logged out) session for a user, if any.
        /// Active means LogOutTime is null.
        /// </summary>
        public UserLog? GetActiveLog(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return _context.UserLogs
                           .OrderByDescending(ul => ul.LogInTime)
                           .ThenByDescending(ul => ul.LogId)
                           .FirstOrDefault(ul => ul.UserId == userId && ul.LogOutTime == null);
        }

        /// <summary>
        /// Starts a new session log row for the user with LogInTime = UtcNow.
        /// </summary>
        public UserLog StartLog(string userId)
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
                _context.SaveChanges();
                _logger.LogInformation($"UserLog added successfully for userId={userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to add UserLog for userId={userId}: {ex.Message}");
            }
            return userLog;
        }

        /// <summary>
        /// Ends a session log by setting LogOutTime = UtcNow and Abandoned = false.
        /// </summary>
        public bool EndLog(int logId)
        {
            UserLog? userLog = _context.UserLogs.FirstOrDefault(ul => ul.LogId == logId);

            if (userLog == null)
            {
                _logger.LogWarning($"No UserLog found for id {logId}");
                return false;
            }

            if (userLog.LogOutTime.HasValue)
            {
                _logger.LogInformation($"UserLog {logId} already closed at {userLog.LogOutTime.Value:u}");
                return false;
            }

            userLog.LogOutTime = DateTime.UtcNow;
            userLog.Abandoned = false;

            try
            {
                _context.SaveChanges();
                _logger.LogInformation($"UserLog {logId} closed successfully for userId={userLog.UserId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error closing UserLog {logId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Abandoned-session policy helper:
        /// If there is any "dangling" (open) session for this user (LogOutTime == null),
        /// close the most recent one as abandoned by setting:
        /// - Abandoned = true
        /// - LogOutTime = UtcNow
        /// </summary>
        public bool CloseDanglingIfAny(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var dangling = _context.UserLogs
                                   .Where(ul => (ul.UserId == userId) && (ul.LogOutTime == null))
                                   .OrderByDescending(ul => ul.LogInTime)
                                   .ThenByDescending(ul => ul.LogId)
                                   .FirstOrDefault();

            if (dangling == null)
            {
                _logger.LogWarning($"No dangling logs for userId={userId}");
                return false;
            }

            dangling.Abandoned = true;
            dangling.LogOutTime = DateTime.UtcNow;

            try
            {
                _context.SaveChanges();
                _logger.LogInformation($"Closed dangling log {dangling.LogId} for userId={userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error closing dangling logs for userId={userId}: {ex.Message}");
                return false;
            }
        }

    }
}
