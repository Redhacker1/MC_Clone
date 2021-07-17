using System;

namespace MinecraftClone.Utility.Threading
{
    public class ThreadTaskRequest
    {

        /// <summary>
        /// Method the thread is supposed to call when created
        /// </summary>
        public Func<object> Method { get; }

        public object Result;

        public bool BHasRun = false;
        


        public ThreadTaskRequest(Func<object> Method)
        {
            this.Method = Method;
        }
    }
}
