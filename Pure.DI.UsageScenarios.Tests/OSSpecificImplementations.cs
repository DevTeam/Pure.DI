// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Shouldly;
    using Xunit;
    using Pure.DI;
    
    public enum OSPlatform
    {
        Windows,
        Linux,
        OSX
    }

    public class OSSpecificImplementations
    {
        [Fact]
        // $visible=true
        // $tag=6 Advanced
        // $priority=01
        // $description=OS specific implementations
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IOsSpecific<TT>>().As(Lifetime.Singleton).To<OsSpecific<TT>>()

                // OS specific bindings
                .Bind<IDependency>(OSPlatform.Windows).To<WindowsImpl>()
                .Bind<IDependency>(OSPlatform.Linux).To<LinuxImpl>()
                .Bind<IDependency>(OSPlatform.OSX).To<OSXImpl>()
                .Bind<IDependency>().To(ctx => ctx.Resolve<IOsSpecific<IDependency>>().Instance)

                // Other bindings
                .Bind<IService>().To<Service>();
            
            var service = OSSpecificImplementationsDI.Resolve<IService>();

            service.Run().Contains("Hello from").ShouldBeTrue();
        }
        
        public interface IOsSpecific<out T> {  T Instance { get; } }

        public class OsSpecific<T>: IOsSpecific<T>
        {
            private readonly Func<T> _windowsFactory;
            private readonly Func<T> _linuxFactory;
            private readonly Func<T> _osxFactory;

            public OsSpecific(
                [Tag(OSPlatform.Windows)] Func<T> windowsFactory,
                [Tag(OSPlatform.Linux)] Func<T> linuxFactory,
                [Tag(OSPlatform.OSX)] Func<T> osxFactory)
            {
                _windowsFactory = windowsFactory;
                _linuxFactory = linuxFactory;
                _osxFactory = osxFactory;
            }

            public T Instance =>
                    Environment.OSVersion.Platform switch
                    {
                        PlatformID.Win32S => OSPlatform.Windows,
                        PlatformID.Win32Windows => OSPlatform.Windows,
                        PlatformID.Win32NT => OSPlatform.Windows,
                        PlatformID.WinCE => OSPlatform.Windows,
                        PlatformID.Xbox => OSPlatform.Windows,
                        PlatformID.Unix => OSPlatform.Linux,
                        PlatformID.MacOSX => OSPlatform.OSX,
                        _ => throw new NotSupportedException()
                    } switch
                    {
                        OSPlatform.Windows => _windowsFactory(),
                        OSPlatform.Linux => _linuxFactory(),
                        OSPlatform.OSX => _osxFactory(),
                        _ => throw new NotSupportedException()
                    };
        }
        
        public interface IDependency { string GetMessage(); }

        public class WindowsImpl : IDependency { public string GetMessage() => "Hello from Windows"; }
        
        public class LinuxImpl : IDependency { public string GetMessage() => "Hello from Linux"; }
        
        public class OSXImpl : IDependency { public string GetMessage() => "Hello from OSX"; }

        public interface IService { string Run(); }
        
        public class Service : IService
        {
            private readonly IDependency _dependency;
            
            public Service(IDependency dependency) => _dependency = dependency;

            public string Run() => _dependency.GetMessage();
        }
        // }
    }
}
