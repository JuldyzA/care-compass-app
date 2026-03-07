using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;


namespace TeamYellow.Repositories
{
    public class CounsellorRepository
    {
        private readonly ApplicationDbContext Context;

        public CounsellorRepository(ApplicationDbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Retrieves all counsellors including their associated user profiles.
        /// </summary>
        public IEnumerable<Counsellor> GetAll()
        {
            return Context.Counsellors.Include(c => c.User).ToList();
        }

        /// <summary>
        /// Retrieves all counsellors with their user profiles, subscriptions, and related payment transactions.
        /// </summary>
        public IEnumerable<Counsellor> GetCounsellorsWithPayments()
        {
            return Context.Counsellors
                .Include(c => c.User)
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.PaymentTransaction)
                .ToList();
        }

        /// <summary>
        /// Retrieves a counsellor by ID, including the associated user profile.
        /// </summary>
        public Counsellor? GetById(int id)
        {
            return Context.Counsellors.Include(c => c.User).FirstOrDefault(c => c.CounsellorId == id);
        }

        /// <summary>
        /// Adds a new counsellor to the database.
        /// </summary>
        /// <param name="entity">The counsellor entity to add.</param>
        /// <returns>The ID of the added counsellor as a string.</returns>
        public string Add(Counsellor entity)
        {
            Context.Counsellors.Add(entity);
            Context.SaveChanges();
            return entity.CounsellorId.ToString();
        }

        /// <summary>
        /// Updates an existing counsellor in the database.
        /// </summary>
        /// <param name="entity">The counsellor entity to update.</param>
        /// <returns>The ID of the updated counsellor as a string.</returns>
        public string Update(Counsellor entity)
        {
            Context.Counsellors.Update(entity);
            Context.SaveChanges();
            return entity.CounsellorId.ToString();
        }

        /// <summary>
        /// Deletes a counsellor by ID from the database.
        /// </summary>
        /// <param name="id">The ID of the counsellor to delete.</param>
        /// <returns>The ID of the deleted counsellor as a string, or empty if not found.</returns>
        public string Delete(int id)
        {
            var entity = Context.Counsellors.Find(id);
            if (entity == null) return string.Empty;
            Context.Counsellors.Remove(entity);
            Context.SaveChanges();
            return id.ToString();
        }

        /// <summary>
        /// Checks if a counsellor exists by ID.
        /// </summary>
        /// <param name="id">The ID of the counsellor.</param>
        /// <returns>True if the counsellor exists, otherwise false.</returns>
        public bool Any(int id)
        {
            return Context.Counsellors.Any(c => c.CounsellorId == id);
        }
    }
}
