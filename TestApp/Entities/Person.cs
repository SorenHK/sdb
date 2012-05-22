using System;
using System.Collections.Generic;

namespace TestApp.Entities
{
    public class Person
    {
        public virtual string Name { get; set; }
        public virtual string Address { get; set; }
        public virtual Gender Gender { get; set; }
        public virtual DateTime BirthDate { get; set; }
        public virtual IList<Pet> Pets { get; set; }
    }
}
