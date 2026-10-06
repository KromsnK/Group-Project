using Microsoft.EntityFrameworkCore;
using Store.Data.Context;
using Store.Data.Entities;
using System.Linq.Expressions;

namespace Store.Data.Repositories
{
    public class GenericRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext context;
        protected readonly DbSet<T> dbSet;

        public GenericRepository(AppDbContext context)
        {
            this.context = context;
            this.dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await this.dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await this.dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await this.dbSet.Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await this.dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            this.dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            this.dbSet.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await this.context.SaveChangesAsync();
        }
    }
}
