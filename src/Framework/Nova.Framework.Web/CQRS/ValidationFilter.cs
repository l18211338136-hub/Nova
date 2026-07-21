using FluentValidation;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;

namespace Nova.Framework.Web.CQRS;

public class ValidationFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("validation");
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        // 从当前依赖注入容器作用域中获取所有针对该消息类型注册的校验器
        var validators = context.GetPayload<IServiceProvider>().GetServices<IValidator<T>>();
        
        if (validators.Any())
        {
            var validationContext = new ValidationContext<T>(context.Message);

            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(validationContext, context.CancellationToken))
            );

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count > 0)
            {
                // 构造只包含错误信息的简洁 Message，避免前端展示多余内容（如 Property Name, Severity 等）
                var errorMsg = string.Join(" ", failures.Select(f => f.ErrorMessage));
                throw new NovaValidationException(errorMsg);
            }
        }

        await next.Send(context);
    }
}
