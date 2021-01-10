// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class LineItem : ICascadeSoftDelete
    {
        public int Id { get; set; }
        
        public int LineNum { get; set; }

        public string ProductSku { get; set; }

        public int NumProduct { get; set; }

        public byte SoftDeleteLevel { get; set; }
        
        //-----------------------------------
        //Relationships
        
        public Quote QuoteRef { get; set; }
        public int QuoteId { get; set; }

    }
}