// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Test")]
namespace SoftDeleteServices.Concrete.Internal
{
    internal static class ValueTaskSyncCheckers
    {
        /// <summary>
        /// This will check the <see cref="ValueTask"/> returned
        /// by a method and ensure it didn't run any async methods.
        /// It then calls GetAwaiter().GetResult() which will
        /// bubble up an exception if there is one
        /// </summary>
        /// <param name="valueTask">The ValueTask from a method that didn't call any async methods</param>
        public static void CheckSyncValueTaskWorked(this ValueTask valueTask)
        {
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Expected a sync task, but got an async task");
            //Stephen Toub recommended calling GetResult every time.
            //This helps with pooled resources, that use the GetResult call to tell it has finished being used
            valueTask.GetAwaiter().GetResult();
        }

        /// <summary>
        /// This will check the <see cref="ValueTask{TResult}"/> returned
        /// by a method and ensure it didn't run any async methods.
        /// It then calls GetAwaiter().GetResult() to return the result
        /// Calling .GetResult() will also bubble up an exception if there is one
        /// </summary>
        /// <param name="valueTask">The ValueTask from a method that didn't call any async methods</param>
        /// <returns>The result returned by the method</returns>
        public static TResult CheckSyncValueTaskWorkedAndReturnResult<TResult>(this ValueTask<TResult> valueTask)
        {
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Expected a sync task, but got an async task");
            return valueTask.GetAwaiter().GetResult();
        }

        public static TResult CheckSyncValueTaskWorkedDynamicAndReturnResult<TResult>(dynamic dynamicValueType)
        {
            var genericHelperType =
                typeof(GenericValueTypeChecker<>).MakeGenericType(typeof(TResult));
            try
            {
                var runner = Activator.CreateInstance(typeof(GenericValueTypeChecker<TResult>), 
                    dynamicValueType);
                return ((GenericValueTypeChecker<TResult>) runner).Result;
            }
            catch (Exception e)
            {
                ExceptionDispatchInfo.Capture(e?.InnerException ?? e).Throw();
            }

            return default;
        }

        private class GenericValueTypeChecker<TResult>
        {
            public TResult Result { get; }

            public GenericValueTypeChecker(dynamic valueTask)
            {
                Result = ((ValueTask<TResult>) valueTask).CheckSyncValueTaskWorkedAndReturnResult();
            }
        }
    }
}