using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Drawing.Printing;
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
        private readonly IDistributedCache _cache;
        private const int CACHE_EXPIRATION_MINUTES = 5;
        public TaskItemsController(IUnitOfWork unitOfWork, IDistributedCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        // GET: api/v1/TaskItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItemResponse>>> GetTaskItems(int pageIndex = 1, int pageSize = 20)
        {
            string cacheKey = $"TaskItems_Page{pageIndex}_Size{pageSize}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedTaskItems = JsonConvert.DeserializeObject<List<TaskItemResponse>>(cachedData);
                return Ok(cachedTaskItems);
            }
            int totalTasks = _unitOfWork.TaskItemRepository.Get().Count();
            int skip = (pageIndex - 1) * pageSize;
            int take = pageSize;
            if (skip >= totalTasks)
            {
                return Ok(new List<TaskItemResponse>());
            }
            var taskItems = _unitOfWork.TaskItemRepository.Get()
                .OrderByDescending(task => task.Id)
                .Skip(skip)
                .Take(take)
                .Select(task => new TaskItemResponse
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    Status = task.Status,
                    DueDate = task.DueDate
                }).ToList();

            foreach (var t in taskItems)
            {
                string cacheKey2 = $"TaskItem_{t.Id}";
                await _cache.SetStringAsync(cacheKey2, JsonConvert.SerializeObject(t), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                });
            }
            var serializedTaskItems = JsonConvert.SerializeObject(taskItems);
            await _cache.SetStringAsync(cacheKey, serializedTaskItems, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });
            return Ok(taskItems);
        }

        // GET: api/v1/TaskItems/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItemResponse>> GetTaskItem(int id)
        {
            string cacheKey = $"TaskItem_{id}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedTaskItem = JsonConvert.DeserializeObject<TaskItemResponse>(cachedData);
                return Ok(cachedTaskItem);
            }

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

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });

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

            string cacheKey = $"TaskItem_{response.Id}";
            string cachedData = await _cache.GetStringAsync(cacheKey);
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });
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

            await _cache.RemoveAsync($"TaskItem_{id}");

            string cacheKey = $"TaskItem_{id}";
            string cachedData = await _cache.GetStringAsync(cacheKey);
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(existingTask), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });

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

            await _cache.RemoveAsync($"TaskItem_{id}");

            return NoContent();
        }
    }
}
