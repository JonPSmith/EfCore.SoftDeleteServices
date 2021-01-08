// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class QuotePrice : ICascadeSoftDelete, IUserId
    {
        public int Id { get; set; }

        public int QuoteId { get; set; }

        public int Price { get; set; }

        public byte SoftDeleteLevel { get; set; }
        public Guid UserId { get; set; }
    }
}