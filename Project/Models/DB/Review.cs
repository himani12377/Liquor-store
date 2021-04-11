using System;
using System.Collections.Generic;

#nullable disable

namespace Project.Models.DB
{
    public partial class Review
    {
        public int ReviewId { get; set; }
        public string ProductId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public int? Rating { get; set; }
        public string Comments { get; set; }
        public virtual Product Product { get; set; }
    }
}
