// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PPMTool.Data
{
    /// <summary>
    /// A task queue with a semaphore to allow only one task at a time to be run
    /// </summary>
    public class TaskQueue
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<Func<Task>> _tasks;

        public TaskQueue()
        {
            // Allow only one task at a time
            _semaphore = new SemaphoreSlim(1, 1);
            _tasks = new ConcurrentQueue<Func<Task>>();
        }

        public async Task Enqueue(Func<Task> taskGenerator)
        {
            // Queue the task generation function
            _tasks.Enqueue(taskGenerator);
            await ProcessQueue();
        }

        private async Task ProcessQueue()
        {
            // Wait on the semaphore
            await _semaphore.WaitAsync();

            try
            {
                // Try run the task
                while (_tasks.TryDequeue(out var taskGenerator))
                {
                    Debug.WriteLine("** Running task from the TaskQueue...");
                    await taskGenerator();
                }
            }
            finally
            {
                // Release the semaphore
                _semaphore.Release();
            }
        }
    }
}
