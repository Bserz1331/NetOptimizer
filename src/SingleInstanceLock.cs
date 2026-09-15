using System;
using System.Threading;

namespace NetOptimizerV2
{
    internal sealed class SingleInstanceLock : IDisposable
    {
        private const string MutexName = "Local\\SpaceCat.NetOptimizer";
        private readonly Mutex mutex;
        private bool owns;

        private SingleInstanceLock(Mutex mutex)
        {
            this.mutex = mutex;
            owns = true;
        }

        public static SingleInstanceLock TryAcquire(out bool acquired)
        {
            return TryAcquire(MutexName, TimeSpan.Zero, out acquired);
        }

        public static SingleInstanceLock TryAcquire(TimeSpan waitTimeout, out bool acquired)
        {
            return TryAcquire(MutexName, waitTimeout, out acquired);
        }

        private static SingleInstanceLock TryAcquire(
            string name,
            TimeSpan waitTimeout,
            out bool acquired)
        {
            acquired = false;
            Mutex mutex = new Mutex(false, name);
            try
            {
                bool ownsMutex;
                try
                {
                    ownsMutex = mutex.WaitOne(waitTimeout);
                }
                catch (AbandonedMutexException)
                {
                    // WaitOne transfers ownership to this process after an abandoned mutex.
                    ownsMutex = true;
                }

                if (!ownsMutex)
                {
                    mutex.Dispose();
                    return null;
                }

                acquired = true;
                return new SingleInstanceLock(mutex);
            }
            catch
            {
                mutex.Dispose();
                throw;
            }
        }

        internal static void RunSelfTest()
        {
            string testMutexName = "Local\\SpaceCat.NetOptimizer.SelfTest." +
                                   Guid.NewGuid().ToString("N");
            bool acquired;
            using (SingleInstanceLock first = TryAcquire(testMutexName, TimeSpan.Zero, out acquired))
            {
                if (!acquired || first == null)
                {
                    throw new InvalidOperationException("單一實例鎖首次取得驗證失敗。");
                }

                bool duplicateAcquired = false;
                Exception duplicateError = null;
                using (ManualResetEventSlim duplicateStarted = new ManualResetEventSlim(false))
                {
                    Thread duplicateThread = new Thread(delegate()
                    {
                        try
                        {
                            duplicateStarted.Set();
                            bool candidateAcquired;
                            using (SingleInstanceLock duplicate = TryAcquire(
                                       testMutexName,
                                       TimeSpan.FromMilliseconds(50),
                                       out candidateAcquired))
                            {
                                duplicateAcquired = candidateAcquired;
                            }
                        }
                        catch (Exception ex)
                        {
                            duplicateError = ex;
                        }
                    });
                    duplicateThread.IsBackground = true;
                    duplicateThread.Start();
                    if (!duplicateStarted.Wait(1000) || !duplicateThread.Join(2000))
                    {
                        throw new InvalidOperationException("單一實例鎖重複取得等待逾時。");
                    }
                }

                if (duplicateError != null)
                {
                    throw new InvalidOperationException(
                        "單一實例鎖重複取得等待發生例外：" + duplicateError.Message,
                        duplicateError);
                }
                if (duplicateAcquired)
                {
                    throw new InvalidOperationException("單一實例鎖重複取得驗證失敗。");
                }

                bool handoffAcquired = false;
                Exception handoffError = null;
                using (ManualResetEventSlim waiterStarted = new ManualResetEventSlim(false))
                {
                    Thread waiter = new Thread(delegate()
                    {
                        try
                        {
                            waiterStarted.Set();
                            bool candidateAcquired;
                            using (SingleInstanceLock candidate = TryAcquire(
                                       testMutexName,
                                       TimeSpan.FromSeconds(2),
                                       out candidateAcquired))
                            {
                                handoffAcquired = candidateAcquired;
                            }
                        }
                        catch (Exception ex)
                        {
                            handoffError = ex;
                        }
                    });
                    waiter.IsBackground = true;
                    waiter.Start();
                    if (!waiterStarted.Wait(1000))
                    {
                        throw new InvalidOperationException("單一實例鎖交接等待未啟動。");
                    }

                    // Release the first owner before joining so the waiter exercises the real handoff.
                    first.Dispose();
                    if (!waiter.Join(3000))
                    {
                        throw new InvalidOperationException("單一實例鎖交接等待逾時。");
                    }
                }

                if (handoffError != null)
                {
                    throw new InvalidOperationException(
                        "單一實例鎖交接等待發生例外：" + handoffError.Message,
                        handoffError);
                }
                if (!handoffAcquired)
                {
                    throw new InvalidOperationException("單一實例鎖交接取得驗證失敗。");
                }
            }
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
