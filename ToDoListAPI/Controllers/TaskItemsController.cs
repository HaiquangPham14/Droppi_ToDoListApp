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
        private const int CACHE_EXPIRATION_MINUTES = 15;
        private const int DefaultPageSize = 20;

        public TaskItemsController(IUnitOfWork unitOfWork, IDistributedCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        // GET: api/v1/TaskItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItemResponse>>> GetTaskItems(int pageIndex = 1, int pageSize = DefaultPageSize)
        {
            int totalTasks = _unitOfWork.TaskItemRepository.Get().Count();
            int totalPages = (int)Math.Ceiling((double)totalTasks / pageSize);

            for (int currentPage = 1; currentPage <= totalPages; currentPage++)
            {
                string cacheKey = $"TaskItems_Page{currentPage}_Size{DefaultPageSize}";
                string cachedData = await _cache.GetStringAsync(cacheKey);

                if (string.IsNullOrEmpty(cachedData))
                {
                    int skip = (currentPage - 1) * pageSize;
                    int take = pageSize;

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
                }
            }

            string requestCacheKey = $"TaskItems_Page{pageIndex}_Size{DefaultPageSize}";
            string requestedCacheData = await _cache.GetStringAsync(requestCacheKey);

            if (!string.IsNullOrEmpty(requestedCacheData))
            {
                var cachedTaskItems = JsonConvert.DeserializeObject<List<TaskItemResponse>>(requestedCacheData);
                return Ok(cachedTaskItems);
            }

            return Ok(new List<TaskItemResponse>());
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

            //update cache 
            string cacheKey2 = $"TaskItems_Page1_Size{DefaultPageSize}";
            string cachedData2 = await _cache.GetStringAsync(cacheKey2);
            List<TaskItemResponse> cachedTaskItems = string.IsNullOrEmpty(cachedData2)
                ? new List<TaskItemResponse>()
                : JsonConvert.DeserializeObject<List<TaskItemResponse>>(cachedData2);

            cachedTaskItems.Insert(0, response);
            if (cachedTaskItems.Count > DefaultPageSize)
            {
                var overflowItems = cachedTaskItems.GetRange(DefaultPageSize, cachedTaskItems.Count - DefaultPageSize);
                cachedTaskItems = cachedTaskItems.Take(DefaultPageSize).ToList();
                await DistributeOverflowToNextCacheAsync(1, overflowItems);
            }
            await _cache.SetStringAsync(cacheKey2, JsonConvert.SerializeObject(cachedTaskItems),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                });
            return CreatedAtAction(nameof(GetTaskItem), new { id = task.Id }, response);
        }
        private async Task DistributeOverflowToNextCacheAsync(int pageIndex, List<TaskItemResponse> overflowItems)
        {
            string nextCacheKey = $"TaskItems_Page{pageIndex + 1}_Size{DefaultPageSize}";
            string nextCachedData = await _cache.GetStringAsync(nextCacheKey);

            List<TaskItemResponse> nextCachedTaskItems = string.IsNullOrEmpty(nextCachedData)
                ? new List<TaskItemResponse>()
                : JsonConvert.DeserializeObject<List<TaskItemResponse>>(nextCachedData);

            nextCachedTaskItems.InsertRange(0, overflowItems);

            if (nextCachedTaskItems.Count > DefaultPageSize)
            {
                var overflowToNext = nextCachedTaskItems.GetRange(DefaultPageSize, nextCachedTaskItems.Count - DefaultPageSize);
                nextCachedTaskItems = nextCachedTaskItems.Take(DefaultPageSize).ToList();
                await DistributeOverflowToNextCacheAsync(pageIndex + 1, overflowToNext);
            }

            await _cache.SetStringAsync(nextCacheKey, JsonConvert.SerializeObject(nextCachedTaskItems),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                });
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

            string individualCacheKey = $"TaskItem_{id}";
            await _cache.RemoveAsync(individualCacheKey);

            await _cache.SetStringAsync(individualCacheKey, JsonConvert.SerializeObject(existingTask), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });

            int pageIndex = 1;
            while (true)
            {
                string cacheKey = $"TaskItems_Page{pageIndex}_Size{DefaultPageSize}";
                string cachedData = await _cache.GetStringAsync(cacheKey);

                if (string.IsNullOrEmpty(cachedData)) break;

                var cachedTaskItems = JsonConvert.DeserializeObject<List<TaskItemResponse>>(cachedData);

                var itemToUpdate = cachedTaskItems.FirstOrDefault(task => task.Id == id);
                if (itemToUpdate != null)
                {
                    itemToUpdate.Title = taskRequest.Title;
                    itemToUpdate.Description = taskRequest.Description;
                    itemToUpdate.Priority = taskRequest.Priority;
                    itemToUpdate.Status = taskRequest.Status;
                    itemToUpdate.DueDate = taskRequest.DueDate;

                    await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(cachedTaskItems), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                    });
                }

                pageIndex++;
            }

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

            int pageIndex = 1;
            while (true)
            {
                string cacheKey = $"TaskItems_Page{pageIndex}_Size{DefaultPageSize}";
                string cachedData = await _cache.GetStringAsync(cacheKey);

                if (string.IsNullOrEmpty(cachedData)) break;

                var cachedTaskItems = JsonConvert.DeserializeObject<List<TaskItemResponse>>(cachedData);

                var itemToRemove = cachedTaskItems.FirstOrDefault(task => task.Id == id);
                if (itemToRemove != null)
                    cachedTaskItems.Remove(itemToRemove);

                int currentCount = cachedTaskItems.Count;
                int expectedCount = DefaultPageSize;

                int nextPageIndex = pageIndex + 1;
                while (currentCount < expectedCount)
                {
                    string nextCacheKey = $"TaskItems_Page{nextPageIndex}_Size{DefaultPageSize}";
                    string nextCachedData = await _cache.GetStringAsync(nextCacheKey);

                    if (string.IsNullOrEmpty(nextCachedData))
                        break;

                    var nextPageItems = JsonConvert.DeserializeObject<List<TaskItemResponse>>(nextCachedData);

                    if (nextPageItems.Count > 0)
                    {
                        cachedTaskItems.Add(nextPageItems.First());
                        nextPageItems.RemoveAt(0);

                        await _cache.SetStringAsync(nextCacheKey, JsonConvert.SerializeObject(nextPageItems), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                        });

                        currentCount++;
                    }

                    nextPageIndex++;
                }

                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(cachedTaskItems), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                });

                pageIndex++;
            }

            return NoContent();
        }
    }
}
