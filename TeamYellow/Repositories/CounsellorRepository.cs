using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;


namespace TeamYellow.Repositories
{
    public class CounsellorRepository
    {
        private readonly ApplicationDbContext _context;

        public CounsellorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all counsellors including their associated user profiles.
        /// </summary>
        public async Task<IEnumerable<Counsellor>> GetAllAsync()
        {
            return await _context.Counsellors
                .Include(c => c.User)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all counsellors with their user profiles, subscriptions, and related payment transactions.
        /// </summary>
        public async Task<IEnumerable<Counsellor>> GetCounsellorsWithPaymentsAsync()
        {
            return await _context.Counsellors
                .Include(c => c.User)
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.PaymentTransaction)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a counsellor by ID, including the associated user profile.
        /// </summary>
        //public Counsellor? GetById(int id)
        //{
        //    return Context.Counsellors.Include(c => c.User).FirstOrDefault(c => c.CounsellorId == id);
        //}
        public async Task<Counsellor?> GetByUserIdAsync(string userId)
        {
            return await _context.Counsellors
            .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        /// <summary>
        /// Adds a new counsellor to the database.
        /// </summary>
        /// <param name="entity">The counsellor entity to add.</param>
        /// <returns>The ID of the added counsellor as a string.</returns>
        public string Add(Counsellor entity)
        {
            try
            {
                _context.Counsellors.Add(entity);
                _context.SaveChanges();
                return entity.CounsellorId.ToString();
            }
            catch (DbUpdateException ex)
            {
                // Log exception or handle as needed
                throw new ApplicationException("An error occurred while adding the counsellor to the database.", ex);
            }
            catch (Exception ex)
            {
                // Log exception or handle as needed
                throw new ApplicationException("An unexpected error occurred while adding the counsellor.", ex);
            }
        }

        /// <summary>
        /// Updates an existing counsellor in the database.
        /// </summary>
        /// <param name="entity">The counsellor entity to update.</param>
        /// <returns>The ID of the updated counsellor as a string.</returns>
        public async Task<string> UpdateAsync(Counsellor entity)
        {
            try
            {
                _context.Counsellors.Update(entity);
                await _context.SaveChangesAsync();
                return entity.CounsellorId.ToString();
            }
            catch (DbUpdateException ex)
            {
                // Log exception or handle as needed
                throw new ApplicationException("An error occurred while updating the counsellor in the database.", ex);
            }
            catch (Exception ex)
            {
                // Log exception or handle as needed
                throw new ApplicationException("An unexpected error occurred while updating the counsellor.", ex);
            }
        }


        /// <summary>
        /// Checks if a counsellor exists by ID.
        /// </summary>
        /// <param name="id">The ID of the counsellor.</param>
        /// <returns>True if the counsellor exists, otherwise false.</returns>
        public async Task<bool> AnyAsync(int id)
        {
            return await _context.Counsellors
                .AnyAsync(c => c.CounsellorId == id);
        }
    }
}
