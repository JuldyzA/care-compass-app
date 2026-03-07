using TeamYellow.Data;

namespace TeamYellow.Repositories
{
    public class DiscountRepository
    {
        private readonly ApplicationDbContext Context;

        public DiscountRepository(ApplicationDbContext context)
        {
            Context = context;
        }
    }
}
