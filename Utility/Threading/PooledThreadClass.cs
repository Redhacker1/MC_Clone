using System.Collections.Generic;
using System.Threading;

namespace MinecraftClone.Utility.Threading
{
    public class PooledThreadClass
    {

        public object ThreadLocker = new object();
        public readonly List<ThreadTaskRequest> TasksAssigned = new List<ThreadTaskRequest>();

        /// <summary>
        /// Used to pause the thread
        /// </summary>
        public bool BIsPaused = false;

        public bool PendingShutdown;

        public object TaskAccessLock = new object();

        /// <summary>
        /// Is true if the thread has no work to do and the manual reset event is triggered.
        /// </summary>
        public bool BIsIdle;

        /// <summary>
        /// Underlying thread class that runs, really should private as there is no way to ensure that the thread is not running/finished therefore it is unsafe to manipulate outside of the thread class on the GrabEvent and/or constructor. You should expose the isAlive etc through this 
        /// </summary>
        Thread _internalThread;

        /// <summary>
        /// Keeps track of the order this thread was "spawned in"
        /// </summary>
        public int ThreadNumber;

        //Constructor seems fine no complaints

        public void DestroyThread()
        {
            PendingShutdown = true;
        }

        public void PrepareThread()
        {
            _internalThread = new Thread(ThreadLoop);
            _internalThread.Start();
        }

        //Finds a task and uses it unless there are no more tasks then just returns
        /// <summary>
        /// Loop the thread takes
        /// </summary>
        /// 
        void ThreadLoop()
        {
            while(!PendingShutdown)
            {
                ThreadTaskRequest CurrentTask = null;

                if (TasksAssigned.Count > 0 && !BIsPaused)
                {
                    if (BIsIdle)
                    {
                        BIsIdle = false;
                    }
                    lock (TaskAccessLock)
                    {
                        CurrentTask = TasksAssigned[0];
                        TasksAssigned.RemoveAt(0);
                    }
                }
                else
                {
                    BIsIdle = true;
                    lock (ThreadLocker)
                    {
                        Monitor.Wait(ThreadLocker);   
                    }
                }

                if (CurrentTask == null) continue;
                CurrentTask.Result = CurrentTask.Method();
                CurrentTask.BHasRun = true;
            }
        }
    }
}
