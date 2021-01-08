// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SoftDeleteServices.Concrete;

namespace SoftDeleteServices.Configuration
{
    /// <summary>
    /// Holds extension method for finding and registering your Soft Delete configurations and services
    /// </summary>
    public static class DiRegisterHelpers
    {
        /// <summary>
        /// This will scan the assemblies that you provide (or the calling assembly) for your Soft Delete configurations
        /// who base class is SingleSoftDeleteConfiguration or CascadeSoftDeleteConfiguration.
        /// From your configurations it will register the correct types of the services you need to call.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assembliesWithConfigs">If not provided it will scan just the assembly that called this method</param>
        /// <returns>Logs about what was registered. Useful if the registering doesn't do what you want</returns>
        public static IList<string> RegisterSoftDelServicesAndYourConfigurations(this IServiceCollection services,
            params Assembly[] assembliesWithConfigs)
        {
            var debugLogs = new List<string>();
            
            if (assembliesWithConfigs.Length == 0)
            {
                assembliesWithConfigs = new[] { Assembly.GetCallingAssembly() };
                debugLogs.Add($"No assemblies provided so only scanning the calling assembly, .{assembliesWithConfigs.Single().GetName().Name}");
            }

            var singleInterfaceTypes = new List<Type>();
            var cascadeInterfaceTypes = new List<Type>();
            foreach (var assembly in assembliesWithConfigs)
            {
                debugLogs.Add($"Starting scanning assembly {assembly.GetName().Name} for your soft delete configurations.");
                singleInterfaceTypes.AddRange(services.RegisterUsersConfigurationsAndReturnInterfaces(debugLogs,
                    typeof(SingleSoftDeleteConfiguration<>), assembly));
                cascadeInterfaceTypes.AddRange(services.RegisterUsersConfigurationsAndReturnInterfaces(debugLogs,
                    typeof(CascadeSoftDeleteConfiguration<>), assembly));
            }

            //Now we register each type of soft delete service based on the interfaces found in the configurations
            foreach (var type in singleInterfaceTypes.Distinct())
            {
                var syncService = typeof(SingleSoftDeleteService<>).MakeGenericType(type);
                var asyncService = typeof(SingleSoftDeleteServiceAsync<>).MakeGenericType(type);
                services.AddTransient(syncService);
                debugLogs.Add($"SoftDeleteServices: registered {syncService.FormDisplayType()} and {asyncService.FormDisplayType()}");
            }
            foreach (var type in cascadeInterfaceTypes.Distinct())
            {
                var syncService = typeof(CascadeSoftDelService<>).MakeGenericType(type);
                var asyncService = typeof(CascadeSoftDelServiceAsync<>).MakeGenericType(type);
                services.AddTransient(syncService);
                debugLogs.Add($"CascadeSoftDeleteServices: registered {syncService.FormDisplayType()} and {asyncService.FormDisplayType()}");
            }

            return debugLogs;
        }

        private static List<Type> RegisterUsersConfigurationsAndReturnInterfaces(this IServiceCollection services,
            List<string> debugLogs,
            Type genericClassBaseType, Assembly assembly)
        {
            var classTypes = new List<Type>();
            var allGenericClasses = assembly.GetExportedTypes()
                .Where(y => y.IsClass && !y.IsAbstract && !y.IsNested && !y.IsGenericType &&
                            y.BaseType.IsGenericType && y.BaseType?.GetGenericTypeDefinition() == genericClassBaseType).ToList();
            foreach (var implementationType in allGenericClasses)
            {
                var genericPart = implementationType.BaseType.GetGenericArguments();
                classTypes.Add(genericPart.Single());

                services.AddSingleton(implementationType.BaseType, implementationType);
                debugLogs.Add($"Registered your configuration class {implementationType.Name}");
            }

            return classTypes;
        }

        private static string FormDisplayType(this Type interfaceType)
        {
            var genericPart = interfaceType.GetGenericArguments(); 
            var indexCharToRemove = interfaceType.Name.IndexOf('`');
            return $"{interfaceType.Name.Substring(0, indexCharToRemove)}<{genericPart.Single().Name}>";
        }
    }
}