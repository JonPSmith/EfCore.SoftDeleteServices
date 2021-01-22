// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SoftDeleteServices.Configuration;

namespace SoftDeleteServices.Concrete.Internal
{
    internal class CascadeWalker<TInterface>
        where TInterface : class
    {
        private readonly DbContext _context;
        private readonly CascadeSoftDeleteConfiguration<TInterface> _config;
        private readonly bool _isAsync;
        private readonly CascadeSoftDelWhatDoing _whatDoing;
        private readonly bool _readEveryTime;

        private readonly HashSet<object> _stopCircularLook = new HashSet<object>();

        public int NumFound { get; private set; }

        public CascadeWalker(DbContext context, CascadeSoftDeleteConfiguration<TInterface> config,
            bool isAsync,
            CascadeSoftDelWhatDoing whatDoing, bool readEveryTime)
        {
            _context = context;
            _config = config;
            _isAsync = isAsync;
            _whatDoing = whatDoing;
            _readEveryTime = readEveryTime && whatDoing == CascadeSoftDelWhatDoing.SoftDelete;
        }

        public async ValueTask WalkEntitiesSoftDelete(object principalInstance, byte cascadeLevel)
        {
            if (!(principalInstance is TInterface castToCascadeSoftDelete && principalInstance.GetType().IsClass) || _stopCircularLook.Contains(principalInstance))
                return; //isn't something we need to consider, or we saw it before, so it returns 

            _stopCircularLook.Add(principalInstance);  //we keep a reference to this to stop the method going in a circular loop

            if (ApplyChangeIfAppropriate(castToCascadeSoftDelete, cascadeLevel))
                //If the entity shouldn't be changed then we leave this entity and any of it children
                return;

            var principalNavs = _context.Entry(principalInstance)
                .Metadata.GetNavigations()
                .Where(x => !x.IsOnDependent && //navigational link goes to dependent entity(s)
                                                //The developer has whatDoing a Cascade delete behaviour (two options) on this link
                            (x.ForeignKey.DeleteBehavior == DeleteBehavior.ClientCascade || x.ForeignKey.DeleteBehavior == DeleteBehavior.Cascade))
                .ToList();

            foreach (var navigation in principalNavs)
            {
                if (navigation.PropertyInfo == null)
                    //This could be changed by enhancing the navigation.PropertyInfo.GetValue(principalInstance);
                    throw new NotImplementedException("Currently only works with navigation links that are properties");

                //It loads the current navigational value so that we can limit the number of database selects if the data is already loaded
                var navValue = navigation.PropertyInfo.GetValue(principalInstance);
                if (navigation.IsCollection)
                {
                    if (_readEveryTime || navValue == null)
                    {
                        var navValueTask = LoadNavigationCollection(principalInstance, navigation, cascadeLevel);
                        if (_isAsync)
                            navValue = await navValueTask;
                        else
                        {                            
                            typeof(IEnumerable).CheckSyncValueTaskWorkedDynamic(navValueTask);
                            navValue = navValueTask.Result;
                        }
                    }
                    if (navValue == null)
                        return; //no relationship
                    foreach (var entity in navValue as IEnumerable)
                    {
                        var walkValueTask = WalkEntitiesSoftDelete(entity, (byte)(cascadeLevel + 1));
                        if (_isAsync)
                            await walkValueTask;
                        else
                            walkValueTask.CheckSyncValueTaskWorked();
                    }
                }
                else
                {
                    if (_readEveryTime || navValue == null)
                    {
                        var navValueTask = LoadNavigationSingleton(principalInstance, navigation, cascadeLevel);
                        if (_isAsync)
                            navValue = await navValueTask;
                        else
                        {
                            navValueTask.CheckSyncValueTaskWorked();
                            navValue = navValueTask.Result;
                        }
                    }
                    if (navValue == null)
                        return; //no relationship
                    var walkValueTask = WalkEntitiesSoftDelete(navValue, (byte)(cascadeLevel + 1));
                    if (_isAsync)
                        await walkValueTask;
                    else
                        walkValueTask.CheckSyncValueTaskWorked();
                }
            }
        }

        /// <summary>
        /// This checks if something has to be done for this entity
        /// If it should not be changed it returns true, which says don't go any deeper from this entity
        /// If it should be changed then it does the change and returns false
        /// </summary>
        /// <param name="castToCascadeSoftDelete"></param>
        /// <param name="cascadeLevel"></param>
        /// <returns></returns>
        private bool ApplyChangeIfAppropriate(TInterface castToCascadeSoftDelete, byte cascadeLevel)
        {
            switch (_whatDoing)
            {
                case CascadeSoftDelWhatDoing.SoftDelete:
                    if (_config.GetSoftDeleteValue.Compile().Invoke(castToCascadeSoftDelete) != 0)
                        //If the entity has already been soft deleted, then we don't change it, nor its child relationships
                        return true;
                    _config.SetSoftDeleteValue(castToCascadeSoftDelete, cascadeLevel);
                    break;
                case CascadeSoftDelWhatDoing.ResetSoftDelete:
                    if (_config.GetSoftDeleteValue.Compile().Invoke(castToCascadeSoftDelete) != cascadeLevel)
                        //Don't reset if it was soft deleted value doesn't match - this stops previously deleted sub-groups being undeleted
                        return true;
                    _config.SetSoftDeleteValue(castToCascadeSoftDelete, 0);
                    break;
                case CascadeSoftDelWhatDoing.CheckWhatWillDelete:
                    if (_config.GetSoftDeleteValue.Compile().Invoke(castToCascadeSoftDelete) == 0)
                        return true;
                    break;
                case CascadeSoftDelWhatDoing.HardDeleteSoftDeleted:
                    if (_config.GetSoftDeleteValue.Compile().Invoke(castToCascadeSoftDelete) == 0)
                        return true;
                    _context.Remove(castToCascadeSoftDelete);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            NumFound++;

            return false;
        }

        private async ValueTask<IEnumerable> LoadNavigationCollection(object principalInstance, INavigation navigation, 
            byte cascadeLevel)
        {
            byte levelToLookFor = _whatDoing == CascadeSoftDelWhatDoing.SoftDelete
                ? (byte)0                     //if soft deleting then look for un-deleted entries
                : (byte)(cascadeLevel + 1);   //otherwise look for next level up

            var navValueType = navigation.PropertyInfo.PropertyType;
            var innerType = navValueType.GetGenericArguments().Single();
            var genericHelperType =
                typeof(GenericCollectionLoader<>).MakeGenericType(typeof(TInterface), innerType);

            dynamic loader = Activator.CreateInstance(genericHelperType, _context, _config, _isAsync,
                principalInstance, navigation.PropertyInfo, levelToLookFor);
            var navValueTask = loader.GetFilteredEntities();
            if (_isAsync)
                return await navValueTask;

            ValueTaskSyncCheckers.CheckSyncValueTaskWorkedDynamic(typeof(IEnumerable), navValueTask);
 
            return navValueTask.Result;
        }

        private class GenericCollectionLoader<TEntity> where TEntity : class, TInterface
        {
            private readonly bool _isAsync;
            private readonly IQueryable<TEntity> _queryOfFilteredEntities;

            public async ValueTask<IEnumerable> GetFilteredEntities()
            {
                return _isAsync
                    ? await _queryOfFilteredEntities.ToListAsync()
                    : _queryOfFilteredEntities.ToList();
            }

            public GenericCollectionLoader(DbContext context, CascadeSoftDeleteConfiguration<TInterface> config, bool isAsync,
                object principalInstance, PropertyInfo propertyInfo, byte levelToLookFor)
            {
                _isAsync = isAsync;
                var query = context.Entry(principalInstance).Collection(propertyInfo.Name).Query();
                _queryOfFilteredEntities = _queryOfFilteredEntities = query.Provider.CreateQuery<TEntity>(query.Expression).IgnoreQueryFilters()
                    .Where(config.FilterToGetValueCascadeSoftDeletedEntities<TEntity, TInterface>(levelToLookFor));
            }
        }

        private async ValueTask<object> LoadNavigationSingleton(object principalInstance, INavigation navigation, byte cascadeLevel)
        {
            byte levelToLookFor = _whatDoing == CascadeSoftDelWhatDoing.SoftDelete
                ? (byte)0                     //if soft deleting then look for un-deleted entries
                : (byte)(cascadeLevel + 1);   //otherwise look for next level up

            //for everything else we need to load the singleton with a IgnoreQueryFilters method
            var navValueType = navigation.PropertyInfo.PropertyType;
            var genericHelperType =
                typeof(GenericSingletonLoader<>).MakeGenericType(typeof(TInterface), navValueType);

            dynamic loader = Activator.CreateInstance(genericHelperType, _context, _config, _isAsync,
                principalInstance, navigation.PropertyInfo, levelToLookFor);

            var navValueTask = loader.GetFilteredSingleton();
            if (_isAsync)
                return await navValueTask;

            ValueTaskSyncCheckers.CheckSyncValueTaskWorkedDynamic(typeof(object), navValueTask);
            //navValueTask.CheckSyncValueTaskWorkedDynamic(navValueTask);
            return navValueTask.Result;
        }

        private class GenericSingletonLoader<TEntity> where TEntity : class, TInterface
        {
            private readonly bool _isAsync;
            private readonly IQueryable<TEntity> _queryOfFilteredSingle;

            public async ValueTask<object> GetFilteredSingleton()
            {
                return _isAsync
                    ? await _queryOfFilteredSingle.SingleOrDefaultAsync()
                    : _queryOfFilteredSingle.SingleOrDefault();
            }

            public GenericSingletonLoader(DbContext context, CascadeSoftDeleteConfiguration<TInterface> config, bool isAsync,
                object principalInstance, PropertyInfo propertyInfo, byte levelToLookFor)
            {
                _isAsync = isAsync;
                var query = context.Entry(principalInstance).Reference(propertyInfo.Name).Query();
                _queryOfFilteredSingle = query.Provider.CreateQuery<TEntity>(query.Expression).IgnoreQueryFilters()
                    .Where(config.FilterToGetValueCascadeSoftDeletedEntities<TEntity, TInterface>(levelToLookFor));
            }
        }

    }
}