using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Nova.UnitTests.Fakes;

/// <summary>
/// 最小 IBackgroundJobClient 测试替身。
/// <para>
/// 真实实现 <see cref="Create"/> / <see cref="ChangeState"/>，将被 Enqueue 的 Job
/// 收集到 <see cref="EnqueuedJobs"/> 供测试断言使用。
/// </para>
/// <para>
/// 用于绕开 NSubstitute 无法拦截 Hangfire <c>Enqueue</c> 扩展方法
/// （<c>Job.FromExpression</c> 内部调用）导致崩溃的问题。
/// </para>
/// </summary>
public sealed class FakeBackgroundJobClient : IBackgroundJobClient
{
    /// <summary>所有被 Enqueue 的 Job，按入队顺序排列。</summary>
    public List<Job> EnqueuedJobs { get; } = new();

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(job);
        return Guid.NewGuid().ToString("N");
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;

    public void Dispose() { }
}
