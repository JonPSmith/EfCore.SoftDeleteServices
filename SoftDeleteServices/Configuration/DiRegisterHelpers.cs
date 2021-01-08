﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
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

            var singleConfigTypes = new List<Type>();
            var cascadeConfigTypes = new List<Type>();
            foreach (var assembly in assembliesWithConfigs)
            {
                debugLogs.Add($"Starting scanning assembly {assembly.GetName().Name} for your soft delete configurations.");
                singleConfigTypes.AddRange(services.RegisterUsersConfigurationsAndReturnInterfaces(debugLogs,
                    typeof(SingleSoftDeleteConfiguration<>), assembly));
                cascadeConfigTypes.AddRange(services.RegisterUsersConfigurationsAndReturnInterfaces(debugLogs,
                    typeof(CascadeSoftDeleteConfiguration<>), assembly));
            }

            var singleDups = singleConfigTypes.GroupBy(x => x).Where(g => g.Count() > 1)
                .Select(y => y.Key).ToList();
            if (singleDups.Any())
            {
                throw new InvalidOperationException($"Found multiple configurations that use the interface {singleDups.First().Name}, which the services can't handle.\n" +
                                                    "If you have multiple configurations with that interface because you have multiple DbContexts, then you will have to use a different interface for each DbContext.\n" +
                                                    "E.g. MyContext1 would have interface ISoftDeleted1 and MyContext2 would have interface ISoftDeleted2");
            }

            //Now we register each type of soft delete service based on the interfaces found in the configurations
            foreach (var type in singleConfigTypes.Distinct())
            {
                var syncService = typeof(SingleSoftDeleteService<>).MakeGenericType(type);
                var asyncService = typeof(SingleSoftDeleteServiceAsync<>).MakeGenericType(type);
                services.AddTransient(syncService);
                services.AddTransient(asyncService);
                debugLogs.Add($"SoftDeleteServices: registered {syncService.FormDisplayType()} and {asyncService.FormDisplayType()}");
            }
            foreach (var type in cascadeConfigTypes.Distinct())
            {
                var syncService = typeof(CascadeSoftDelService<>).MakeGenericType(type);
                var asyncService = typeof(CascadeSoftDelServiceAsync<>).MakeGenericType(type);
                services.AddTransient(syncService);
                services.AddTransient(asyncService);
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

                services.AddScoped(implementationType.BaseType, implementationType);  //Scoped because it contains the the DbContext
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