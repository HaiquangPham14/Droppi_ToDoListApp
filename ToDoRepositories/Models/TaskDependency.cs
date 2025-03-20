using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoRepositories.Enum;
using Microsoft.EntityFrameworkCore;

namespace ToDoRepositories.Models
{
    [PrimaryKey("Id")]
    public class TaskDependency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int TaskId { get; set; }
        [ForeignKey(nameof(TaskId))]
        public virtual TaskItem Task { get; set; } = null!;
        public int DependentTaskId { get; set; }

        [ForeignKey(nameof(DependentTaskId))]
        public virtual TaskItem DependentTask { get; set; } = null!;
    }
}
