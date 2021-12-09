using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace QnABot.Repository
{
    public interface IGenericRepository<T> where T : class
    {
        T GetById(long id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> expression);
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
