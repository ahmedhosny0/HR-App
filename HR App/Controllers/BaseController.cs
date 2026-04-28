using CK.ViewModel;
using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class BaseController : Controller
    {
        protected string username;
        protected string Password;
        protected string Role;
        protected string StoreIddynamic;
        protected string StoreIdRms;
        protected string PriceCategory;
        protected string Isuser;
        protected string checkStart;
        protected string checkEnd;
        protected string Inventlocation;
        protected string Delivery;
        protected string FranchiseTMT;
        protected string StoreOrder;
        protected string CloudBranch;
        protected string ItemOrder;
        protected string Company;
        protected string district;
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Retrieve session values once for all actions
            username = HttpContext.Session.GetString("Username");
            Password = HttpContext.Session.GetString("Password");
            Role = HttpContext.Session.GetString("Role");
            StoreIddynamic = HttpContext.Session.GetString("StoreIddynamic");
            StoreIdRms = HttpContext.Session.GetString("StoreIdRms");
            PriceCategory = HttpContext.Session.GetString("PriceCategory");
            Isuser = HttpContext.Session.GetString("isUsername");
            checkStart = HttpContext.Session.GetString("StartDate");
            checkEnd = HttpContext.Session.GetString("EndDate");
            Inventlocation = HttpContext.Session.GetString("Inventlocation");
            Delivery = HttpContext.Session.GetString("Delivery");
            FranchiseTMT = HttpContext.Session.GetString("FranchiseTMT");
            StoreOrder = HttpContext.Session.GetString("StoreOrder");
            CloudBranch = HttpContext.Session.GetString("CloudId");
            ItemOrder = HttpContext.Session.GetString("ItemOrder");
            Company = HttpContext.Session.GetString("Company");
            district = HttpContext.Session.GetString("District");
            // Store them in ViewBag for views
            ViewBag.Username = username;
            ViewBag.Password = Password;
            ViewBag.Role = Role;
            ViewBag.StoreIddynamic = StoreIddynamic;
            ViewBag.StoreIdRms = StoreIdRms;
            ViewBag.PriceCategory = PriceCategory;
            ViewBag.isUsername = Isuser;
            ViewBag.checkStart = checkStart;
            ViewBag.checkEnd = checkEnd;
            ViewBag.uuu = Isuser;
            ViewBag.Delivery = Delivery;
            ViewBag.FranchiseTMT = FranchiseTMT;
            ViewBag.StoreOrder = StoreOrder;
            ViewBag.CloudBranch = CloudBranch;
            ViewBag.ItemOrder = ItemOrder;
            ViewBag.Company = Company;
            ViewBag.district = district;
            ConnectionDB db = new ConnectionDB();
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    ViewBag.HasPendingItems = false;
                    return;
                }

                bool hasPendingItems = false;

                using (SqlConnection connection = new SqlConnection(db.TopSoftConnection))
                using (SqlCommand command = new SqlCommand(@"
                SELECT TOP 1 1
                FROM ItemTransactionsCode
                WHERE Arrived = 1
                  AND ItemStatus IN (4,5,6,7)
                  AND DATEDIFF(DAY, UpdatedDateTime, GETDATE()) >= 1
                  AND CreatedBy = @Username
            ", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    hasPendingItems = (result != null);
                }

                ViewBag.HasPendingItems = hasPendingItems;
            }
            catch
            {
                ViewBag.HasPendingItems = false;
            }       
        }
    }
}
