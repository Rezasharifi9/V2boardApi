using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.DomainModel;

namespace DataLayer.Repository
{
    public class Repository<T> : IRepository<T> where T : class
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

        public void Delete(int id)
        {
            T existing = table.Find(id);
            table.Remove(existing);
        }

        public void Delete(T obj)
        {
            table.Remove(obj);
        }
    }
}
