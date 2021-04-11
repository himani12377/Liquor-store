using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project.Models.DB;
using Newtonsoft.Json;

namespace Project.Controllers
{
    public class ProductsNgController : Controller
    {
        private readonly ProjectContext _context;

        public ProductsNgController(ProjectContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Method GetProducts outputs JSON to support client app UIs like HTML-AngularJS and Adobe Flash
        // GET: Products/GetProducts
        // This demo app is setup so that AngularJS replaces the code in Products/Index
        // With Angular.JS we need to load the page first, see method above
        // then give it a separate method to call by Ajax.

        //Test string
        //GET: /ProductsNg/GetProducts
        public string GetProducts()
        {
            string sql = "SELECT * FROM Product";
            var products = _context.Product.FromSqlRaw(sql).ToList();
            string json = JsonConvert.SerializeObject(products);
            return json;
        }

        //Test string
        //GET: /ProductsNg/PutProduct?productId=1MOR4ME&unitCost=21.63
        public string PutProduct(string productId, double unitCost)
        {
            string sql = "UPDATE Product SET UnitCost = @p0 WHERE ProductId = @p1";
            int rowsChanged = _context.Database.ExecuteSqlRaw(sql, unitCost, productId);
            return rowsChanged.ToString();
        }
    }
}
