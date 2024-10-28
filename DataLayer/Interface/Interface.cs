using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public interface IRepository<T> where T : class
    {
        List<T> ToList();
        List<T> Where(Expression<Func<T, bool>> predicate);
        Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate);
        T GetById(object id);
        void Insert(T obj);
        void Update(T obj);
        void Delete(int id);
        bool Save();
    }
}
