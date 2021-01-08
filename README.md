# EfCore.SoftDeleteServices

This library to provide simple soft delete and cascade soft delete in EF Core. It provides:

- **Useful methods:** The features in this library are:
  - Set the SoftDeleted flag on an entity class, with checks.
  - Provides a secure query to find all the Soft Deleted entities for a specific entity class.
  - Reset the SoftDeleted flag on an entity class, which checks.
  - Hard delete (i.e. call EF Core `Remove` method) a entity class, but only if it is already Soft Deleted.  
*NOTE: Methods can work with entity instance, or found via primary keys. Also has sync and async versions of all methods.*
- **Cascade Soft Delete:** This library has a service that can mimic the database cascade delete, but Soft Deleting the entities. For instance, Cascade Soft Deleting a Company could also soft delete dependent relationships.
- **Keeps your data secure:** This library can handle Query Filters that contain multiple parts to the filter, e.g. Soft Delete with a multi-tenant filter. It builds queries that will replace the other filters so that your data stays secure.
- **Fully configurable:** It works with your properties and interfaces. The only rule it has is your Soft Delete property must be of type `bool`, or for the cascade delete it must be of type `byte`.
- **DI-friendly:** This library is designed to work with dependency injection (DI) and contains a method which will scan for your Soft Delete configuration files and set up all the services you need to use this library.

*The cascade soft delete is pretty clever, and can handle multi-level soft deletes - see [this section](https://www.thereformedprogrammer.net/ef-core-in-depth-soft-deleting-data-with-global-query-filters/#building-solution-3-cascade-softdeleteservice) from my article [EF Core In depth - Soft deleting data with Global Query Filters](https://www.thereformedprogrammer.net/ef-core-in-depth-soft-deleting-data-with-global-query-filters/).*

MIT License.

## Documentation

Coming soon!

## Limitations

- When loading via keys it assumes the primary key property(s) are properties.
- The navigational links have to be properties.
- Currently the soft delete property can't be a shadow property.

All of these limitations could be removed, but it takes time to implement and check.
## Terms

- **Hard delete** is when you delete a row in the database, via the EF Core `Remove` method. A hard delete removes the row from the database and may effect other entities/rows.
- **Single soft delete** mimics a one row, hard delete. The entity/row is still in the database, but won't show up in EF Core query. But you can un-soft delete, referred to as a soft delete **reset**, and the makes the entity visible in an EF Core query.
- **Cascade soft delete** mimics the hard delete's cascade approach and will soft delete any dependant relationships (the EF Core `DeleteBehavior` has an effect on what happens).
- **Soft delete** covers both Single soft delete and Cascade soft delete

