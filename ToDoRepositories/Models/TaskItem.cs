using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ToDoRepositories.Enum;
using System.Text.Json.Serialization;

namespace ToDoRepositories.Models
{
    [PrimaryKey("Id")]
    public class TaskItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PriorityLevel Priority { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Enum.TaskStatus Status { get; set; }
    }
}
