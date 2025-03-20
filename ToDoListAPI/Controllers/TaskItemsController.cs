using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ToDoRepositories.DTO;
using ToDoRepositories.DTO.Request;
using ToDoRepositories.DTO.Response;
using ToDoRepositories.Interface;
using ToDoRepositories.Models;

namespace ToDoListAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TaskItemsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public TaskItemsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/v1/TaskItems
        [HttpGet]
        public ActionResult<IEnumerable<TaskItemResponse>> GetTaskItems(int pageIndex = 1, int pageSize = 20)
        {
            var taskItems = _unitOfWork.TaskItemRepository.Get(
                pageIndex: pageIndex,
                pageSize: pageSize)
                .Select(task => new TaskItemResponse
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    Status = task.Status,
                    DueDate = task.DueDate
                }).ToList();

            return Ok(taskItems);
        }

        // GET: api/v1/TaskItems/{id}
        [HttpGet("{id}")]
        public ActionResult<TaskItemResponse> GetTaskItem(int id)
        {
            var task = _unitOfWork.TaskItemRepository.GetByID(id);

            if (task == null)
                return NotFound($"Task with ID {id} not found.");

            var response = new TaskItemResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                DueDate = task.DueDate
            };

            return Ok(response);
        }

        // POST: api/v1/TaskItems
        [HttpPost]
        public async Task<ActionResult<TaskItemResponse>> CreateTaskItem(TaskItemRequest taskRequest)
        {
            var task = new TaskItem
            {
                Title = taskRequest.Title,
                Description = taskRequest.Description,
                Status = taskRequest.Status,
                Priority = taskRequest.Priority,
                DueDate = taskRequest.DueDate
            };

            _unitOfWork.TaskItemRepository.Insert(task);
            _unitOfWork.Save();

            var response = new TaskItemResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                DueDate = task.DueDate
            };

            return CreatedAtAction(nameof(GetTaskItem), new { id = task.Id }, response);
        }

        // PUT: api/v1/TaskItems/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTaskItem(int id, TaskItemRequest taskRequest)
        {
            var existingTask = _unitOfWork.TaskItemRepository.GetByID(id);

            if (existingTask == null)
                return NotFound($"Task with ID {id} not found.");

            existingTask.Title = taskRequest.Title;
            existingTask.Description = taskRequest.Description;
            existingTask.Priority = taskRequest.Priority;
            existingTask.Status = taskRequest.Status;
            existingTask.DueDate = taskRequest.DueDate;

            _unitOfWork.TaskItemRepository.Update(existingTask);
            _unitOfWork.Save();

            return NoContent();
        }

        // DELETE: api/v1/TaskItems/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskItem(int id)
        {
            var task = _unitOfWork.TaskItemRepository.GetByID(id);

            if (task == null)
                return NotFound($"Task with ID {id} not found.");

            _unitOfWork.TaskItemRepository.Delete(task);
            _unitOfWork.Save();

            return NoContent();
        }
    }
}
