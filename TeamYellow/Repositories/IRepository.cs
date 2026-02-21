namespace TeamYellow.Repositories
{
    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();

        T? GetById(int id);

        string Add(T entity);

        string Update(T entity);

        string Delete(int id);

        bool Any(int id);
    }
}
