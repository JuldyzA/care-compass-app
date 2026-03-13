using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

/// <summary>
/// Repository providing data access operations for <see cref="Counsellor"/> entities.
/// </summary>
public class CounsellorRepository(ApplicationDbContext context) : ICounsellorRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <summary>
    /// Retrieves the counsellor profile associated with the specified ASP.NET Identity user ID.
    /// </summary>
    /// <param name="userId">The Identity user ID to search by.</param>
    /// <returns>
    /// The matching <see cref="Counsellor"/> if found; otherwise <c>null</c>.
    /// </returns>
    public async Task<Counsellor?> GetByUserIdAsync(string userId)
    {
        return await _context.Counsellors
        .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    /// <summary>
    /// Persists a new <see cref="Counsellor"/> record to the database.
    /// </summary>
    /// <param name="counsellor">The counsellor entity to add.</param>
    /// <returns>The newly created <see cref="Counsellor"/> with any database-generated values populated.</returns>
    public async Task<Counsellor> CreateAsync(Counsellor counsellor)
    {
        _context.Counsellors.Add(counsellor);
        await _context.SaveChangesAsync();
        return counsellor;
    }

    /// <summary>
    /// Checks whether a counsellor with the given practitioner licence ID already exists.
    /// Used to ensure uniqueness before assigning a new licence ID.
    /// </summary>
    /// <param name="licenceId">The practitioner licence ID to check.</param>
    /// <returns><c>true</c> if the licence ID is already in use; otherwise <c>false</c>.</returns>
    public async Task<bool> LicenceIdExistsAsync(string licenceId)
    {
        return await _context.Counsellors.AnyAsync(c => c.PractitionerLicenceId == licenceId);
    }
}
