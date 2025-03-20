using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoRepositories.Models;

namespace ToDoRepositories.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<TaskItem> TaskItemRepository { get; }
        IGenericRepository<TaskDependency> TaskDependencyRepository { get; }

        void Save();
    }
}
