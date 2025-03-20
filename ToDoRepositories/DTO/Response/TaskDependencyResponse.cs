using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoRepositories.DTO.Response
{
    public class TaskDependencyResponse
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int DependentTaskId { get; set; }
    }
}
