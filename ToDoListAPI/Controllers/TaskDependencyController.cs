using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ToDoRepositories.DTO.Request;
using ToDoRepositories.DTO.Response;
using ToDoRepositories.Interface;
using ToDoRepositories.Models;

namespace ToDoListAPI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TaskDependenciesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private const int CACHE_EXPIRATION_MINUTES = 15;
        private const int DefaultPageSize = 20;

        public TaskDependenciesController(IUnitOfWork unitOfWork, IDistributedCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        // GET: api/v1/TaskDependencies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDependencyResponse>>> GetTaskDependencies(int pageIndex = 1, int pageSize = DefaultPageSize)
        {
            int totalTaskDependencies = _unitOfWork.TaskDependencyRepository.Get().Count();
            int totalPages = (int)Math.Ceiling((double)totalTaskDependencies / DefaultPageSize);

            for (int currentPage = 1; currentPage <= totalPages; currentPage++)
            {
                string cacheKey = $"TaskDependencies_Page{currentPage}_Size{DefaultPageSize}";
                string cachedData = await _cache.GetStringAsync(cacheKey);

                if (string.IsNullOrEmpty(cachedData))
                {
                    int skip = (currentPage - 1) * pageSize;
                    int take = pageSize;
                    var dependencies = _unitOfWork.TaskDependencyRepository.Get()
                        .OrderByDescending(taskdependency => taskdependency.Id)
                        .Skip(skip)
                        .Take(take)
                        .Select(dep => new TaskDependencyResponse
                        {
                            Id = dep.Id,
                            TaskId = dep.TaskId,
                            DependentTaskId = dep.DependentTaskId,
                        }).ToList();
                    foreach (var d in dependencies)
                    {
                        string cacheKey2 = $"TaskDependency_{d.Id}";
                        await _cache.SetStringAsync(cacheKey2, JsonConvert.SerializeObject(d), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                        });
                    }
                    var serializedTaskDependencies = JsonConvert.SerializeObject(dependencies);
                    await _cache.SetStringAsync(cacheKey, serializedTaskDependencies, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                    });
                    }
            }
            string requestCacheKey = $"TaskDependencies_Page{pageIndex}_Size{DefaultPageSize}";
            string requestedCacheData = await _cache.GetStringAsync(requestCacheKey);
            if (!string.IsNullOrEmpty(requestedCacheData))
            {
                var cachedTaskDependencies = JsonConvert.DeserializeObject<List<TaskDependencyResponse>>(requestedCacheData);
                return Ok(cachedTaskDependencies);
            }
            return Ok(new List<TaskDependencyResponse>());
        }

        // GET: api/v1/TaskDependencies/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDependencyResponse>> GetTaskDependency(int id)
        {
            string cacheKey = $"TaskDependency_{id}";
            string cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedTaskDependency = JsonConvert.DeserializeObject<TaskDependencyResponse>(cachedData);
                return Ok(cachedTaskDependency);
            }
            var dependency = _unitOfWork.TaskDependencyRepository.GetByID(id);

            if (dependency == null)
                return NotFound($"Task Dependency with ID {id} not found.");

            var response = new TaskDependencyResponse
            {
                Id = dependency.Id,
                TaskId = dependency.TaskId,
                DependentTaskId = dependency.DependentTaskId,
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });
            return Ok(response);
        }

        // POST: api/v1/TaskDependencies
        [HttpPost]
        public async Task<ActionResult<TaskDependencyResponse>> CreateTaskDependency(TaskDependencyRequest request)
        {
            if (IsDuplicateDependency(request.TaskId, request.DependentTaskId))
            {
                return BadRequest("This dependency already exists!!!");
            }
            if (IsCircularDependency(request.TaskId, request.DependentTaskId))
            {
                return BadRequest("Cannot create a circular dependency!!!");
            }

            var taskDependency = new TaskDependency
            {
                TaskId = request.TaskId,
                DependentTaskId = request.DependentTaskId
            };

            _unitOfWork.TaskDependencyRepository.Insert(taskDependency);
            _unitOfWork.Save();

            var response = new TaskDependencyResponse
            {
                Id = taskDependency.Id,
                TaskId = taskDependency.TaskId,
                DependentTaskId = taskDependency.DependentTaskId
            };

            string cacheKey = $"TaskDependency_{response.Id}";
            string cachedData = await _cache.GetStringAsync(cacheKey);
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });
            //update cache 

            string cacheKey2 = $"TaskDependencies_Page1_Size{DefaultPageSize}";
            string cachedData2 = await _cache.GetStringAsync(cacheKey2);
            List<TaskDependencyResponse> cachedTaskDependencies = string.IsNullOrEmpty(cachedData2)
                ? new List<TaskDependencyResponse>()
                : JsonConvert.DeserializeObject<List<TaskDependencyResponse>>(cachedData2);

            cachedTaskDependencies.Insert(0, response);
            if (cachedTaskDependencies.Count > DefaultPageSize)
            {
                var overflowItems = cachedTaskDependencies.GetRange(DefaultPageSize, cachedTaskDependencies.Count - DefaultPageSize);
                cachedTaskDependencies = cachedTaskDependencies.Take(DefaultPageSize).ToList();
                await DistributeOverflowToNextCacheAsync(1, overflowItems);
            }

            await _cache.SetStringAsync(cacheKey2, JsonConvert.SerializeObject(cachedTaskDependencies),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                });

            return CreatedAtAction(nameof(GetTaskDependency), new { id = taskDependency.Id }, response);
        }
        //
        // DistributeOverflow next Cache
        //
        private async Task DistributeOverflowToNextCacheAsync(int pageIndex, List<TaskDependencyResponse> overflowItems)
        {
            string nextCacheKey = $"TaskDependencies_Page{pageIndex + 1}_Size{DefaultPageSize}";
            string nextCachedData = await _cache.GetStringAsync(nextCacheKey);

            List<TaskDependencyResponse> nextCachedTaskDependencies = string.IsNullOrEmpty(nextCachedData)
                ? new List<TaskDependencyResponse>()
                : JsonConvert.DeserializeObject<List<TaskDependencyResponse>>(nextCachedData);

            nextCachedTaskDependencies.InsertRange(0, overflowItems);

            if (nextCachedTaskDependencies.Count > DefaultPageSize)
            {
                var overflowToNext = nextCachedTaskDependencies.GetRange(DefaultPageSize, nextCachedTaskDependencies.Count - DefaultPageSize);
                nextCachedTaskDependencies = nextCachedTaskDependencies.Take(DefaultPageSize).ToList();
                await DistributeOverflowToNextCacheAsync(pageIndex + 1, overflowToNext);
            }

            await _cache.SetStringAsync(nextCacheKey, JsonConvert.SerializeObject(nextCachedTaskDependencies),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                });
        }
        // PUT: api/v1/TaskDependencies/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTaskDependency(int id, TaskDependencyRequest request)
        {
            var existingDependency = _unitOfWork.TaskDependencyRepository.GetByID(id);

            if (existingDependency == null)
                return NotFound($"Task Dependency with ID {id} not found!");
            if (IsDuplicateDependency(request.TaskId, request.DependentTaskId, id))
            {
                return BadRequest("This dependency already exists!!!");
            }
            if (IsCircularDependency(request.TaskId, request.DependentTaskId))
            {
                return BadRequest("Cannot create a circular dependency!!!");
            }

            existingDependency.TaskId = request.TaskId;
            existingDependency.DependentTaskId = request.DependentTaskId;

            _unitOfWork.TaskDependencyRepository.Update(existingDependency);
            _unitOfWork.Save();

            string cacheKey = $"TaskDependency_{id}";
            string cachedData = await _cache.GetStringAsync(cacheKey);
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(existingDependency), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
            });

            int pageIndex = 1;
            while (true)
            {
                string cacheKey2 = $"TaskDependencies_Page{pageIndex}_Size{DefaultPageSize}";
                string cachedData2 = await _cache.GetStringAsync(cacheKey2);

                if (string.IsNullOrEmpty(cachedData2)) break;

                var cachedTaskDependencies = JsonConvert.DeserializeObject<List<TaskDependencyResponse>>(cachedData2);
                var itemToUpdate = cachedTaskDependencies.FirstOrDefault(dep => dep.Id == id);

                if (itemToUpdate != null)
                {
                    itemToUpdate.TaskId = request.TaskId;
                    itemToUpdate.DependentTaskId = request.DependentTaskId;

                    await _cache.SetStringAsync(cacheKey2, JsonConvert.SerializeObject(cachedTaskDependencies), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                    });
                }

                pageIndex++;
            }
            return NoContent();
        }
        private bool IsDuplicateDependency(int taskId, int dependentTaskId, int? ignoreId = null)
        {
            var existingDependency = _unitOfWork.TaskDependencyRepository.Get()
                .FirstOrDefault(d => d.TaskId == taskId && d.DependentTaskId == dependentTaskId);
            if (existingDependency != null && (!ignoreId.HasValue || existingDependency.Id != ignoreId.Value))
            {
                return true;
            }

            return false;
        }
        private bool IsCircularDependency(int taskId, int dependentTaskId)
        {
            if (taskId == dependentTaskId)
                return true;

            Queue<int> queue = new Queue<int>();
            queue.Enqueue(dependentTaskId);

            while (queue.Count > 0)
            {
                int currentTaskId = queue.Dequeue();

                var dependentTasks = _unitOfWork.TaskDependencyRepository.Get()
                    .Where(d => d.TaskId == currentTaskId)
                    .Select(d => d.DependentTaskId)
                    .ToList();
                if (dependentTasks.Contains(taskId))
                {
                    return true;
                }
                foreach (var task in dependentTasks)
                {
                    queue.Enqueue(task);
                }
            }

            return false;
        }
        // DELETE: api/v1/TaskDependencies/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskDependency(int id)
        {
            var dependency = _unitOfWork.TaskDependencyRepository.GetByID(id);

            if (dependency == null)
                return NotFound($"Task Dependency with ID {id} not found.");

            _unitOfWork.TaskDependencyRepository.Delete(dependency);
            _unitOfWork.Save();

            await _cache.RemoveAsync($"TaskDependency_{id}");

            int pageIndex = 1;
            while (true)
            {
                string cacheKey = $"TaskDependencies_Page{pageIndex}_Size{DefaultPageSize}";
                string cachedData = await _cache.GetStringAsync(cacheKey);

                if (string.IsNullOrEmpty(cachedData)) break;

                var cachedTaskDependencies = JsonConvert.DeserializeObject<List<TaskDependencyResponse>>(cachedData);

                var itemToRemove = cachedTaskDependencies.FirstOrDefault(dep => dep.Id == id);
                if (itemToRemove != null)
                    cachedTaskDependencies.Remove(itemToRemove);

                int currentCount = cachedTaskDependencies.Count;
                int expectedCount = DefaultPageSize;

                int nextPageIndex = pageIndex + 1;

                while (currentCount < expectedCount)
                {
                    string nextCacheKey = $"TaskDependencies_Page{nextPageIndex}_Size{DefaultPageSize}";
                    string nextCachedData = await _cache.GetStringAsync(nextCacheKey);

                    if (string.IsNullOrEmpty(nextCachedData))
                        break;

                    var nextPageItems = JsonConvert.DeserializeObject<List<TaskDependencyResponse>>(nextCachedData);

                    if (nextPageItems.Count > 0)
                    {
                        cachedTaskDependencies.Add(nextPageItems.First());
                        nextPageItems.RemoveAt(0);

                        await _cache.SetStringAsync(nextCacheKey, JsonConvert.SerializeObject(nextPageItems),
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                            });

                        currentCount++;
                    }

                    nextPageIndex++;
                }

                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(cachedTaskDependencies),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                    });

                pageIndex++;
            }

            return NoContent();
        }
    }
}
