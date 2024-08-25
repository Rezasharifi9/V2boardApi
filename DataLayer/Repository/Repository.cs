using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DataLayer.Repository
{
    public class Repository<T> : IRepository<T>, IDisposable where T : class
    {
        private Entities db;
        public DbSet<T> table = null;

        public Repository(Entities _db)
        {
            db = _db;
            table = db.Set<T>();
        }

        public Repository()
        {
            db = new Entities();
            table = db.Set<T>();
        }

        public List<T> Where(Expression<Func<T, bool>> predicate)
        {
            return table.Where(predicate).ToList();
        }

        public List<T> GetAll()
        {
            return table.ToList();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await table.ToListAsync().ConfigureAwait(false);
        }

        public T GetById(object id)
        {
            return table.Find(id);
        }

        public void Insert(T obj)
        {
            table.Add(obj);
        }

        public void Update(T obj)
        {
            table.Attach(obj);
            db.Entry(obj).State = EntityState.Modified;
        }

        public bool Save()
        {
            return Convert.ToBoolean(db.SaveChanges());
        }

        public async Task<int> SaveChangesAsync()
        {
            return await db.SaveChangesAsync().ConfigureAwait(false);
        }

        public void Delete(int id)
        {
            T existing = table.Find(id);
            table.Remove(existing);
        }
        public Task DeleteRangeAsync(List<T> list)
        {
            table.RemoveRange(list);
            return Task.CompletedTask;
        }


        public void Delete(T obj)
        {
            table.Remove(obj);
        }

        public async Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            return await table.Where(predicate).ToListAsync().ConfigureAwait(false);
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await table.Where(predicate).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}
