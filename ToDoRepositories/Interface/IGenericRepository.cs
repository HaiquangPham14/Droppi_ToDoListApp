using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ToDoRepositories.Interface
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        IEnumerable<TEntity> Get(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "",
            int? pageIndex = null,
            int? pageSize = null);
        TEntity GetByID(object id);

        void Insert(TEntity entity);

        void InsertList(List<TEntity> entities);

        void Delete(object id);

        void Delete(TEntity entityToDelete);

        void DeleteList(IEnumerable<TEntity> entitiesToDelete);

        void Update(TEntity entityToUpdate);

        void UpdateList(IEnumerable<TEntity> entitiesToUpdate);

        TEntity Get(Expression<Func<TEntity, bool>> predicate);
    }
}
