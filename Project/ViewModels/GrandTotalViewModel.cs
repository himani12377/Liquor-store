using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Project.ViewModels
{
    public class GrandTotalViewModel
    {
        [Key]
        public decimal GrandTotal { get; set; }
    }
}
