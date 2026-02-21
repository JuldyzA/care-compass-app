using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;


namespace TeamYellow.Repositories
{
    public class CounsellorRepository : IRepository<Counsellor>
    {
        private readonly ApplicationDbContext Context;

        public CounsellorRepository(ApplicationDbContext context)
        {
            Context = context;
        }
        public IEnumerable<Counsellor> GetAll()
        {
            return Context.Counsellors.Include(c => c.User).ToList();
        }

        public IEnumerable<Counsellor> GetCounsellorsWithPayments()
        {
            return Context.Counsellors
                .Include(c => c.User)
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.PaymentTransaction)
                .ToList();
        }
        public Counsellor? GetById(int id)
        {
            return Context.Counsellors.Include(c => c.User).FirstOrDefault(c => c.CounsellorId == id);
        }

        public string Add(Counsellor entity)
        {
            Context.Counsellors.Add(entity);
            Context.SaveChanges();
            return entity.CounsellorId.ToString();
        }

        public string Update(Counsellor entity)
        {
            Context.Counsellors.Update(entity);
            Context.SaveChanges();
            return entity.CounsellorId.ToString();
        }

        public string Delete(int id)
        {
            var entity = Context.Counsellors.Find(id);
            if (entity == null) return string.Empty;
            Context.Counsellors.Remove(entity);
            Context.SaveChanges();
            return id.ToString();
        }

        public bool Any(int id)
        {
            return Context.Counsellors.Any(c => c.CounsellorId == id);
        }
    }
}
