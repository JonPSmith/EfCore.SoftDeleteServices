# EfCore.SoftDeleteServices

This repo contains code to handle soft delete in a generic way. The code handles all the setting, resetting and finding of soft deleted entities. This code also incorporates other Query filters, such as multi-tenant keys, to make sure you only soft delete/reset entities that you have access to.

I have written two services.  

- **Single soft delete**: where a single entity class can be hidden from normal queries and restore back if required.
- **Cascade soft delete**: where when an entity is soft deleted, then its dependent entity classes are also soft deleted.

*The cascade soft delete is pretty clever, and can handle multi-level soft deletes - see [this sction](https://www.thereformedprogrammer.net/ef-core-in-depth-soft-deleting-data-with-global-query-filters/#building-solution-3-cascade-softdeleteservice) from my article [EF Core In depth – Soft deleting data with Global Query Filters](https://www.thereformedprogrammer.net/ef-core-in-depth-soft-deleting-data-with-global-query-filters/).*

## Limitations

- When loading via keys it assumes the primary key property(s) are properties.
- The navigational links have to be properties.
- Currently the soft delete property can't be a shadow property.

All of these limitattions could be removed, but it takes time to implement and check.

## General information on how the simple and cascade methods work

There four basic things your can do with both the single and cascade libraies 
1. Set the entity's soft deleted property to hidden, i.e. the entity won't be seen a normal query. 
2. Reset the entity's soft delete property to not soft deleted, i.e. the entity is  seen in a normal query.
3. Hard delete any entity(s) that have are already soft deleted (useful protection against hard delete being applied by accident).
3. Find all the soft deleted items that are soft deleted and can be reset - useful for showing a user the soft deleted 

I lot of things are configuable in the `SoftDeleteConfiguration` class. Its designed to work with your own data layer and interfaces. See the `Test` project for examples of how to use it. 


## Terms

- **Hard delete** is when you delete a row in the database, via the EF Core `Remove` method. A hard delete removes the row from the database and may effect other entities/rows.
- **Single soft delete** mimics a one row, hard delete. The entity/row is still in the database, but won't show up in EF Core query. But you can un-soft delete, referred to as a soft delete **reset**, and the makes the entity visible in an EF Core query.
- **Cascade soft delete** mimics the hard delete's cascade approach and will soft delete any dependant relationships (the EF Core `DeleteBehavior` has an effect on what happens).
- **Soft delete** covers both Single soft delete and Cascade soft delete

