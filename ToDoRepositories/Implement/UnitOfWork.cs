using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoRepositories.Interface;
using ToDoRepositories.Models;

namespace ToDoRepositories.Implement
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ToDoListDBContext _context;
        private IGenericRepository<TaskItem> taskItemRepository;
        private IGenericRepository<TaskDependency> taskDependencyRepository;

        public UnitOfWork(ToDoListDBContext context)
        {
            _context = context;
        }
        public IGenericRepository<TaskItem> TaskItemRepository
        {
            get
            {
                return taskItemRepository ??= new GenericRepository<TaskItem>(_context);
            }
        }

        public IGenericRepository<TaskDependency> TaskDependencyRepository
        {
            get
            {
                return taskDependencyRepository ??= new GenericRepository<TaskDependency>(_context);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
