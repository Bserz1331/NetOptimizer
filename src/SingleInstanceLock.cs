using System;
using System.Threading;

namespace NetOptimizerV2
{
    internal sealed class SingleInstanceLock : IDisposable
    {
        private const string MutexName = "Local\\SpaceCat.NetOptimizer";
        private sealed class Acquisition
        {
            public readonly string Name;
            public readonly TimeSpan WaitTimeout;
            public readonly ManualResetEventSlim Ready = new ManualResetEventSlim(false);
            public readonly ManualResetEventSlim Release = new ManualResetEventSlim(false);
            public Mutex Mutex;
            public Thread OwnerThread;
            public bool Acquired;
            public Exception Error;

            public Acquisition(string name, TimeSpan waitTimeout)
            {
                Name = name;
                WaitTimeout = waitTimeout;
            }
        }

        private readonly Acquisition acquisition;
        private int disposed;

        private SingleInstanceLock(Acquisition acquisition)
        {
            this.acquisition = acquisition;
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
            ValidateWaitTimeout(waitTimeout);
            Acquisition acquisition = new Acquisition(name, waitTimeout);
            Thread ownerThread = new Thread(AcquireAndHold);
            acquisition.OwnerThread = ownerThread;
            ownerThread.IsBackground = true;
            try
            {
                ownerThread.Start(acquisition);
                acquisition.Ready.Wait();
            }
            catch
            {
                try { acquisition.Release.Set(); } catch { }
                JoinOwner(acquisition);
                DisposeSignals(acquisition);
                throw;
            }

            if (acquisition.Error != null)
            {
                Exception error = acquisition.Error;
                JoinOwner(acquisition);
                DisposeSignals(acquisition);
                throw error;
            }
            if (!acquisition.Acquired)
            {
                JoinOwner(acquisition);
                DisposeSignals(acquisition);
                return null;
            }

            acquired = true;
            return new SingleInstanceLock(acquisition);
        }

        private static void ValidateWaitTimeout(TimeSpan waitTimeout)
        {
            if (waitTimeout < TimeSpan.Zero && waitTimeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException("waitTimeout");
            }
            if (waitTimeout > TimeSpan.FromMilliseconds(Int32.MaxValue))
            {
                throw new ArgumentOutOfRangeException("waitTimeout");
            }
        }

        private static void AcquireAndHold(object value)
        {
            Acquisition acquisition = (Acquisition)value;
            try
            {
                acquisition.Mutex = new Mutex(false, acquisition.Name);
                try
                {
                    acquisition.Acquired = acquisition.Mutex.WaitOne(acquisition.WaitTimeout);
                }
                catch (AbandonedMutexException)
                {
                    // WaitOne transfers ownership to this process after an abandoned mutex.
                    acquisition.Acquired = true;
                }

                acquisition.Ready.Set();
                if (acquisition.Acquired)
                {
                    acquisition.Release.Wait();
                }
            }
            catch (Exception ex)
            {
                acquisition.Error = ex;
                try { acquisition.Ready.Set(); } catch { }
            }
            finally
            {
                if (acquisition.Mutex != null)
                {
                    if (acquisition.Acquired)
                    {
                        try { acquisition.Mutex.ReleaseMutex(); } catch { }
                    }
                    try { acquisition.Mutex.Dispose(); } catch { }
                    acquisition.Mutex = null;
                }
            }
        }

        private static void JoinOwner(Acquisition acquisition)
        {
            if (acquisition == null || acquisition.OwnerThread == null ||
                Thread.CurrentThread == acquisition.OwnerThread)
            {
                return;
            }
            try { acquisition.OwnerThread.Join(); } catch { }
        }

        private static void DisposeSignals(Acquisition acquisition)
        {
            if (acquisition == null) { return; }
            try { acquisition.Ready.Dispose(); } catch { }
            try { acquisition.Release.Dispose(); } catch { }
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

                    // Release the first owner from another thread so the waiter exercises
                    // both the real handoff and cross-thread disposal.
                    Exception releaseError = null;
                    Thread releaser = new Thread(delegate()
                    {
                        try { first.Dispose(); }
                        catch (Exception ex) { releaseError = ex; }
                    });
                    releaser.IsBackground = true;
                    releaser.Start();
                    if (!releaser.Join(3000))
                    {
                        throw new InvalidOperationException("單一實例鎖跨執行緒釋放逾時。");
                    }
                    if (releaseError != null)
                    {
                        throw new InvalidOperationException(
                            "單一實例鎖跨執行緒釋放發生例外：" + releaseError.Message,
                            releaseError);
                    }
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
            if (Interlocked.Exchange(ref disposed, 1) != 0) { return; }
            try { acquisition.Release.Set(); } catch { }
            JoinOwner(acquisition);
            DisposeSignals(acquisition);
        }
    }
}
