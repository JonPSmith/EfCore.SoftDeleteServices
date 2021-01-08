// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class Quote : ICascadeSoftDelete, IUserId
    {
        public int Id { get; set; }

        public string Name { get; set; }

        //-------------------------------------------------
        // relationships

        public int CompanyId { get; set; }
        public Company BelongsTo { get; set; }

        public QuotePrice PriceInfo { get; set; }

        public byte SoftDeleteLevel { get; set; }
        public Guid UserId { get; set; }
    }
}