// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class Customer : ICascadeSoftDelete, IUserId
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }

        public int? CustomerInfoId { get; set; }
        public CustomerInfo MoreInfo { get; set; }

        public HashSet<Quote> Quotes { get; set; }

        public byte SoftDeleteLevel { get; set; }
        public Guid UserId { get; set; }

        public static Customer SeedCustomerWithQuotes(CascadeSoftDelDbContext context, Guid userId, string companyName = "Company1")
        {
            var customer = new Customer
            {
                CompanyName = companyName,
                UserId = userId,
                Quotes = new HashSet<Quote>()
            };
            for (int i = 0; i < 4; i++)
            {
                customer.Quotes.Add(
                    new Quote {
                        Name = $"quote{i}", UserId = userId, 
                        PriceInfo = new QuotePrice {UserId = userId, Price = i},
                        LineItems = new List<LineItem>
                        {
                            new LineItem{LineNum = 1, ProductSku = "Door", NumProduct = 2},
                            new LineItem{LineNum = 2, ProductSku = "Window", NumProduct = 6},
                            new LineItem{LineNum = 3, ProductSku = "Wall", NumProduct = 4},
                            new LineItem{LineNum = 3, ProductSku = "Roof", NumProduct = 1},
                        }
                    }
                );
            }

            context.Add(customer);
            context.SaveChanges();

            return customer;
        }
    }
}