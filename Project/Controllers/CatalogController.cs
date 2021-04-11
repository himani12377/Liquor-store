

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project.Models.DB;
using Microsoft.AspNetCore.Http;
using MVCManukauTech.Helper;

namespace Project.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ProjectContext _context;

        public CatalogController(ProjectContext context)
        {
            _context = context;
        }

        // GET: Catalog?CategoryName=Travel
        public async Task<IActionResult> Index()
        {
            //2021-Q1 add CategoryName to SELECT list of fields
            string SQL = "SELECT ProductId, Product.CategoryId AS CategoryId, Name, ImageFileName, UnitCost"
                + ", SUBSTRING(Description, 1, 100) + '...' AS Description, CategoryName "
                + "FROM Product INNER JOIN Category ON Product.CategoryId = Category.CategoryId ";
            string categoryName = Request.Query["CategoryName"];

            if (categoryName != null)
            {
                //2021-Q1 security check - if ProductId is dodgy then return bad request and log the fact 
                //  of a possible hacker attack.  Excessive length or containing possible control characters
                //  are cause for concern!  TODO move this into a separate reusable code method with more sophistication.
                if (categoryName.Length > 20 || categoryName.IndexOf("'") > -1 || categoryName.IndexOf("#") > -1)
                {
                    //TODO Code to log this event and send alert email to admin
                    return BadRequest(); // Http status code 400
                }

                //2021-Q1 Passed the above test so extend SQL
                //2021-Q1 Security improvement @p0
                SQL += " WHERE CategoryName = @p0";
                //SQL += " WHERE CategoryName = '{0}'";
                //SQL = String.Format(SQL, CategoryName);
                //Send extra info to the view that this is the selected CategoryName
                ViewBag.CategoryName = categoryName;
            }

            int intPage = 1;
            String strPage = Request.Query["pageNumber"];

            if (strPage != null)
            {
                int.TryParse(strPage, out intPage);
            }

            int pageSize = 6;
            var products = _context.CatalogViewModel.FromSqlRaw(SQL, categoryName);
            var x = await PaginatedList<ViewModels.CatalogViewModel>.CreateAsync(products.AsNoTracking(), intPage, pageSize);
            return View(x);
            
            
            //2021-Q1 Security improvement implementation of @p0
            //var products = _context.CatalogViewModel.FromSqlRaw(SQL, categoryName);
            //return View(products.ToList());
        }

        // GET: Catalog/Details?ProductId=1MORE4ME
        public IActionResult Details(string ProductId)
        {
            if (ProductId == null)
            {
                return BadRequest(); // Http status code 400
            }
            //2021-Q1 security check - if ProductId is dodgy then return bad request and log the fact 
            //  of a possible hacker attack.  Excessive length or containing possible control characters
            //  are cause for concern!  TODO move this into a separate reusable code method with more sophistication.
            if (ProductId.Length > 20 || ProductId.IndexOf("'") > -1 || ProductId.IndexOf("#") > -1)
            {
                //TODO Code to log this event and send alert email to admin
                return BadRequest(); // Http status code 400
            }

            //2021-Q1 Security improvement implementation of @p0
            //2021-Q1 change to query based on class CatalogViewModel
            //  Change above code to give all of the description rather than summarising with the first 100 characters
            string SQL = "SELECT ProductId, Product.CategoryId AS CategoryId, Name, ImageFileName, UnitCost"
            + ", Description, CategoryName "
            + "FROM Product INNER JOIN Category ON Product.CategoryId = Category.CategoryId "
            + " WHERE ProductId = @p0";

            //2021-Q1 case of one product to look at the details.
            //  SQL gives some kind of collection where we need to clean that up with ToList() then take element [0]
            //2021-Q11 Security improvement implementation of @p0 substitute ProductId
            var product = _context.CatalogViewModel.FromSqlRaw(SQL, ProductId).ToList()[0];
            if (product == null)
            {
                return NotFound(); //Http status code 404
            }
            return View(product);
        }

    }
}
