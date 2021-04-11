using System;
using System.Collections.Generic;

#nullable disable

namespace Project.Models.DB
{
    public partial class OrderStatus
    {
        public OrderStatus()
        {
            Order = new HashSet<Order>();
        }

        public int OrderStatusId { get; set; }
        public string Description { get; set; }
        public virtual ICollection<Order> Order { get; set; }
    }
}
