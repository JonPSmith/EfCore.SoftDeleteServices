// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Test")]
namespace SoftDeleteServices.Concrete.Internal
{
    internal static class ValueTaskSyncCheckers
    {
        public static void CheckSyncValueTaskWorked(this ValueTask valueTask)
        {
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Expected a sync task, but got an async task");
            if (valueTask.IsFaulted)
            {
                var task = valueTask.AsTask();
                if (task.Exception?.InnerExceptions.Count == 1)
                    throw task.Exception.InnerExceptions.Single();
                if (task.Exception == null)
                    throw new InvalidOperationException("ValueTask faulted but didn't have a exception");
                throw task.Exception;
            }
        }

        public static void CheckSyncValueTaskWorked<T>(this ValueTask<T> valueTask)
        {
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Expected a sync task, but got an async task");
            if (valueTask.IsFaulted)
            {
                var task = valueTask.AsTask();
                if (task.Exception?.InnerExceptions.Count == 1)
                    throw task.Exception.InnerExceptions.Single();
                if (task.Exception == null)
                    throw new InvalidOperationException("ValueTask faulted but didn't have a exception");
                throw task.Exception;
            }
        }

        public static void CheckSyncValueTaskWorkedDynamic(this Type entityType, dynamic dynamicValueType)
        {
            var genericHelperType =
                typeof(GenericValueTypeChecker<>).MakeGenericType(entityType);
            Activator.CreateInstance(genericHelperType, (object)dynamicValueType);
        }

        private class GenericValueTypeChecker<TOut>
        {
            public GenericValueTypeChecker(dynamic valueTask)
            {
                ((ValueTask<TOut>)valueTask).CheckSyncValueTaskWorked();
            }
        }
    }
}