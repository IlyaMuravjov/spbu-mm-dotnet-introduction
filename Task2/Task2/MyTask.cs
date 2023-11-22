using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace MyExecutor
{
    internal class MyTask<TResult> : IMyTask<TResult>, IMyRunnableTask
    {
        private interface IMyTaskState;

        private record CompletedTaskState(
            Result<TResult> Result
        ) : IMyTaskState;

        private record UncompletedTaskState(
            Action<Result<TResult>> Callback,
            UncompletedTaskState? Next = null
        ) : IMyTaskState;

        private readonly IMyRunnableTaskExecutor _executor;
        private Func<TResult>? _func;
        private readonly EventWaitHandle _completionWaitHandle;
        private volatile IMyTaskState _state;

        public MyTask(IMyRunnableTaskExecutor executor, Func<TResult> func)
        {
            _executor = executor;
            _func = func;
            _completionWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            _state = new UncompletedTaskState(Callback: (_) => _completionWaitHandle.Set());
        }

        public bool IsCompleted => _state is CompletedTaskState;

        public TResult Result
        {
            get
            {
                _completionWaitHandle.WaitOne();
                return ((CompletedTaskState)_state).Result.Match(
                    Succ: val => val,
                    Fail: ex => throw ex
                );
            }
        }

        public void Run() => Complete(Try(_func).Match(
            Succ: val => new Result<TResult>(val),
            Fail: ex => new Result<TResult>(new AggregateException(ex))
        ));

        public void Cancel() => Complete(new Result<TResult>(new OperationCanceledException()));

        private void Complete(Result<TResult> result)
        {
            // let GC collect _func
            _func = null;
            while (true)
            {
                IMyTaskState state = _state;
                switch (state)
                {
                    case UncompletedTaskState uncompletedState:
                        {
                            if (Interlocked.CompareExchange(ref _state, new CompletedTaskState(result), state) == state)
                            {
                                List<Action<Result<TResult>>> callbacks = [];
                                for (var curNode = uncompletedState; curNode != null; curNode = curNode.Next)
                                {
                                    callbacks.Add(curNode.Callback);
                                }
                                callbacks.Reverse();
                                callbacks.ForEach(callback => callback(result));
                                return;
                            }

                            break;
                        }

                    case CompletedTaskState:
                        throw new InvalidOperationException($"Complete has already been called, state: '{state}', result: '{result}'");
                    default:
                        throw new InvalidOperationException($"Unknown task state: '{state}'");
                }
            } 
        }

        public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuation)
        {
            while (true)
            {
                IMyTaskState state = _state;
                switch (state)
                {
                    case UncompletedTaskState curUncompletedState:
                        {
                            MyTask<TNewResult> newTask = new(_executor, () => continuation(Result));
                            UncompletedTaskState newState = new(
                                Callback: (result) => _executor.EnqueueRunnableTask(newTask),
                                Next: curUncompletedState
                            );
                            if (Interlocked.CompareExchange(ref _state, newState, state) == state)
                                return newTask;
                            break;
                        }
                    case CompletedTaskState curCompletedState:
                        return _executor.Enqueue(() => continuation(Result));
                    default:
                        throw new InvalidOperationException($"Unknown task state: '{state}'");
                }
            }
        }
    }
}
