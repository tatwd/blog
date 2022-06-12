---
title: ConfigureAwait 方法的作用
tags:
  - dotnet
  - 技术笔记
create_time: 2022-06-12
---

方法 `ConfigureAwait` 可用来控制 `await` 之后的代码块是否要在原上下文中进行执行，即是否要捕获原上下文。默认情况下，是会去捕获原上下文的，即相当与 `ConfigureAwait(true)`，其逻辑过程大致如下：

```csharp
object scheduler = null;
if (continueOnCapturedContext)
{
  scheduler = SynchronizationContext.Current;
  if (scheduler is null && TaskScheduler.Current != TaskScheduler.Default)
  {
    scheduler = TaskScheduler.Current;
  }
}
```

特殊情况下，如果执行上下文设置为**只允许运行一个线程**时，而恰好**此线程处于阻塞状态（状态未完成）**，那么在捕获上下文的线程中继续执行后续步骤将发生死锁。

例如，对于下列代码，`pair.ConcurrentScheduler` 调度器将创建一个线程去执行方法 `Deadlock`，当执行到 `task.Wait()`时，当前线程将发生阻塞；当 `Delay` 执行完成， 试图在当前线程中继续执行后续代码将无法成功。

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

var pair = new ConcurrentExclusiveSchedulerPair(
  taskScheduler: TaskScheduler.Default,
  maxConcurrencyLevel: 1); // 只能同时执行 1 个 task
var taskFactory = new TaskFactory(pair.ConcurrentScheduler);

await taskFactory.StartNew(Deadlock);

void Deadlock()
{
  var task = WaitAsync();
  task.Wait(); // 阻塞当前线程
}

async Task WaitAsync()
{
  Console.WriteLine(TaskScheduler.Current.Id);
  Console.WriteLine(Task.CurrentId);
  Console.WriteLine(Environment.CurrentManagedThreadId);

  await Task.Delay(TimeSpan.FromSeconds(1));

  // 后续代码将无法执行
  Console.WriteLine(TaskScheduler.Current.Id);
  Console.WriteLine(Task.CurrentId);
  Console.WriteLine(Environment.CurrentManagedThreadId);
}
```

要解决这个问题，再不改变调用方行为的情况，可以通过如下修改解决：

```csharp
await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false)
```
因为这将使后续代码在 `TaskScheduler.Default` 中寻找线程进行执行。

当然，对于这个例子，调用 `task.Wait()` 这个阻塞方法才是最应该处理的问题，应使用 `await`/`async` 进行解决。

## 参考资料

- [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
