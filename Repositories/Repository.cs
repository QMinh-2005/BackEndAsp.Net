using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;

namespace MyOwnLearning.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly WebBadmintonContext _context;
        protected readonly DbSet<T> _dbset;

        public Repository(WebBadmintonContext context)
        {
            _context = context;
            _dbset = context.Set<T>();
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return (await _dbset.ToListAsync());
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbset.FindAsync(id);
        }
        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbset.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        public virtual async Task<T> UpdateAsync(T entity)
        {
            _dbset.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        public virtual async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbset.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbset.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }


    }
}
