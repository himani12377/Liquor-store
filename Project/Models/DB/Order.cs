using System;
using System.Collections.Generic;

#nullable disable

namespace Project.Models.DB
{
    public partial class Order
    {
        public Order()
        {
            OrderDetail = new HashSet<OrderDetail>();
        }
        public int OrderId { get; set; }
        public string Username { get; set; }
        public string SessionId { get; set; }
        public int? OrderStatusId { get; set; }
        public DateTime? DateOfSession { get; set; }
        public DateTime? DateOfOrder { get; set; }
        public DateTime? DateOfShipping { get; set; }
        public int? TransactionId { get; set; }
        public string Notes { get; set; }
        public string CustomerName { get; set; }
        public string DeliveryAddress { get; set; }
        public string Phone { get; set; }
        public string EmailAddress { get; set; }
        public string CardOwner { get; set; }
        public string CardType { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public virtual OrderStatus OrderStatus { get; set; }
        public virtual ICollection<OrderDetail> OrderDetail { get; set; }
    }
}
