using System;
using System.Threading;

namespace NetOptimizerV2
{
    internal sealed class SingleInstanceLock : IDisposable
    {
        private readonly Mutex mutex;
        private bool owns;

        private SingleInstanceLock(Mutex mutex)
        {
            this.mutex = mutex;
            owns = true;
        }

        public static SingleInstanceLock TryAcquire(out bool acquired)
        {
            Mutex mutex = new Mutex(true, "Local\\SpaceCat.NetOptimizer", out acquired);
            if (!acquired)
            {
                mutex.Dispose();
                return null;
            }
            return new SingleInstanceLock(mutex);
        }

        public void Dispose()
        {
            if (!owns) { return; }
            owns = false;
            try { mutex.ReleaseMutex(); } catch { }
            mutex.Dispose();
        }
    }
}
