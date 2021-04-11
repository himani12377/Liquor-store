using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Project.ViewModels
{
    public class CatalogViewModel
    {
        [Key]
        public string ProductId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string ImageFileName { get; set; }
        public double UnitCost { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
    }
}
