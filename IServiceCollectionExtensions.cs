namespace Meander;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AllowLazy(this IServiceCollection services)
    {
        var lastRegistration = services.Last();
        if (lastRegistration.Lifetime == ServiceLifetime.Singleton)
            throw new NotSupportedException();

        var lazyServiceType = typeof(Lazy<>).MakeGenericType(lastRegistration.ServiceType);
        var lazyServiceCtor = lazyServiceType.GetConstructor(new[] {
            typeof(Func<>).MakeGenericType(lastRegistration.ServiceType)
        });

        var lazyFactoryProxyType = typeof(LazyFactoryProxy<>)
            .MakeGenericType(lastRegistration.ServiceType);
        services.Add(new ServiceDescriptor(
            lazyServiceType,
            serviceLocator =>
            {
                var proxy = Activator.CreateInstance(lazyFactoryProxyType, serviceLocator,
                    lastRegistration.ServiceType ?? lastRegistration.ImplementationType);
                return lazyServiceCtor.Invoke(new[] { (proxy as ILazyFactoryProxy)!.GetLazyFactory() });
            },
            lastRegistration.Lifetime));

        return services;
    }

    private interface ILazyFactoryProxy
    {
        object GetLazyFactory();
    }

    private class LazyFactoryProxy<T> : ILazyFactoryProxy
    {
        private readonly Func<T> _result;

        public LazyFactoryProxy(IServiceProvider serviceLocator, Type serviceType) =>
            _result = () => (T)serviceLocator.GetRequiredService(serviceType);

        public object GetLazyFactory() => _result;
    }
}
