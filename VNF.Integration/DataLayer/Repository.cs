using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace VNF.Integration.DataLayer
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        public VNFContext db = new VNFContext();


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

        public virtual void Save()
        {
            db.SaveChanges();
        }

        public virtual void Update(TEntity entidade)
        {
            db.Entry(entidade).State = EntityState.Modified;
        }

        public virtual void Delete(int id)
        {
            TEntity e = db.Set<TEntity>().Find(id);
            if (e != null)
            {
                db.Set<TEntity>().Remove(e);
            }
        }
    }
}