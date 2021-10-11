using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VNF.Integration.DataLayer
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IEnumerable<TEntity> Get();
        TEntity GetByID(int id);
        void Insert(TEntity entidade);
        void Delete(int id);
        void Update(TEntity entidade);
        void Save();
    }
}
