// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class Company : ICascadeSoftDelete, IUserId
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }

        public HashSet<Quote> Quotes { get; set; }

        public byte SoftDeleteLevel { get; set; }
        public Guid UserId { get; set; }

        public static Company SeedCompanyWithQuotes(CascadeSoftDelDbContext context, Guid userId, string companyName = "Company1")
        {
            var company = new Company
            {
                CompanyName = companyName,
                UserId = userId,
                Quotes = new HashSet<Quote>()
            };
            for (int i = 0; i < 4; i++)
            {
                company.Quotes.Add(
                    new Quote {Name = $"quote{i}", UserId = userId, PriceInfo = new QuotePrice {UserId = userId, Price = i}}
                );
            }

            context.Add(company);
            context.SaveChanges();

            return company;
        }
    }
}