using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoRepositories.Enum;

namespace ToDoRepositories.DTO.Request
{
    public class TaskItemRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public PriorityLevel Priority { get; set; } = PriorityLevel.Low;
        public Enum.TaskStatus Status { get; set; } = Enum.TaskStatus.NotStarted;
    }
}
