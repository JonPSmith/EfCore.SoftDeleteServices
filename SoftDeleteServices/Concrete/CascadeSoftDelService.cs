// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Concrete.Internal;
using SoftDeleteServices.Configuration;
using StatusGeneric;

namespace SoftDeleteServices.Concrete
{
    public enum CascadeSoftDelWhatDoing { SoftDelete, ResetSoftDelete, CheckWhatWillDelete, HardDeleteSoftDeleted }

    /// <summary>
    /// This service handles multiple, cascade soft delete, i.e. it soft deletes an entity and its dependent relationships
    /// </summary>
    /// <typeparam name="TInterface">You provide the interface you applied to your entity classes to require a boolean flag</typeparam>
    public class CascadeSoftDelService<TInterface>
        where TInterface : class
    {
        private readonly DbContext _context;
        private readonly CascadeSoftDeleteConfiguration<TInterface> _config;

        /// <summary>
        /// This provides a equivalent to a SQL cascade delete, but using a soft delete approach.
        /// </summary>
        /// <param name="config"></param>
        public CascadeSoftDelService(CascadeSoftDeleteConfiguration<TInterface> config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _context = config.Context ?? throw new ArgumentNullException(nameof(config), "You must provide the DbContext");

            if (_config.GetSoftDeleteValue == null)
                throw new InvalidOperationException($"You must set the {nameof(_config.GetSoftDeleteValue)} with a query to get the soft delete byte");
            if (_config.SetSoftDeleteValue == null)
                throw new InvalidOperationException($"You must set the {nameof(_config.SetSoftDeleteValue)} with a function to set the value of the soft delete bool");

        }

        /// <summary>
        /// This finds the entity using its primary key(s) and then cascade soft deletes the entity any dependent entities with the correct delete behaviour
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="keyValues">primary key values</param>
        /// <returns>Returns status. If not errors then Result has the number of entities that have been soft deleted. Zero if error of Not Found and notFoundAllowed is true</returns>
        public IStatusGeneric<int> SetCascadeSoftDeleteViaKeys<TEntity>(params object[] keyValues)
            where TEntity : class, TInterface
        {
            return CheckExecuteCascadeSoftDelete<TEntity>(SetCascadeSoftDelete, keyValues);
        }

        /// <summary>
        /// This finds the entity using its primary key(s) and then resets the soft delete flag so it is now visible
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="keyValues">primary key values</param>
        /// <returns>Returns status. If not errors then Result has the number of entities that have been reset. Zero if error of Not Found and notFoundAllowed is true</returns>
        public IStatusGeneric<int> ResetCascadeSoftDeleteViaKeys<TEntity>(params object[] keyValues)
            where TEntity : class, TInterface
        {
            return CheckExecuteCascadeSoftDelete<TEntity>(ResetCascadeSoftDelete, keyValues);
        }

        /// <summary>
        /// This finds the entity using its primary key(s) and counts this entity and any dependent entities
        /// that are already been cascade soft deleted and are valid to be hard deleted.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="keyValues">primary key values</param>
        /// <returns>Returns status. If not errors then Message contains a message to warn what will be deleted if the HardDelete... method is called.
        /// Zero if error of Not Found and notFoundAllowed is true</returns>
        public IStatusGeneric<int> CheckCascadeSoftDeleteViaKeys<TEntity>(params object[] keyValues)
            where TEntity : class, TInterface
        {
            return CheckExecuteCascadeSoftDelete<TEntity>( (entity, _) => CheckCascadeSoftDelete(entity), keyValues);
        }

        /// <summary>
        /// This finds the entity using its primary key(s) and hard deletes this entity and any dependent entities
        /// that are already been cascade soft deleted.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="keyValues">primary key values</param>
        /// <returns>Returns status. If not errors then Result has the number of entities that have been hard deleted. Zero if error of Not Found and notFoundAllowed is true</returns>
        public IStatusGeneric<int> HardDeleteSoftDeletedEntriesViaKeys<TEntity>(params object[] keyValues)
            where TEntity : class, TInterface
        {
            return CheckExecuteCascadeSoftDelete<TEntity>(HardDeleteSoftDeletedEntries, keyValues);
        }

        /// <summary>
        /// This with cascade soft delete this entity and any dependent entities with the correct delete behaviour
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="softDeleteThisEntity">entity class with cascade soft delete interface. Mustn't be null</param>
        /// <param name="callSaveChanges">Defaults to calling SaveChanges. If set to false, then you must call SaveChanges</param>
        /// <returns>Returns a status. If no errors then Result contains the number of entities that had been cascaded deleted, plus summary string in Message part</returns>
        public IStatusGeneric<int> SetCascadeSoftDelete<TEntity>(TEntity softDeleteThisEntity, bool callSaveChanges = true)
            where TEntity : class, TInterface
        {
            if (softDeleteThisEntity == null) throw new ArgumentNullException(nameof(softDeleteThisEntity));

            //If is a one-to-one entity we return an error
            var keys = _context.Entry(softDeleteThisEntity).Metadata.GetForeignKeys();
            if (!keys.All(x => x.DependentToPrincipal?.IsCollection == true || x.PrincipalToDependent?.IsCollection == true))
                //This it is a one-to-one entity
                throw new InvalidOperationException("You cannot soft delete a one-to-one relationship. " +
                                                    "It causes problems if you try to create a new version.");

            var status = new StatusGenericHandler<int>();
            if (_config.GetSoftDeleteValue.Compile().Invoke(softDeleteThisEntity) != 0)
                return status.AddError($"This entry is already {_config.TextSoftDeletedPastTense}.");

            var walker = new CascadeWalker<TInterface>(_context, _config, false,
                CascadeSoftDelWhatDoing.SoftDelete, _config.ReadEveryTime);
            var valueTask = walker.WalkEntitiesSoftDelete(softDeleteThisEntity, 1);
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Can only run sync tasks");
            if (callSaveChanges)
                _context.SaveChanges();
            
            return ReturnSuccessFullResult(CascadeSoftDelWhatDoing.SoftDelete, walker.NumFound);
        }

        /// <summary>
        /// This will result the cascade soft delete flag on this entity and any dependent entities with the correct delete behaviour and cascade level
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="resetSoftDeleteThisEntity">entity class with cascade soft delete interface. Mustn't be null</param>
        /// <param name="callSaveChanges">Defaults to calling SaveChanges. If set to false, then you must call SaveChanges</param>
        /// <returns>Returns a status. If no errors then Result contains the number of entities that had been reset, plus summary string in Message part</returns>
        public IStatusGeneric<int> ResetCascadeSoftDelete<TEntity>(TEntity resetSoftDeleteThisEntity, bool callSaveChanges = true)
            where TEntity : class, TInterface
        {
            if (resetSoftDeleteThisEntity == null) throw new ArgumentNullException(nameof(resetSoftDeleteThisEntity));

            var status = new StatusGenericHandler<int>();
            var currentDeleteLevel = _config.GetSoftDeleteValue.Compile().Invoke(resetSoftDeleteThisEntity);
            if (currentDeleteLevel == 0)
                return status.AddError($"This entry isn't {_config.TextSoftDeletedPastTense}.");

            if (currentDeleteLevel > 1)
                return status.AddError($"This entry was soft deleted {currentDeleteLevel - 1} " +
                    $"level{(currentDeleteLevel > 2  ? "s" : "")} above here");

            //For reset you need to read every time because some of the collection might be soft deleted already
            var walker = new CascadeWalker<TInterface>(_context, _config, false,
                CascadeSoftDelWhatDoing.ResetSoftDelete, false);
            var valueTask = walker.WalkEntitiesSoftDelete(resetSoftDeleteThisEntity, 1);
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Can only run sync tasks");
            if (callSaveChanges)
                _context.SaveChanges();

            return ReturnSuccessFullResult(CascadeSoftDelWhatDoing.ResetSoftDelete, walker.NumFound);
        }

        /// <summary>
        /// This looks for this entity and any dependent entities that are already been cascade soft deleted and are valid to be hard deleted. 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="checkHardDeleteThisEntity">entity class with cascade soft delete interface. Mustn't be null</param>
        /// <returns>Returns a status. If no errors then Result contains the number of entities which are eligible for hard delete, plus summary string in Message part</returns>
        public IStatusGeneric<int> CheckCascadeSoftDelete<TEntity>(TEntity checkHardDeleteThisEntity)
            where TEntity : class, TInterface
        {
            if (checkHardDeleteThisEntity == null) throw new ArgumentNullException(nameof(checkHardDeleteThisEntity));
            var status = new StatusGenericHandler<int>();
            if (_config.GetSoftDeleteValue.Compile().Invoke(checkHardDeleteThisEntity) == 0)
                return status.AddError($"This entry isn't {_config.TextSoftDeletedPastTense}.");

            //For reset you need to read every time because some of the collection might be soft deleted already
            var walker = new CascadeWalker<TInterface>(_context, _config, false,
                CascadeSoftDelWhatDoing.CheckWhatWillDelete, _config.ReadEveryTime);
            var valueTask = walker.WalkEntitiesSoftDelete(checkHardDeleteThisEntity, 1);
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Can only run sync tasks");
            return ReturnSuccessFullResult(CascadeSoftDelWhatDoing.CheckWhatWillDelete, walker.NumFound);
        }

        /// <summary>
        /// This hard deletes this entity and any dependent entities that are already been cascade soft deleted  
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="hardDeleteThisEntity">entity class with cascade soft delete interface. Mustn't be null</param>
        /// <param name="callSaveChanges">Defaults to calling SaveChanges. If set to false, then you must call SaveChanges</param>
        /// <returns>Returns a status. If no errors then Result contains the number of entities which were hard deleted, plus summary string in Message part</returns>
        public IStatusGeneric<int> HardDeleteSoftDeletedEntries<TEntity>(TEntity hardDeleteThisEntity, bool callSaveChanges = true)
            where TEntity : class, TInterface
        {
            if (hardDeleteThisEntity == null) throw new ArgumentNullException(nameof(hardDeleteThisEntity));
            var status = new StatusGenericHandler<int>();
            if (_config.GetSoftDeleteValue.Compile().Invoke(hardDeleteThisEntity) == 0)
                return status.AddError($"This entry isn't {_config.TextSoftDeletedPastTense}.");

            //For reset you need to read every time because some of the collection might be soft deleted already
            var walker = new CascadeWalker<TInterface>(_context, _config, false,
                CascadeSoftDelWhatDoing.HardDeleteSoftDeleted, false);
            var valueTask = walker.WalkEntitiesSoftDelete(hardDeleteThisEntity, 1);
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Can only run sync tasks");
            if (callSaveChanges)
                _context.SaveChanges();

            return ReturnSuccessFullResult(CascadeSoftDelWhatDoing.HardDeleteSoftDeleted, walker.NumFound);
        }


        /// <summary>
        /// This returns the cascade soft deleted entities of type TEntity that can be reset, i.e. SoftDeleteLevel == 1
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public IQueryable<TEntity> GetSoftDeletedEntries<TEntity>()
            where TEntity : class, TInterface
        {
            return _context.Set<TEntity>().IgnoreQueryFilters().Where(_config.FilterToGetValueCascadeSoftDeletedEntities<TEntity, TInterface>((byte)1));
        }

        //---------------------------------------------------------
        //private methods

        public IStatusGeneric<int> CheckExecuteCascadeSoftDelete<TEntity>(
            Func<TInterface, bool, IStatusGeneric<int>> softDeleteAction, params object[] keyValues)
            where TEntity : class, TInterface
        {
            var status = new StatusGenericHandler<int>();
            var valueTask = _context.LoadEntityViaPrimaryKeys<TEntity>(_config.OtherFilters, false, keyValues);
            if (!valueTask.IsCompleted)
                throw new InvalidOperationException("Can only run sync tasks");
            if (valueTask.Result == null)
            {
                if (!_config.NotFoundIsNotAnError)
                    status.AddError("Could not find the entry you ask for.");
                return status;
            }

            return softDeleteAction(valueTask.Result, true);
        }

        private IStatusGeneric<int> ReturnSuccessFullResult(CascadeSoftDelWhatDoing whatDoing, int numFound)
        {
            var status = new StatusGenericHandler<int>();
            status.SetResult(numFound);
            switch (whatDoing)
            {
                case CascadeSoftDelWhatDoing.SoftDelete:
                    status.Message = FormMessage("soft deleted", numFound);
                    break;
                case CascadeSoftDelWhatDoing.ResetSoftDelete:
                    status.Message = FormMessage("recovered", numFound);
                    break;
                case CascadeSoftDelWhatDoing.CheckWhatWillDelete:
                    status.Message = numFound == 0
                        ? "No entries will be hard deleted"
                        : $"Are you sure you want to hard delete this entity{DependentsSuffix(numFound)}";
                    break;
                case CascadeSoftDelWhatDoing.HardDeleteSoftDeleted:
                    status.Message = FormMessage("hard deleted", numFound);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return status;
        }

        private string FormMessage(string what, int numFound)
        {
            if (numFound == 0)
                return $"No entries have been {what}";
            var dependentsSuffix = numFound > 1
                ? $" and its {numFound - 1} dependents"
                : "";
            return $"You have {what} an entity{DependentsSuffix(numFound)}";
        }

        private string DependentsSuffix(int numFound)
        {
            return numFound > 1
                ? $" and its {numFound - 1} dependents"
                : "";
        }
    }
}