# Release Notes

## 8.0.0

- Only supports .NET 8 - This makes it easier to update for future NET releases

## 4.0.0

Update to NET 8 - this version supports .NET 6, 7 and 8

## 4.0.0-rc1-0001

- First build using NET 8-rc1 - supports .NET 6, 7 and 8

## 3.1.0

- Supports .Net 6 and .Net 7

## 3.0.0

- Changed target framework to netstandard2.1 to work with any version of .NET

## 2.0.2 

- Bug fix: Fixes Owned Entities causing a failure - see issue #7

## 2.0.1

- Bug fix: Cascade delete would fail to soft delete a relationship in certain circumstances - see issue #11

## 2.0.0

- Updated to work with both EF Core 5 and EF Core 6

## 2.0.0-preview001

- Updated to work with Net6-rc.2 preview

## 1.1.3

- Bug Fix - Handling one-to-one with foreign key in primary entity - see issue #1

## 1.1.2

- Bug Fix - better handling of exceptions in sync methods (added better stacktrace)

## 1.1.1

- Bug Fix - better handling of exceptions in sync methods

## 1.1.0

- New feature: Can now use shadow properties to hold the soft delete value

## 1.0.1

- Added test to throw exception if no soft delete configuration not found.

## 1.0.0

- First version of the library



