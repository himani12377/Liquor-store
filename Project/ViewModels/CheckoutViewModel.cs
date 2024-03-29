﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Project.ViewModels
{
    public class CheckoutViewModel
    {
        public string CustomerName { get; set; }
        public string AddressStreet { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public string PostCode { get; set; }
        public string CardOwner { get; set; }
        public string CardType { get; set; }
        public string CardNumber { get; set; }
        public string CSC { get; set; }

        //2021-Q1 added to display GrandTotal on Checkout page
        public decimal GrandTotal { get; set; }
    }
}
