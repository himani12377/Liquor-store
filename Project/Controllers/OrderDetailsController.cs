using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.DB;
using Project.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PayPal.Api;
using demoPaypal;

namespace MVCManukauTech.Controllers
{
    public class OrderDetailsController : Controller
    {
        private readonly ProjectContext _context;
        private readonly string _bankOfFictionURL;
        private readonly string _merchantId;
        private readonly string _merchantPassword;
        public static List<Item> AllitemList = new List<Item>();
        public static string SubTotal = "0";

        public OrderDetailsController(ProjectContext context, IConfiguration configuration)
        {
            _context = context;
            //2019-09-18 JPC Version 13.1 store Bank Of Fiction URL and credentials (oversimplification!) in appsettings.json
            _bankOfFictionURL = configuration.GetSection("AppSettings")["BankOfFictionURL"];
            _merchantId = configuration.GetSection("AppSettings")["MerchantId"];
            _merchantPassword = configuration.GetSection("AppSettings")["MerchantPassword"];
        }

        //2014-08-25 started JPC John Calder - adapted from XSpy version 1 webforms 2004-2008



        public ActionResult FailureView()
        {
            return View();
        }
        public ActionResult SuccessView(string guid = null, string paymentId = null, string token = null, string PayerID = null)
        {
            ViewBag.guid = guid;
            ViewBag.paymentId = paymentId;
            ViewBag.token = token;
            ViewBag.PayerID = PayerID;

            return View();
        }


        // GET: OrderDetails/ShoppingCart?ProductId=1MOR4ME
        // or to simply view the cart
        // GET: OrderDetails/ShoppingCart
        public IActionResult ShoppingCart(bool IsPaypal = false)
        {
            string SQLGetOrder = "";
            string SQLStartOrder = "";
            string SQLCart = "";
            string SQLBuy = "";
            string SQLUnitCostLookup = "";
            int rowsChanged = 0;

            if (User.IsInRole("FullMember"))
            {
                //Code for action for a Full Member
            }
            else if (User.IsInRole("Associate"))
            {
                //Code for action for an Associate Member
            }


            string ProductId = Request.Query["ProductId"];
            // Security checking
            if (ProductId != null && (ProductId.Length > 20 || ProductId.IndexOf("'") > -1 || ProductId.IndexOf("#") > -1))
            {
                //
                return BadRequest();
            }

            //Have we created an order for this user yet?
            //If not then create a placeholder for a mostly empty order
            //Note that any Order in progress has an OrderStatusId of 0 or 1
            //We are not interested in Orders with higher status because they have already gone through checkout
            //2015-08-07 JPC Security improvement implementation of @p0
            //2019-03-19 JPC Change of table name from Orders to Order causes keyword clash 
            //  and therefore need to wrap Order in square brackets!
            SQLGetOrder = "SELECT * FROM [Order] WHERE SessionId = @p0 AND OrderStatusId <= 1;";

            //140825 JPC.  We may need 2 attempts at reading the order out of the database.
            //  Managing this as a for..loop with 2 loops.  If successful first time then break out.
            //  (Other opinion Rosemarie T - "this is a bit dodgy!")
            //150807 JPC Security improvement implementation of @p0
            var orders = _context.Order.FromSqlRaw(SQLGetOrder, HttpContext.Session.Id).ToList();
            for (int iForLoop = 0; iForLoop <= 1; iForLoop++)
            {
                //Do we have an order?
                if (orders.Count == 1)
                {
                    //we have an order, we can continue to the next step
                    break;
                }
                else if (iForLoop == 1)
                {
                    //failed on the second attempt
                    throw new Exception("ERROR with database table 'Order'.  This session fails to write a new order.");
                }
                else if (orders.Count > 1)
                {
                    //This would be a major error. In theory impossible but we need to be ready for any outcome
                    throw new Exception("ERROR with database table 'Order'.  This session is running more than one shopping cart.");
                }
                else
                {
                    //else we get an order started
                    //150807 JPC Security improvement implementation of @p0
                    SQLStartOrder = "INSERT INTO [Order](SessionId, OrderStatusId) VALUES(@p0, 0);";
                    rowsChanged = _context.Database.ExecuteSqlRaw(SQLStartOrder, HttpContext.Session.Id);
                    // a good result would be one row changed
                    if (rowsChanged != 1)
                    {
                        //Error handling code to go in here.  Poss return a view with error messages.
                        //Code from our old webforms version is -- 
                        throw new Exception("ERROR with database table 'Order'.");
                    }
                    //ready to try reading that order again
                    //150807 JPC Security improvement implementation of @p0, parameter Session.SessionID
                    orders = _context.Order.FromSqlRaw(SQLGetOrder, HttpContext.Session.Id).ToList();
                    //go round and test orders again
                }
            }

            //What is the OrderId
            int orderId = orders[0].OrderId;

            //040514 JPC EDUC add ORDER BY clause
            //080704 JPC Note that with use of SQL Server, "LineNo" is a reserved word!
            //  Therefore change to "LineNumber"
            //150807 JPC Security improvement implementation of @p0
            //20180313 JPC temp drop parameter because of problems
            SQLCart = "SELECT OrderDetail.OrderId AS OrderId, OrderDetail.LineNumber As LineNumber, OrderDetail.ProductId As ProductId, " +
                "Product.Name As ProductName, Product.ImageFileName As ImageFileName, " +
                "OrderDetail.Quantity As Quantity, OrderDetail.UnitCost As UnitCost " +
                "FROM OrderDetail INNER JOIN Product ON Product.ProductId = OrderDetail.ProductId " +
                "WHERE OrderDetail.OrderId = @p0 ORDER BY OrderDetail.LineNumber;";
            // Note that this is a "view" query across 2 tables 
            // so we need to create a new VIEW MODEL class "OrderDetailsQueryForCart" to match up
            // See this under folder "ViewModels"
            //150807 JPC Security improvement implementation of @p0, parameter orderId
            var cart = _context.OrderDetailsQueryForCart.FromSqlRaw(SQLCart, orderId).ToList();



            //140903 JPC
            //What to do about a product for the case where the user clicked add to cart ..
            //IF the product is already in the cart THEN raise the quantity by one ELSE add it in

            int lineNumber = 0;
            int? quantity = 0;
            //140903 JPC cover case of user is only looking at the cart without adding any changes
            if (ProductId == null)
            {
                //use lineNumber = -1 as a flag to skip the data writing in the following "if" block
                lineNumber = -1;
            }
            else
            {
                foreach (var item in cart)
                {
                    if (item.ProductId == ProductId)
                    {
                        lineNumber = item.LineNumber;
                        quantity = item.Quantity;
                        break;
                    }
                }
            } //end if

            rowsChanged = 0;
            if (lineNumber > 0)
            {
                quantity += 1;
                //2015-08-07 JPC Security improvement implementation of @p0, @p1, @p2 - (was {0}, {1}, {2})
                //2020-03-09 JPC ASP.NET Core 3.1 - ExecuteSqlCommand --> ExecuteSqlRaw
                SQLBuy = "UPDATE OrderDetail SET Quantity = @p0 WHERE OrderId = @p1 AND LineNumber = @p2 ";
                rowsChanged = _context.Database.ExecuteSqlRaw(SQLBuy, quantity, orderId, lineNumber);
            }
            else if (lineNumber == 0)
            {
                //writing a new line.  we need to know the unitcost
                //in real life work this could grow into a major method in a separate class involving special member discounts etc
                //here there is a choice between sending it in the querystring, or doing a new database lookup 
                //the querystring is easier but I am concerned about users being able to interfere with querystrings so I will go with the database

                //Safe bet is to stick to the SELECT * approach to match the existing generated classes.  
                //Can also call this the "go with the flow" method
                //150807 JPC Security improvement implementation of @p0
                SQLUnitCostLookup = "SELECT * FROM Product WHERE ProductId = @p0";
                var products = _context.Product.FromSqlRaw(SQLUnitCostLookup, ProductId).ToList();
                decimal unitCost = Convert.ToDecimal(products[0].UnitCost);

                lineNumber = cart.Count + 1;
                //150807 JPC Security improvement implementation of @p0 etc
                SQLBuy = "INSERT INTO OrderDetail VALUES(@p0, @p1, @p2, @p3, @p4)";
                rowsChanged = _context.Database.ExecuteSqlRaw(SQLBuy, orderId, lineNumber, ProductId, 1, unitCost);
            }

            //If User has selected a product to add then Query is UPDATE or INSERT but they both run like this
            if (SQLBuy != "")
            {
                if (rowsChanged != 1)
                {
                    //Error handling code to go in here.  Poss return a view with error messages.
                    //Code from our old webforms version is -- 
                    throw new Exception("ERROR with database table 'Order'.");
                }

                //If we have changed the cart in the database, then we need to reload it here
                //140903 JPC note the syntax for working with a "View Model"
                //150807 JPC Security improvement implementation of @p0, parameter orderId
                //2019-08-16 JPC needs to run AsNoTracking() method to bypass the "Entity Cache"
                //ref: https://codethug.com/2016/02/19/Entity-Framework-Cache-Busting/
                //big thanks to "codethug" (Tim Larson)
                cart = _context.OrderDetailsQueryForCart.FromSqlRaw(SQLCart, orderId).AsNoTracking().ToList();

                AllitemList = new List<Item>();
                decimal total = 0;
                foreach (var item in cart)
                {
                    AllitemList.Add(new Item
                    {
                        name = item.ProductName,
                        currency = "NZD",
                        price = Convert.ToString(Math.Round(Convert.ToDecimal(item.UnitCost), 2)),
                        quantity = Convert.ToString(item.Quantity),
                        sku = "sku"
                    });
                    total = total + (Convert.ToDecimal(item.UnitCost) * Convert.ToDecimal(item.Quantity));
                }

                SubTotal = Convert.ToString(Math.Round(total, 2));

            }

            //Give that Session object some work to do to wake it up and get it functional
            HttpContext.Session.SetInt32("OrderId", orderId);
            //20180312 JPC use ViewBag to get the orderId to the cart for display
            ViewBag.OrderId = orderId;
            return View(cart);

        }


        //AJAX!
        // This sample GET string is not useful as a copy/paste test because it only runs as a step in a longer process
        // that would be difficult to test manually.  Quoted as documentation example only
        // GET: OrderDetails/ShoppingCartUpdate?Quantity=4&LineNumber=7

        [HttpPost]
        public string ShoppingCartUpdate(string Quantity, string LineNumber)
        {
            string SQLUpdateOrderDetails = "";
            int rowsChanged = 0;

            //140903 JPC check that Quantity and LineNumber are numeric. Non-numeric or decimal could indicate hacker mischief-making
            //20180312 JPC IsNumeric method is coded at the bottom of this class
            if (/*!IsNumeric(Quantity) || !IsNumeric(LineNumber)
                ||*/ Convert.ToInt32(Quantity) != Convert.ToDouble(Quantity)
                || Convert.ToInt32(LineNumber) != Convert.ToDouble(LineNumber))
            {
                //TODO Code to log this event and send alert email to admin
                return "ERROR";
            }
            int orderId = Convert.ToInt32(HttpContext.Session.GetInt32("OrderId"));
            //2015-08-07 JPC Security improvement implementation of @p0 etc
            SQLUpdateOrderDetails = "UPDATE OrderDetail SET Quantity = @p0 WHERE OrderId = @p1 AND LineNumber = @p2";
            rowsChanged = _context.Database.ExecuteSqlRaw(SQLUpdateOrderDetails, Quantity, orderId, LineNumber);
            if (rowsChanged == 1)
            {
                //expected good result
                return "SUCCESS";
            }
            else if (rowsChanged == 0)
            {
                //nothing happened, a bit sad but there is no change so simple feedback is needed
                return "ERROR";
            }
            else
            {
                //more than 1 rows changed is in theory impossible.
                //the only possibility I can think of is some kind of hacking injection attack where % signs
                //get into the mix and give a wider scope to what the WHERE statement finds.
                //if it does happen then we have a major problem on our hands and we need 
                //to cancel this shopping cart immediately 
                //needs SQL DELETE for the cart
                return "ERROR HIGH SEVERITY"; //placeholder only, 
            }

        }

        public IActionResult CheckoutError(string error)
        {

            ViewBag.Message = error;
            return View();
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Checkout([FromBody] CheckoutViewModel checkout)
        {

            int orderId = Convert.ToInt32(HttpContext.Session.GetInt32("OrderId"));

            string SQLGetGrandTotal = "SELECT SUM(Quantity * UnitCost) AS GrandTotal FROM OrderDetail WHERE OrderId = @p0";

            var grandTotals = _context.GrandTotalViewModel.FromSqlRaw(SQLGetGrandTotal, orderId).ToList();
            decimal grandTotal = grandTotals[0].GrandTotal;

            string DeliveryAddress = checkout.AddressStreet + " " + checkout.Location
                + " " + checkout.Country + " " + checkout.PostCode;

            int rowsChanged;
            int statusId = 3;
            string SQL;
            try
            {
                //Change of statusId effectively clears the cart so the Customer cannot accidently buy these goods a second time!
                //Code goes here for UPDATE SQL statement and run this SQL Command with _context.Database.ExecuteSqlRaw
                SQL = "UPDATE [Order] SET TransactionId = @p0, OrderStatusId = @p1 "
                    + ", Notes = @p2, CustomerName = @p3, DeliveryAddress = @p4 "
                    + ", Phone = @p5, EmailAddress = @p6, CardOwner = @p7 "
                    + ", CardType = @p8 "
                    + "WHERE OrderId = @p9";
                rowsChanged = _context.Database.ExecuteSqlRaw(
                SQL, 0, statusId, "", " "
                , " ", " ", " ", " ", " "
                , orderId);
            }
            catch
            {


            }


            HttpContext.Session.SetInt32("OrderId", 0);


            return Ok();


        }


        #region Paypal

        public static string guid = "";
        public static string createdPayment_id = "";

        public ActionResult PaymentWithPaypal(string Cancel = null)
        {
            //getting the apiContext
            APIContext apiContext = PaypalConfiguration.GetAPIContext();

            try
            {
                //A resource representing a Payer that funds a payment Payment Method as paypal
                //Payer Id will be returned when payment proceeds or click to pay
                string payerId = "";// Request.Params["PayerID"];

                if (string.IsNullOrEmpty(payerId))
                {
                    //this section will be executed first because PayerID doesn't exist
                    //it is returned by the create function call of the payment class

                    // Creating a payment
                    // baseURL is the url on which paypal sendsback the data.
                    string baseURI = "https://localhost:44324/OrderDetails/SuccessView?";

                    //here we are generating guid for storing the paymentID received in session
                    //which will be used in the payment execution

                    guid = Convert.ToString((new Random()).Next(100000));

                    //CreatePayment function gives us the payment approval url
                    //on which payer is redirected for paypal account payment

                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid);

                    //get links returned from paypal in response to Create function call

                    var links = createdPayment.links.GetEnumerator();

                    string paypalRedirectUrl = null;

                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;

                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            //saving the payapalredirect URL to which user will be redirected for payment
                            paypalRedirectUrl = lnk.href;
                        }
                    }

                    // saving the paymentID in the key guid
                    createdPayment_id = createdPayment.id;

                    return Redirect(paypalRedirectUrl);
                }
                else
                {

                    // This function exectues after receving all parameters for the payment

                    var executedPayment = ExecutePayment(apiContext, payerId, guid);

                    //If executed payment failed then we will show payment failure message to user
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("FailureView");
                    }
                }
            }
            catch (Exception ex)
            {
                return View("FailureView");
            }

            //on successful payment, show success page to user.
            return View("SuccessView");
        }

        private PayPal.Api.Payment payment;
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            this.payment = new Payment() { id = paymentId };
            return this.payment.Execute(apiContext, paymentExecution);
        }

        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {
            //create itemlist and add item objects to it
            var itemList = new ItemList() { items = new List<Item>() };

            //Adding Item Details like name, currency, price etc

            foreach (var item in AllitemList)
            {
                Item objitem = new Item();

                objitem.name = item.name;
                objitem.currency = "NZD";
                objitem.price = item.price;
                objitem.quantity = item.quantity;
                objitem.sku = "sku";
                itemList.items.Add(objitem);
            }

            var payer = new Payer() { payment_method = "paypal" };

            // Configure Redirect Urls here with RedirectUrls object
            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };

            // Adding Tax, shipping and Subtotal details
            var details = new Details()
            {
                tax = "0",
                shipping = "0",
                subtotal = SubTotal
            };

            //Final amount with details
            var amount = new Amount()
            {
                currency = "NZD",
                total = SubTotal, // Total must be equal to sum of tax, shipping and subtotal.
                details = details
            };

            var transactionList = new List<Transaction>();
            // Adding description about the transaction
            transactionList.Add(new Transaction()
            {
                description = "Transaction description",
                invoice_number = "your invoice number", //Generate an Invoice No
                amount = amount,
                item_list = itemList
            });

            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };

            // Create a payment using a APIContext
            return this.payment.Create(apiContext);
        }

        #endregion


        public string deleteProducts(string p_Id)
        {
            int rowsChanged = 0;
            string SQLUpdateOrderDetails = " delete from OrderDetail where OrderId = @p0 AND ProductId = @p1";
            int orderId = Convert.ToInt32(HttpContext.Session.GetInt32("OrderId"));

            rowsChanged = _context.Database.ExecuteSqlRaw(SQLUpdateOrderDetails, orderId, p_Id);

            if (rowsChanged == 1)
            {
                return "SUCCESS";
            }
            else if (rowsChanged == 0)
            {
                return "ERROR";
            }
            else
            {
                return "ERROR HIGH SEVERITY";
            }
        }
    }
}
