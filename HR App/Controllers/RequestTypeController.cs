using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class RequestTypeController : BaseController
    {
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        #region RequestTypes
        public IActionResult RequestTypes()
        {
            List<RequestTypeVM> list = new List<RequestTypeVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RequestTypeId, Name FROM HR_RequestTypes";
                SqlCommand cmd = new SqlCommand(q, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new RequestTypeVM
                    {
                        RequestTypeId = Convert.ToInt32(dr["RequestTypeId"]),
                        Name = dr["Name"].ToString()
                    });
                }
            }

            return View(list);
        }
        [HttpPost]
        public IActionResult AddRequestType(RequestTypeVM model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("اسم النوع مطلوب");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string insert = @"
        INSERT INTO HR_RequestTypes (Name)
        VALUES (@Name)";

                SqlCommand cmd = new SqlCommand(insert, con);
                cmd.Parameters.AddWithValue("@Name", model.Name);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("RequestTypes");
        }
        public IActionResult EditRequestType(int id)
        {
            RequestTypeVM model = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RequestTypeId, Name FROM HR_RequestTypes WHERE RequestTypeId = @Id";
                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new RequestTypeVM
                    {
                        RequestTypeId = Convert.ToInt32(dr["RequestTypeId"]),
                        Name = dr["Name"].ToString()
                    };
                }
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult EditRequestType(RequestTypeVM model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("اسم النوع مطلوب");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        UPDATE HR_RequestTypes
        SET Name = @Name
        WHERE RequestTypeId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@Id", model.RequestTypeId);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "تم التعديل بنجاح";
            return RedirectToAction("RequestTypes");
        }
        public IActionResult DeleteRequestType(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "DELETE FROM HR_RequestTypes WHERE RequestTypeId = @Id";
                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "تم الحذف بنجاح";
            return RedirectToAction("RequestTypes");
        }
        #endregion

    }
}
