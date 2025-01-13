using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;
using WebApi.EndPointsBuilder.Attributes;

namespace WebApi.EndPointsBuilder
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all implementations of the IEndPoint<O> and IEndPoint<I, O> interfaces in the DI container.
        /// </summary>
        /// <param name="services">IServiceCollection for registering dependencies.</param>
        /// <returns>IServiceCollection for later use.</returns>
        public static IServiceCollection AddBaseEndPointHandlers(this IServiceCollection services)
        {
            foreach (var assembly in GetAllAsseblies())
            {
                var handlerTypes = assembly.GetTypes()
            .Where(t =>
                    !t.IsAbstract &&                // Исключаем абстрактные классы
                    !t.IsInterface &&               // Исключаем интерфейсы
                    t.GetInterfaces().Contains(typeof(IEndPoint))) // Проверяем, реализует ли тип интерфейс IEndPoint
            .ToList();
                if (handlerTypes.Count > 0)
                {
                    foreach (var handlerType in handlerTypes)
                    {
                        services.AddTransient(handlerType);
                    }
                }
            }
            return services;
        }

        /// <summary>
        /// Registers endpoints in WebApplication for all IEndPoint<O> and IEndPoint<I, O> implementations.
        /// </summary>
        /// <param name="app">WebApplication to register endpoints for.</param>
        /// <exception cref="NotSupportedException"></exception>
        public static void RegisterEndPoints(this WebApplication app)
        {
            GetValidationFilter();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                    .Where(t =>
                        !t.IsAbstract &&
                        !t.IsInterface &&
                        t.GetInterfaces().Contains(typeof(IEndPoint)))
                    .ToList();

                foreach (var handlerType in handlerTypes)
                {
                    using var scope = app.Services.CreateScope();
                    var handlerInstance = scope.ServiceProvider.GetService(handlerType);
                    var endPointMethods = handlerType.GetMethods()
                        .Where(m => m.GetCustomAttribute<EndPointMethodAttribute>(inherit: false) != null)
                        .ToList();

                    foreach (var endPointMethod in endPointMethods)
                    {
                        var httpAttr = endPointMethod.GetCustomAttribute<HttpMethodAttribute>(inherit: false);
                        if (httpAttr == null)
                        {
                            throw new InvalidOperationException($"Атрибут HttpMethodAttribute не найден для метода '{endPointMethod.Name}' в типе {handlerType.Name}.");
                        }

                        var needValidationFilter = endPointMethod.GetCustomAttributes<ValidationEndPointAttribute>(inherit: false).Any();
                        var endpointRoute = httpAttr.Template;

                        var handlerParameters = endPointMethod.GetParameters().Select(p => p.ParameterType).ToArray();
                        var handlerMethodReturnType = endPointMethod.ReturnType;

                        Type delegateType = handlerParameters.Length == 0
                            ? Expression.GetDelegateType(handlerMethodReturnType)
                            : Expression.GetDelegateType(handlerParameters.Concat(new[] { handlerMethodReturnType }).ToArray());

                        var delegateInstance = Delegate.CreateDelegate(delegateType, handlerInstance, endPointMethod);

                        foreach (var httpMet in httpAttr.HttpMethods)
                        {
                            RouteHandlerBuilder rhb;
                            switch (httpMet)
                            {
                                case "POST":
                                    rhb = app.MapPost(endpointRoute, delegateInstance)
                                    .WithOpenApi()
                                    .AddValidationFilter(needValidationFilter);
                                    break;
                                case "GET":
                                    app.MapGet(endpointRoute, delegateInstance)
                                    .WithOpenApi()
                                    .AddValidationFilter(needValidationFilter);
                                    break;
                                case "PUT":
                                    app.MapPut(endpointRoute, delegateInstance)
                                    .WithOpenApi()
                                    .AddValidationFilter(needValidationFilter);
                                    break;
                                case "DELETE":
                                    app.MapDelete(endpointRoute, delegateInstance)
                                    .WithOpenApi()
                                    .AddValidationFilter(needValidationFilter);
                                    break;
                                case "PATCH":
                                    app.MapPatch(endpointRoute, delegateInstance)
                                    .WithOpenApi()
                                    .AddValidationFilter(needValidationFilter);
                                    break;
                                case "OPTIONS":
                                    app.MapMethods(endpointRoute, new[] { "OPTIONS" }, delegateInstance)
                                    .WithOpenApi()
                                    .AddValidationFilter(needValidationFilter);
                                    break;
                                default:
                                    throw new NotSupportedException($"HTTP метод  не поддерживается.");
                            }
                        }
                    }
                }
            }
        }

        private static RouteHandlerBuilder AddValidationFilter(this RouteHandlerBuilder builder, bool isAdd)
        {
            var filterType = GetValidationFilter();
            if (!typeof(IEndpointFilter).IsAssignableFrom(filterType))
                throw new ArgumentException($"{filterType.Name} не реализует IEndpointFilter");
            var filterInstance = Activator.CreateInstance(filterType) as IEndpointFilter;
            if (filterInstance == null)
                throw new ArgumentException($"Не удалось создать экземпляр типа {filterType.Name}");
            if(isAdd)
            {
                builder.AddEndpointFilter(filterInstance);
            }
            return builder;
        }
        /// <summary>
        /// Возвращает валидационный фильтр
        /// </summary>
        /// <returns></returns>
        private static Type GetValidationFilter()
        {
            List<Type> validationFilters = new List<Type>();
            foreach (var assembly in GetAllAsseblies())
            {
                try
                {
                    var filterTypes = assembly.GetTypes()
                        .Where(t => typeof(IEndpointFilter).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                        .ToList();
                    validationFilters.AddRange(filterTypes);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Console.WriteLine($"Ошибка получения типов из сборки {assembly.FullName}: {ex.Message}");
                }
            }
            return validationFilters.FirstOrDefault();
        }

        /// <summary>
        /// Возвращает все сборки
        /// </summary>
        /// <returns></returns>
        public static List<Assembly> GetAllAsseblies()
        {            
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var assemblyFiles = Directory.GetFiles(appDirectory, "*.dll");
            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFile);
                    if (!loadedAssemblies.Any(a => a.FullName == assemblyName.FullName))
                    {
                        loadedAssemblies.Add(AppDomain.CurrentDomain.Load(assemblyName));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки сборки {assemblyFile}: {ex.Message}");
                }
            }
            return loadedAssemblies;
        }
    }
}