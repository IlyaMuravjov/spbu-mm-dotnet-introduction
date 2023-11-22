using LanguageExt;
using MyExecutor;

namespace MyExecutorTest
{
    [TestClass]
    public class MyThreadPoolTest
    {

        [TestMethod]
        public void WhenEnqueuedOneShortTaskMyThreadPoolShouldRunThatTask()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task = myThreadPool.Enqueue(() => 42);
            Assert.AreEqual(42, task.Result);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenTaskThrowsExceptionMyThreadPoolShouldWrapItInAggregateException()
        {
            MyThreadPool myThreadPool = new(10);
            Exception exception = new IndexOutOfRangeException();
            IMyTask<int> task = myThreadPool.Enqueue<int>(() => throw exception);
            AggregateException aggregateException = Assert.ThrowsException<AggregateException>(() => task.Result);
            Assert.AreSame(exception, aggregateException.InnerException);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenTaskIsAlreadyFinishedContinueWithShouldWork()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task1 = myThreadPool.Enqueue(() => 42);
            var _ = task1.Result;
            IMyTask<string> task2 = task1.ContinueWith(val => (val + 5).ToString());
            Assert.AreEqual("47", task2.Result);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenTaskIsStillRunningContinueWithShouldWork()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task1 = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });
            IMyTask<string> task2 = task1.ContinueWith(val => (val + 5).ToString());
            Assert.AreEqual("47", task2.Result);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenEnqueuedOne1000msTaskMyThreadPoolShouldTakeBetween750msAnd1250msToRunThatTask()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });
            Assert.IsFalse(task.IsCompleted);
            Thread.Sleep(750);
            Assert.IsFalse(task.IsCompleted);
            Thread.Sleep(1250);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(42, task.Result);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenEnqueuedTwoChained1000msTasksMyThreadPoolShouldTakeBetween1500msAnd2500msToRunThem()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task1 = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });
            IMyTask<int> task2 = task1.ContinueWith(val =>
            {
                Thread.Sleep(1000);
                return val + 10;
            });
            Assert.IsFalse(task1.IsCompleted);
            Assert.IsFalse(task2.IsCompleted);
            Thread.Sleep(500);
            Assert.IsFalse(task1.IsCompleted);
            Assert.IsFalse(task2.IsCompleted);
            Thread.Sleep(1000);
            Assert.IsTrue(task1.IsCompleted);
            Assert.IsFalse(task2.IsCompleted);
            Thread.Sleep(1000);
            Assert.IsTrue(task1.IsCompleted);
            Assert.IsTrue(task2.IsCompleted);
            Assert.AreEqual(42, task1.Result);
            Assert.AreEqual(52, task2.Result);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenThereAre5ThreadsInAPoolThen5TasksCanRunInParallel()
        {
            MyThreadPool myThreadPool = new(5);
            int counter = 0;
            for (int i = 0; i < 5; i++)
            {
                myThreadPool.Enqueue(() =>
                {
                    Interlocked.Increment(ref counter);
                    while (counter < 5) { }
                    return Unit.Default;
                });
            }
            Thread.Sleep(1000);
            Assert.AreEqual(counter, 5);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenMyThreadsPoolIsDisposedThenCurrentlyRunningTaskShouldStillComplete()
        {
            MyThreadPool myThreadPool = new(5);
            IMyTask<string> task = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return "Hello, world";
            });
            Thread.Sleep(500);
            myThreadPool.Dispose();
            Assert.AreEqual("Hello, world", task.Result);
        }

        [TestMethod]
        public void WhenMyThreadsPoolIsDisposedThenTaskInQueueShouldGetCancelled()
        {
            MyThreadPool myThreadPool = new(1);
            myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return "Hello, world";
            });
            IMyTask<int> task = myThreadPool.Enqueue(() => 42);
            Thread.Sleep(500);
            myThreadPool.Dispose();
            Assert.ThrowsException<OperationCanceledException>(() => task.Result);
        }

        [TestMethod]
        public void WhenMyThreadsPoolIsDisposedThenTaskContinuationShouldGetCancelled()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            }).ContinueWith(val => val + 1);
            Thread.Sleep(500);
            myThreadPool.Dispose();
            Assert.ThrowsException<OperationCanceledException>(() => task.Result);
        }

        [TestMethod]
        public void WhenMyThreadsPoolIsDisposedThenNewTasksShouldGetCancelled()
        {
            MyThreadPool myThreadPool = new(10);
            myThreadPool.Dispose();
            IMyTask<int> task = myThreadPool.Enqueue(() => 42);
            Assert.ThrowsException<OperationCanceledException>(() => task.Result);
        }

        [TestMethod]
        public void WhenMyThreadsPoolIsDisposedThenContinuationsShouldGetCancelled()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task1 = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });
            Thread.Sleep(500);
            IMyTask<int> task2 = task1.ContinueWith(val => val + 1);
            myThreadPool.Dispose();
            Assert.ThrowsException<OperationCanceledException>(() => task2.Result);
        }

        [TestMethod]
        public void WhenMyThreadsPoolIsDisposedThenNewContinuationsShouldGetCancelled()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task1 = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });
            Thread.Sleep(500);
            myThreadPool.Dispose();
            IMyTask<int> task2 = task1.ContinueWith(val => val + 1);
            Assert.ThrowsException<OperationCanceledException>(() => task2.Result);
        }

        [TestMethod]
        public void WhenEnqueuedTwoParallel1000msTasksMyThreadPoolShouldTakeBetween750msAnd1250msToRunThem()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task1 = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });
            IMyTask<int> task2 = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 52;
            });
            Assert.IsFalse(task1.IsCompleted);
            Assert.IsFalse(task2.IsCompleted);
            Thread.Sleep(750);
            Assert.IsFalse(task1.IsCompleted);
            Assert.IsFalse(task2.IsCompleted);
            Thread.Sleep(500);
            Assert.IsTrue(task1.IsCompleted);
            Assert.IsTrue(task2.IsCompleted);
            Assert.AreEqual(42, task1.Result);
            Assert.AreEqual(52, task2.Result);
            myThreadPool.Dispose();
        }

        [TestMethod]
        public void WhenEnqueued1000msTaskWithTwo1000msContinuationsMyThreadPoolShouldTakeUnder2500msToRunAllTasks()
        {
            MyThreadPool myThreadPool = new(10);
            IMyTask<int> task1 = myThreadPool.Enqueue(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });
            IMyTask<int> task2 = task1.ContinueWith(val =>
            {
                Thread.Sleep(1000);
                return val + 1;
            });
            IMyTask<int> task3 = task1.ContinueWith(val =>
            {
                Thread.Sleep(1000);
                return val + 10;
            });
            Assert.IsFalse(task1.IsCompleted);
            Assert.IsFalse(task2.IsCompleted);
            Assert.IsFalse(task3.IsCompleted);
            Thread.Sleep(2500);
            Assert.IsTrue(task1.IsCompleted);
            Assert.IsTrue(task2.IsCompleted);
            Assert.IsTrue(task3.IsCompleted);
            Assert.AreEqual(42, task1.Result);
            Assert.AreEqual(43, task2.Result);
            Assert.AreEqual(52, task3.Result);
            myThreadPool.Dispose();
        }
    }
}
