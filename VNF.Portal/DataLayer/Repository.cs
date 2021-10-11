using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;
using System.Data;

namespace VNF.Portal.DataLayer
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        public DB db = new DB();

        public virtual IEnumerable<TEntity> Get()
        {
            return db.Set<TEntity>().ToList();
        }

        public virtual TEntity GetByID(int id)
        {
            return db.Set<TEntity>().Find(id);
        }
        
        public virtual void Insert(TEntity entidade)
        {
            db.Set<TEntity>().Add(entidade);
        }

        public virtual void Delete(int id)
        {
            TEntity e = db.Set<TEntity>().Find(id);
            if (e != null)
            {
                db.Set<TEntity>().Remove(e);
            }
        }

        public virtual void Update(TEntity entidade)
        {
            db.Entry(entidade).State = EntityState.Modified;
        }

        public virtual void Save()
        {
            db.SaveChanges();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    db.Dispose();
                }
            }
            this.disposed = true;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}