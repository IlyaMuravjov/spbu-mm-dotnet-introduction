using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace MyExecutor
{
    public sealed class MyThreadPool : IMyRunnableTaskExecutor
    {

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Queue<IMyRunnableTask> _tasks = new();
        private readonly EventWaitHandle _taskWaitHandle = new(false, EventResetMode.AutoReset);
        private readonly List<Thread> _threads = [];
        private bool _canceled = false;

        public MyThreadPool(int threadCount)
        {
            if (threadCount <= 0)
            {
                throw new ArgumentOutOfRangeException($"Non-positive thread count: {threadCount}");
            }
            for (int i = 0; i < threadCount; i++)
            {
                var cancellationToken = _cancellationTokenSource.Token;
                Thread thread = new(() => { RunWorker(cancellationToken); });
                thread.Start();
                _threads.Add(thread);
            }
        }

        private void RunWorker(CancellationToken cancellationToken)
        {
            WaitHandle[] waitHandles = [_taskWaitHandle, cancellationToken.WaitHandle];
            while (!cancellationToken.IsCancellationRequested)
            {
                WaitHandle.WaitAny(waitHandles);
                IMyRunnableTask? task;
                lock (_tasks)
                {
                    _tasks.TryDequeue(out task);
                    if (_tasks.Count > 0)
                    {
                        _taskWaitHandle.Set();
                    }
                }
                task?.Run();
            }
        }

        public IMyTask<TResult> Enqueue<TResult>(Func<TResult> func)
        {
            MyTask<TResult> task = new(this, func);
            ((IMyRunnableTaskExecutor) this).EnqueueRunnableTask(task);
            return task;
        }

        void IMyRunnableTaskExecutor.EnqueueRunnableTask(IMyRunnableTask task)
        {
            lock (_tasks)
            {
                if (_canceled)
                {
                    task.Cancel();
                }
                else
                {
                    _tasks.Enqueue(task);
                    _taskWaitHandle.Set();
                }
            }
            
        }

        public void Dispose()
        {
            lock(_tasks)
            {
                _canceled = true;
                while (_tasks.Count > 0)
                {
                    _tasks.Dequeue().Cancel();
                }
            }
            _cancellationTokenSource.Cancel();
            foreach (var thread in _threads)
            {
                thread.Join();
            }
        }
    }
}
