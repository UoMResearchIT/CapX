using System.Collections.Concurrent;
using System.Diagnostics;

namespace PPMTool.Data
{
    /// <summary>
    /// A task queue with a semaphore to allow only one task at a time to be run
    /// </summary>
    public class TaskQueue
    {
        private readonly SemaphoreSlim semaphore;
        private readonly ConcurrentQueue<Func<Task>> tasks;

        public TaskQueue()
        {
            // Allow only one task at a time
            semaphore = new SemaphoreSlim(1, 1);
            tasks = new ConcurrentQueue<Func<Task>>();
        }

        public async Task Enqueue(Func<Task> taskGenerator)
        {
            // Queue the task generation function
            tasks.Enqueue(taskGenerator);
            await ProcessQueue();
        }

        private async Task ProcessQueue()
        {
            // Wait on the semaphore
            await semaphore.WaitAsync();

            try
            {
                // Try run the task
                while (tasks.TryDequeue(out var taskGenerator))
                {
                    Debug.WriteLine("** Running task from the TaskQueue...");
                    await taskGenerator();
                }
            }
            finally
            {
                // Release the semaphore
                semaphore.Release();
            }
        }
    }
}
