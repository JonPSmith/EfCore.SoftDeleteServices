// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class ShadowCascadeDelClass : IShadowCascadeSoftDelete
    {
        public int Id { get; set; }
    }
}