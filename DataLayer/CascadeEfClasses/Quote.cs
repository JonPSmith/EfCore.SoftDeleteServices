﻿// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class Quote : ICascadeSoftDelete, IUserId
    {
        public int Id { get; set; }

        public string Name { get; set; }

        //-------------------------------------------------
        // relationships

        public int CustomerId { get; set; }
        public Customer BelongsTo { get; set; }

        public QuotePrice PriceInfo { get; set; }
        
        public ICollection<LineItem> LineItems { get; set; }

        //Soft delete/UserId parts

        public byte SoftDeleteLevel { get; set; }
        public Guid UserId { get; set; }
    }
}