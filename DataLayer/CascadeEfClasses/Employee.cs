// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;

namespace DataLayer.CascadeEfClasses
{
    public class Employee : ICascadeSoftDelete
    {
        private Employee() {}

        public Employee(string name, Employee manager, EmployeeContract contact)
        {
            Name = name;
            Manager = manager;
            Contract = contact;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public byte SoftDeleteLevel { get; set; }

        //----------------------------------------
        //relationships

        public int? ManagerId { get; set; }
        public Employee Manager { get; set; }

        public ICollection<Employee> WorksFromMe { get; set; } 

        public EmployeeContract Contract { get; set; }

        public override string ToString()
        {
            return $"Name: {Name} - has {WorksFromMe?.Count ?? 0} staff, Contract = {Contract?.ContractText ?? "-none-"} SoftDeleteLevel: {SoftDeleteLevel}";
        }


        //---------------------------------------------------
        //static unit test helper

        public static void ShowHierarchical(Employee employee, Action<string> output, bool nameOnly = true, int indent = 0, HashSet<Employee> stopCircularRef = null)
        {
            stopCircularRef ??= new HashSet<Employee>();
            if (stopCircularRef.Contains(employee))
            {
                output($"Circular ref back to {employee.Name}");
                return;
            }

            stopCircularRef.Add(employee);

            const int indentSize = 2;
            output(new string(' ', indent * indentSize) + (nameOnly ? employee.Name : employee.ToString()));
            foreach (var person in employee.WorksFromMe ?? new List<Employee>())
            {
                ShowHierarchical(person, output, nameOnly, indent + 1, stopCircularRef);
            }
        }

        public static Employee SeedEmployeeSoftDel(CascadeSoftDelDbContext context)
        {
            var ceo = new Employee("CEO", null, null);
            //development
            var cto = new Employee("CTO", ceo, new EmployeeContract{ContractText = "$$$"});
            var pm1 = new Employee("ProjectManager1", cto, new EmployeeContract { ContractText = "$$" });
            var dev1a = new Employee("dev1a", pm1, new EmployeeContract { ContractText = "$" });
            var dev1b = new Employee("dev1b", pm1, new EmployeeContract { ContractText = "$" });
            var pm2 = new Employee("ProjectManager2", cto, new EmployeeContract { ContractText = "$$" });
            var dev2a = new Employee("dev2a", pm2, null);
            var dev2b = new Employee("dev2b", pm2, new EmployeeContract { ContractText = "$" });
            //sales
            var salesDir = new Employee("SalesDir", ceo, new EmployeeContract { ContractText = "$$$" });
            var sales1 = new Employee("sales1", salesDir, new EmployeeContract { ContractText = "$$" });
            var sales2 = new Employee("sales2", salesDir, new EmployeeContract { ContractText = "$$" });

            context.AddRange(ceo, cto, pm1, pm2, dev1a, dev1b, dev2a, dev2b, salesDir, sales1, sales2);
            context.SaveChanges();

            return ceo;
        }
    }
}