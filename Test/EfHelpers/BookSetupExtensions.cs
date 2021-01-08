// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using DataLayer.SingleEfClasses;
using DataLayer.SingleEfCode;

namespace Test.EfHelpers
{
    public static class BookSetupExtensions
    {
        public static Book AddBookWithReviewToDb(this SingleSoftDelDbContext context, string title = "test")
        {
            var book = new Book
                { Title = title, Reviews = new List<Review> { new Review { NumStars = 1 } } };
            context.Add(book);
            context.SaveChanges();
            return book;
        }
    }
}