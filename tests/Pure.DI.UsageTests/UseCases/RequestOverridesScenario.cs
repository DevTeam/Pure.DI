/*
$v=true
$p=12
$d=Request overrides
$h=When this occurs: you need per-request overrides with different scopes for nested services.
$h=What it solves: applies request data to the main workflow while keeping background or system dependencies isolated.
$h=How it is solved in the example: uses nested factories and overrides to select the nearest context.
$f=
$f=What it shows:
$f=- Demonstrates override precedence across nested factories.
$f=- Shows that the closest override wins for deeper dependencies.
$f=
$f=Important points:
$f=- Multiple overrides for the same type pick the nearest one in the graph.
$f=- The last override on the same level wins.
$f=
$f=Useful when:
$f=- You handle multi-tenant requests and need system services to run under a system context.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.UseCases.RequestOverridesScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();

        var handler = composition.CreateHandler(new Request("tenant-a", "user-1"));
// }
        handler.Service.Context.TenantId.ShouldBe("tenant-a");
        handler.Service.Context.UserId.ShouldBe("user-1");
        handler.Service.Repository.Context.IsSystem.ShouldBeTrue();
        handler.Service.Audit.Context.UserId.ShouldBe("user-1");

        composition.SaveClassDiagram();
    }
}

// {
record Request(string TenantId, string UserId);

interface IRequestContext
{
    string TenantId { get; }

    string UserId { get; }

    bool IsSystem { get; }
}

class RequestContext(string tenantId, string userId, bool isSystem) : IRequestContext
{
    public static RequestContext System => new("system", "system", true);

    public string TenantId { get; } = tenantId;

    public string UserId { get; } = userId;

    public bool IsSystem { get; } = isSystem;
}

interface IRepository
{
    IRequestContext Context { get; }
}

class Repository(IRequestContext context) : IRepository
{
    public IRequestContext Context { get; } = context;
}

interface IAuditWriter
{
    IRequestContext Context { get; }
}

class AuditWriter(IRequestContext context) : IAuditWriter
{
    public IRequestContext Context { get; } = context;
}

class Service(
    IRequestContext context,
    Func<IRepository> repositoryFactory,
    IAuditWriter audit)
{
    public IRequestContext Context { get; } = context;

    public IRepository Repository { get; } = repositoryFactory();

    public IAuditWriter Audit { get; } = audit;
}

class Handler(Service service)
{
    public Service Service { get; } = service;
}

partial class Composition
{
    private void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IRepository>().To<Repository>()
            .Bind<IAuditWriter>().To<AuditWriter>()
            .Bind().To<Service>()
            .Bind().To<Handler>()
            .Bind().To<Func<IRepository>>(ctx => () =>
            {
                // Inner override applies to repository dependencies only.
                ctx.Override<IRequestContext>(RequestContext.System);
                ctx.Inject(out IRepository repository);
                return repository;
            })
            .Bind().To<Func<Request, Handler>>(ctx => request =>
            {
                // Outer override applies to the request handler and its main workflow.
                ctx.Override<IRequestContext>(new RequestContext(request.TenantId, request.UserId, false));
                ctx.Inject(out Handler handler);
                return handler;
            })
            .Root<Func<Request, Handler>>(nameof(CreateHandler));
}
// }
