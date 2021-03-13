// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using DataLayer.Interfaces;

namespace DataLayer.SingleEfClasses
{
    public class Order : ISingleSoftDelete, IUserId
    {
        public int Id { get; set; }
        public string OrderRef { get; set; }

        public bool SoftDeleted { get; set; }
        public Guid UserId { get; set; }

        public int? AddressId { get; set; }
        public Address UserAddress { get; set; }
    }
}