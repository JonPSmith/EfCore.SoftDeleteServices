// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataLayer.Interfaces;

namespace DataLayer.SingleEfClasses
{
    public class BookDDD : ISingleSoftDeletedDDD
    {
        public BookDDD(string title)
        {
            Title = title;
            _reviews = new HashSet<Review>();
        }

        public int Id { get; private set; }
        public string Title { get; private set; }
        public bool SoftDeleted { get; private set; }

        public void ChangeSoftDeleted(bool softDeleted)
        {
            SoftDeleted = softDeleted;
        }

        public void AddReview(Review review)
        {
            if (_reviews == null)
                throw new NullReferenceException("You must include the reviews");
            _reviews.Add(review);
        }


        private HashSet<Review> _reviews;
        public IReadOnlyCollection<Review> Reviews => _reviews.ToList().AsReadOnly();
    }
}