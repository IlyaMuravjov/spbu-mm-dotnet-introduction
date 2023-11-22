namespace MyExecutor
{
    internal interface IMyRunnableTaskExecutor : IMyExecutor
    {
        void EnqueueRunnableTask(IMyRunnableTask task);
    }
}
