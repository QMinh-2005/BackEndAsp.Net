namespace MyOwnLearning.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();

        //trả về T? để nhỡ chẳng may không tìm được thì trả về null
        Task<T?> GetByIdAsync(int id);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task AddRangeAsync(IEnumerable<T> entities);

    }
}
