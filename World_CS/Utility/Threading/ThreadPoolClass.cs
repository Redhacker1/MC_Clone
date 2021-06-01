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
        /// <param name="DesiredThreads"> Threads to use in threadpool leave at 0 if unsure </param>
        public void InitializePool(byte DesiredThreads = 0, byte SubtractVal = 0)
        {
            if (DesiredThreads == 0)
            {
                Threads = (byte) (Environment.ProcessorCount - SubtractVal);
            }
            else
            {
                Threads = DesiredThreads;
            }
            SetMaxThreadCount(Threads);
        }
        /// <summary>
        /// Sets the max thread count after intialization
        /// </summary>
        /// <param name="ThreadCount">Threads to allocate</param>
        public void SetMaxThreadCount(byte ThreadCount)
        {
            _pooledThreadClasses = new PooledThreadClass[ThreadCount];
            for (int I = 0; I < Threads; I++)
            {
                _pooledThreadClasses[I] = new PooledThreadClass();
            }
        }

        /// <summary>
        /// Add request to task Queue, that way it can be run (with delegate to run after!).
        /// </summary>
        /// <param name="Method"> Method you want to call, format in lambda AKA like: () => {method in question} </param>
        /// <param name="callback">What to call after the call has been completed</param>
        /// <returns>Returns A task that will contain the value returned, a task can also in theory be re-added in the future</returns>
        public ThreadTaskRequest AddRequest(Func<object> Method)
        {
            if (Method == null) throw new ArgumentNullException(nameof(Method));

            ThreadTaskRequest TaskClass = new ThreadTaskRequest(Method);
            PooledThreadClass LowestTaskThread = null;
            int LowestTaskNumber = int.MaxValue;
                    
            foreach (PooledThreadClass ThreadClass in _pooledThreadClasses)
            {
                if (LowestTaskNumber > ThreadClass.TasksAssigned.Count)
                {
                    LowestTaskNumber = ThreadClass.TasksAssigned.Count;
                    LowestTaskThread = ThreadClass; 
                }
            }


            if (LowestTaskThread?.TaskAccessLock != null)
            {
                lock (LowestTaskThread?.TaskAccessLock)
                {
                    LowestTaskThread?.TasksAssigned.Add(TaskClass);
                    if (LowestTaskThread.BIsIdle)
                    {
                        lock (LowestTaskThread.ThreadLocker)
                        {
                            Monitor.Pulse(LowestTaskThread.ThreadLocker);
                        }
                    }
                }   
            }
            return TaskClass;
        }

        /// <summary>
        /// Starts the threadpool, the name scheme is set up like this because the threadpool acts like a fire of sorts, you stoke it with fuel (Tasks) then you let it burn until all the "fuel" is gone, then it stops, will make an autoignite feature later
        /// </summary>
        public void IgniteThreadPool()
        {
            foreach (PooledThreadClass Thread in _pooledThreadClasses)
            {
                Thread.PrepareThread();
                
                lock (Thread.ThreadLocker)
                {
                    Monitor.Pulse(Thread.ThreadLocker);
                }
            }

        }

        /// <summary>
        /// Currently Non-functional 
        /// </summary>
        public void ShutDownHandler()
        {
            foreach (PooledThreadClass PooledThreadClass in _pooledThreadClasses)
            {
                PooledThreadClass.DestroyThread();
                lock (PooledThreadClass.ThreadLocker)
                {
                    Monitor.Pulse(PooledThreadClass.ThreadLocker);
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
            foreach (PooledThreadClass Thread in _pooledThreadClasses)
            {
                if (Thread.BIsIdle == false && Thread.TasksAssigned.Count != 0 && !Thread.PendingShutdown)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
