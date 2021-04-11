using System;
using System.Collections.Generic;

#nullable disable

namespace Project.Models.DB
{
    public partial class Product
    {
        public Product()
        {
            OrderDetail = new HashSet<OrderDetail>();
            Review = new HashSet<Review>();
        }
        public string ProductId { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string ImageFileName { get; set; }
        public double? UnitCost { get; set; }
        public string Description { get; set; }
        public bool? IsDownload { get; set; }
        public string DownloadFileName { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<OrderDetail> OrderDetail { get; set; }
        public virtual ICollection<Review> Review { get; set; }
    }
}
