namespace MyExecutor
{
    public interface IMyExecutor : IDisposable
    {
        IMyTask<TResult> Enqueue<TResult>(Func<TResult> func);
    }
}
