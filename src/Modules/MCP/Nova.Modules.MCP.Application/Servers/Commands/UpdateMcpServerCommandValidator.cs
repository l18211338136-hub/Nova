using System;
using FluentValidation;

namespace Nova.Modules.Mcp.Application.Servers.Commands;

public class UpdateMcpServerCommandValidator : AbstractValidator<UpdateMcpServerCommand>
{
    public UpdateMcpServerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("MCP 服务 ID 不能为空。");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("服务名称为必填项。")
            .MaximumLength(100).WithMessage("服务名称长度不能超过 100 个字符。");

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("基础地址为必填项。")
            .MaximumLength(500).WithMessage("基础地址长度不能超过 500 个字符。")
            .Must(IsValidHttpUrl).WithMessage("请输入合法的 HTTP/HTTPS 基础地址。");

        RuleFor(x => x.SwaggerUrl)
            .MaximumLength(500).WithMessage("Swagger 地址长度不能超过 500 个字符。")
            .Must(url => string.IsNullOrWhiteSpace(url) || IsValidHttpUrl(url))
            .WithMessage("请输入合法的 HTTP/HTTPS Swagger 地址。");
    }

    private static bool IsValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
