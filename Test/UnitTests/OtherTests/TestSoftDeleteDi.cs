// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Reflection;
using AssemblyBadConfig1;
using AssemblyBadConfig2;
using AssemblyBadConfig3;
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using Microsoft.Extensions.DependencyInjection;
using SoftDeleteServices.Concrete;
using SoftDeleteServices.Configuration;
using Test.ExampleConfigs;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.OtherTests
{
    public class TestSoftDeleteDi
    {
        private readonly ITestOutputHelper _output;

        public TestSoftDeleteDi(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestRegisterServiceManuallyOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            var context = new SingleSoftDelDbContext(options);
            
            //ATTEMPT
            var services = new ServiceCollection();
            services.AddScoped(x => context);
            services.AddSingleton<SingleSoftDeleteConfiguration<ISingleSoftDelete>, ConfigSoftDeleteWithUserId>();
            services.AddTransient<SingleSoftDeleteService<ISingleSoftDelete>>();
            var serviceProvider = services.BuildServiceProvider();

            //VERIFY
            var service1 = serviceProvider.GetRequiredService<SingleSoftDelDbContext>();
            var service2 = serviceProvider.GetRequiredService<SingleSoftDeleteConfiguration<ISingleSoftDelete>>();
            var service3 = serviceProvider.GetRequiredService<SingleSoftDeleteService<ISingleSoftDelete>>();
        }

        [Fact]
        public void TestRegisterServiceViaProvidedMethodTestOk()
        {
            //SETUP
            var options1 = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            var context1 = new SingleSoftDelDbContext(options1);
            var options2 = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            var context2 = new CascadeSoftDelDbContext(options2);

            //ATTEMPT
            var services = new ServiceCollection();
            services.AddScoped(x => context1);
            services.AddScoped(x => context2);
            var logs = services.RegisterSoftDelServicesAndYourConfigurations();
            var serviceProvider = services.BuildServiceProvider();

            //VERIFY
            foreach (var log in logs)
            {
                _output.WriteLine(log);
            }
            serviceProvider.GetRequiredService<SingleSoftDeleteConfiguration<ISingleSoftDelete>>();
            serviceProvider.GetRequiredService<SingleSoftDeleteConfiguration<ISingleSoftDeletedDDD>>();
            serviceProvider.GetRequiredService<CascadeSoftDeleteConfiguration<ICascadeSoftDelete>>();

            serviceProvider.GetRequiredService<SingleSoftDeleteService<ISingleSoftDelete>>();
            serviceProvider.GetRequiredService<SingleSoftDeleteService<ISingleSoftDeletedDDD>>();
            serviceProvider.GetRequiredService<CascadeSoftDelService<ICascadeSoftDelete>>();

            serviceProvider.GetRequiredService<SingleSoftDeleteServiceAsync<ISingleSoftDelete>>();
            serviceProvider.GetRequiredService<SingleSoftDeleteServiceAsync<ISingleSoftDeletedDDD>>();
            serviceProvider.GetRequiredService<CascadeSoftDelServiceAsync<ICascadeSoftDelete>>();
        }

        [Fact]
        public void TestRegisterServiceViaProvidedExceptionOnDepBaseTypeSoftDeleteOk()
        {
            //SETUP
            var services = new ServiceCollection();

            //ATTEMPT
            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.RegisterSoftDelServicesAndYourConfigurations(Assembly.GetAssembly(typeof(ConfigSoftDelete1))));

            //VERIFY
            ex.Message.ShouldStartWith("Found multiple single soft delete configurations that use the interface ISingleSoftDelete, which the services can't handle.");
        }

        [Fact]
        public void TestRegisterServiceViaProvidedExceptionOnDepBaseTypeSoftDeleteMultipleOk()
        {
            //SETUP
            var services = new ServiceCollection();

            //ATTEMPT
            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.RegisterSoftDelServicesAndYourConfigurations(Assembly.GetAssembly(typeof(ConfigSoftDelete3))));

            //VERIFY
            ex.Message.ShouldStartWith("Found multiple single soft delete configurations that use the interfaces ISingleSoftDelete and ISingleSoftDeletedDDD, which the services can't handle.");
        }

        [Fact]
        public void TestRegisterServiceViaProvidedExceptionOnDepBaseTypeCascadeDeleteOk()
        {
            //SETUP
            var services = new ServiceCollection();

            //ATTEMPT
            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.RegisterSoftDelServicesAndYourConfigurations(Assembly.GetAssembly(typeof(ConfigCascadeDelete1))));

            //VERIFY
            ex.Message.ShouldStartWith("Found multiple cascade soft delete configurations that use the interface ICascadeSoftDelete, which the services can't handle.");
        }

    }
}