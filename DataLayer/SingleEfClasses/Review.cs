﻿// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace DataLayer.SingleEfClasses
{
    public class Review
    {
        public int Id { get; set; }
        public int NumStars { get; set; }
        public int BookId { get; set; }
    }
}