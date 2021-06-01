using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MinecraftClone.World_CS.Utility.Threading
{
    public class ThreadPoolClass
    {

        // An array of threads
        PooledThreadClass[] _pooledThreadClasses;

        /// <summary>
        /// Number of threads currently in use
        /// </summary>
        public byte Threads;

        /// <summary>
        /// Sets the threadpool up to be used
        /// </summary>
        /// <param name="threads"> Threads to use in threadpool leave at 0 if unsure </param>
        public void InitializePool(byte threads = 0, byte subtractVal = 3)
        {
            if (threads == 0)
            {
                Threads = (byte) (Environment.ProcessorCount - subtractVal);
            }
            else
            {
                Threads = threads;
            }
            SetMaxThreadCount(Threads);
        }
        /// <summary>
        /// Sets the max thread count after intialization
        /// </summary>
        /// <param name="threadCount">Threads to allocate</param>
        public void SetMaxThreadCount(byte threadCount)
        {
            _pooledThreadClasses = new PooledThreadClass[threadCount];
            for (int i = 0; i < Threads; i++)
            {
                _pooledThreadClasses[i] = new PooledThreadClass();
            }
        }

        /// <summary>
        /// Add request to task Queue, that way it can be run (with delegate to run after!).
        /// </summary>
        /// <param name="method"> Method you want to call, format in lambda AKA like: () => {method in question} </param>
        /// <param name="callback">What to call after the call has been completed</param>
        /// <returns>Returns A task that will contain the value returned, a task can also in theory be re-added in the future</returns>
        public ThreadTaskRequest AddRequest(Func<object> method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            ThreadTaskRequest taskClass = new ThreadTaskRequest(method);
            PooledThreadClass lowestTaskThread = null;
            int lowestTaskNumber = int.MaxValue;
                    
            foreach (PooledThreadClass threadClass in _pooledThreadClasses)
            {
                if (lowestTaskNumber > threadClass.TasksAssigned.Count)
                {
                    lowestTaskNumber = threadClass.TasksAssigned.Count;
                    lowestTaskThread = threadClass; 
                }
            }


            if (lowestTaskThread?.TaskAccessLock != null)
            {
                lock (lowestTaskThread?.TaskAccessLock)
                {
                    lowestTaskThread?.TasksAssigned.Add(taskClass);
                    if (lowestTaskThread.BIsIdle)
                    {
                        lock (lowestTaskThread.ThreadLocker)
                        {
                            Monitor.Pulse(lowestTaskThread.ThreadLocker);
                        }
                    }
                }   
            }
            return taskClass;
        }

        /// <summary>
        /// Starts the threadpool, the name scheme is set up like this because the threadpool acts like a fire of sorts, you stoke it with fuel (Tasks) then you let it burn until all the "fuel" is gone, then it stops, will make an autoignite feature later
        /// </summary>
        public void IgniteThreadPool()
        {
            foreach (PooledThreadClass thread in _pooledThreadClasses)
            {
                thread.PrepareThread();
                
                lock (thread.ThreadLocker)
                {
                    Monitor.Pulse(thread.ThreadLocker);
                }
            }

        }

        /// <summary>
        /// Currently Non-functional 
        /// </summary>
        public void ShutDownHandler()
        {
            foreach (PooledThreadClass pooledThreadClass in _pooledThreadClasses)
            {
                pooledThreadClass.DestroyThread();
                lock (pooledThreadClass.ThreadLocker)
                {
                    Monitor.Pulse(pooledThreadClass.ThreadLocker);
                }
            }
        }

        /// <summary>
        /// Unsure of this works, this might crash the program, that being said is supposed to signal true if the threadpool is completely idle
        /// </summary>
        /// <returns> true if the threadpool is idle, false if it is not</returns>
        [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
        public bool AllThreadsIdle()
        {
            foreach (PooledThreadClass thread in _pooledThreadClasses)
            {
                if (thread.BIsIdle == false && thread.TasksAssigned.Count != 0 && !thread.PendingShutdown)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
