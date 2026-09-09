using backend.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace backend.Functions;

/// <summary>
/// The Functions worker middleware does not populate <see cref="IHttpContextAccessor"/>
/// inside a function invocation by default (the function runs on the gRPC invocation
/// thread while the real request lives on the ASP.NET Core request thread). ASP.NET
/// Identity's SignInManager reads its context from <see cref="IHttpContextAccessor"/>,
/// so we bridge it here. See https://github.com/Azure/azure-functions-dotnet-worker/issues/2372
/// </summary>
public sealed class HttpContextAccessorMiddleware(IHttpContextAccessor httpContextAccessor) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is not null)
        {
            httpContextAccessor.HttpContext = httpContext;
        }

        try
        {
            await next(context);
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
        }
    }
}