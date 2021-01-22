// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SoftDeleteServices.Concrete.Internal;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.OtherTests
{
    public class TestValueTask
    {
        private readonly ITestOutputHelper _output;

        public TestValueTask(ITestOutputHelper output)
        {
            _output = output;
        }

        [Flags]
        public enum VTaskOptions { Sync = 0, Async = 1, CancelAsync = 2, ThrowException = 4}
        
        private async ValueTask ValueTaskMethod(VTaskOptions options, int depth = 0)
        {
            if (options.HasFlag(VTaskOptions.Async))
            {
                var cancelToken = options.HasFlag(VTaskOptions.Async)
                    ? new CancellationToken()
                    : CancellationToken.None;
                await Task.Delay(1, cancelToken);
            }

            if (depth > 0 && !options.HasFlag(VTaskOptions.Async))
            {
                var valueTask = ValueTaskMethod(options, depth - 1);
                valueTask.CheckSyncValueTaskWorked();
                return;
            }

            if (options.HasFlag(VTaskOptions.ThrowException))
                throw new Exception("Exception thrown");
        }

        private async ValueTask<int> ValueTaskIntMethod(VTaskOptions options)
        {
            if (options.HasFlag(VTaskOptions.Async))
            {
                var cancelToken = options.HasFlag(VTaskOptions.Async)
                    ? new CancellationToken()
                    : CancellationToken.None;
                await Task.Delay(1, cancelToken);
            }

            if (options.HasFlag(VTaskOptions.ThrowException))
                throw new InvalidOperationException("Exception thrown");

            return 1;
        }

        private void CheckSyncValueTask(ValueTask valueTask, VTaskOptions options)
        {
            if (options.HasFlag(VTaskOptions.ThrowException))
            {
                valueTask.IsFaulted.ShouldBeTrue();
                valueTask.IsCompleted.ShouldBeTrue();
                valueTask.IsCompletedSuccessfully.ShouldBeFalse();
                
                var task = valueTask.AsTask();
                task.Exception.ShouldNotBeNull();
            }
            else
            {
                valueTask.IsFaulted.ShouldBeFalse();
                valueTask.IsCompleted.ShouldBeTrue();
                valueTask.IsCompletedSuccessfully.ShouldBeTrue();

                var task = valueTask.AsTask();
                task.Exception.ShouldBeNull();
            }

            valueTask.IsCanceled.ShouldEqual(options.HasFlag(VTaskOptions.CancelAsync));
        }

        //----------------------------------------------------------------------
        //Tests

        [Theory]
        [InlineData(VTaskOptions.Sync)]
        [InlineData(VTaskOptions.ThrowException)]
        public void TestValueTaskMethodSyncOk(VTaskOptions options)
        {
            //SETUP

            //ATTEMPT
            var valueTask = ValueTaskMethod(options);

            //VERIFY
            CheckSyncValueTask(valueTask, options);
        }

        [Theory]
        [InlineData(VTaskOptions.Sync)]
        [InlineData(VTaskOptions.Async)]
        [InlineData(VTaskOptions.ThrowException)]
        public void TestCheckSyncValueTaskWorked(VTaskOptions options)
        {
            //SETUP
            var valueTask = ValueTaskMethod(options);

            //ATTEMPT
            try
            {
                valueTask.CheckSyncValueTaskWorked();
            }
            catch (Exception e)
            {
                options.ShouldNotEqual(VTaskOptions.Sync);
                if (options.HasFlag(VTaskOptions.Async))
                    e.Message.ShouldEqual("Expected a sync task, but got an async task");
                if (options.HasFlag(VTaskOptions.ThrowException))
                    e.Message.ShouldEqual("Exception thrown");

                return;
            }

            //VERIFY
            options.ShouldEqual(VTaskOptions.Sync);
        }

        [Theory]
        [InlineData(VTaskOptions.Sync)]
        [InlineData(VTaskOptions.ThrowException)]
        public void TestValueTaskIntMethodSyncOk(VTaskOptions options)
        {
            //SETUP

            //ATTEMPT
            var valueTaskInt = ValueTaskIntMethod(options);

            //VERIFY
            valueTaskInt.IsFaulted.ShouldEqual(options.HasFlag(VTaskOptions.ThrowException));
        }

        [Theory]
        [InlineData(VTaskOptions.Sync)]
        [InlineData(VTaskOptions.Async)]
        [InlineData(VTaskOptions.ThrowException)]
        public void TestCheckSyncValueTaskIntWorked(VTaskOptions options)
        {
            //SETUP
            var valueTaskInt = ValueTaskIntMethod(options);

            //ATTEMPT
            try
            {
                valueTaskInt.CheckSyncValueTaskWorked();
            }
            catch (Exception e)
            {
                options.ShouldNotEqual(VTaskOptions.Sync);
                if (options.HasFlag(VTaskOptions.Async))
                    e.Message.ShouldEqual("Expected a sync task, but got an async task");
                if (options.HasFlag(VTaskOptions.ThrowException))
                    e.Message.ShouldEqual("Exception thrown");

                return;
            }

            //VERIFY
            options.ShouldEqual(VTaskOptions.Sync);
        }


        //----------------------------------------------------------------------
        //sync depth exception

        [Theory]
        [InlineData(VTaskOptions.Sync)]
        [InlineData(VTaskOptions.ThrowException)]
        public void TestValueTaskMethodSyncWithDepthOk(VTaskOptions options)
        {
            //SETUP

            //ATTEMPT
            var valueTask = ValueTaskMethod(options, 1);

            //VERIFY
            CheckSyncValueTask(valueTask, options);
        }

        //----------------------------------------------------------------------
        // Async

        [Theory]
        [InlineData(VTaskOptions.Async)]
        [InlineData(VTaskOptions.Async | VTaskOptions.CancelAsync)]
        [InlineData(VTaskOptions.Async | VTaskOptions.ThrowException)]
        public async Task TestValueTaskMethodAsyncOk(VTaskOptions options)
        {
            //SETUP

            //ATTEMPT
            try
            {
                await ValueTaskMethod(options);
            }
            catch
            {
                options.HasFlag(VTaskOptions.ThrowException).ShouldBeTrue();
                return;
            }

            //VERIFY
            options.HasFlag(VTaskOptions.ThrowException).ShouldBeFalse();
        }

        [Theory]
        [InlineData(VTaskOptions.Async)]
        [InlineData(VTaskOptions.Async | VTaskOptions.CancelAsync)]
        [InlineData(VTaskOptions.Async | VTaskOptions.ThrowException)]
        public async Task TestValueTaskIntMethodAsyncOk(VTaskOptions options)
        {
            //SETUP
            int result;
            
            //ATTEMPT
            try
            {
                result = await ValueTaskIntMethod(options);
            }
            catch
            {
                options.HasFlag(VTaskOptions.ThrowException).ShouldBeTrue();
                return;
            }

            //VERIFY
            options.HasFlag(VTaskOptions.ThrowException).ShouldBeFalse();
            result.ShouldEqual(1);
        }


    }
}