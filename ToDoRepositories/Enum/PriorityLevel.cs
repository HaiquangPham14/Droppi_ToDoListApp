using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ToDoRepositories.Enum
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PriorityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
}
