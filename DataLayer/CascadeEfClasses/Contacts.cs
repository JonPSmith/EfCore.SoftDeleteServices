// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace DataLayer.CascadeEfClasses
{
    [Owned]
    public class Contacts
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}