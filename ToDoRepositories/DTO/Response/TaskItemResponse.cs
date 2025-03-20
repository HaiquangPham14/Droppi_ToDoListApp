using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoRepositories.Enum;

namespace ToDoRepositories.DTO.Response
{
    public class TaskItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public PriorityLevel Priority { get; set; }
        public Enum.TaskStatus Status { get; set; }
    }
}
