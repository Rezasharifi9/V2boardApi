using DataLayer.DomainModel;
using DataLayer.Models.DashboardModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Repository
{
    public class ViewRepository<T> : IRepository<T>, IDisposable where T : class
    {
        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public T GetById(object id)
        {
            throw new NotImplementedException();
        }

        public void Insert(T obj)
        {
            throw new NotImplementedException();
        }

        public bool Save()
        {
            throw new NotImplementedException();
        }

        public List<T> ToList()
        {
            using (var context = new Entities())
            {
                var results = context.Set<T>().ToList();

                return results;
            }
        }

        public void Update(T obj)
        {
            throw new NotImplementedException();
        }

        public List<T> Where(Expression<Func<T, bool>> predicate)
        {
            using (var context = new Entities())
            {
                var results = context.Set<T>().Where(predicate).ToList();

                return results;
            }
        }

        public Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            throw new NotImplementedException();
        }
    }
}
