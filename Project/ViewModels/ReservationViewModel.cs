using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.ViewModels
{
    public class ReservationViewModel
    {
        public int TransactionId { get; set; }
        public bool IsReserved { get; set; }
        public string Notes { get; set; }
    }
}
