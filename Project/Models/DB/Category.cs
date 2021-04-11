using System;
using System.Collections.Generic;

#nullable disable

namespace Project.Models.DB
{
    public partial class Category
    {
        public Category()
        {
            Product = new HashSet<Product>();
        }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public virtual ICollection<Product> Product { get; set; }
    }
}
