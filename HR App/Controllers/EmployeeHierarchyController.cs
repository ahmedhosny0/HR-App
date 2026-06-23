using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class EmployeeHierarchyController : Controller
    {
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        #region EmployeeHierarchy
        public IActionResult EmployeeHierarchy()
        {
            ViewBag.Roles = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RoleId, RoleName FROM HR_Roles";
                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ((List<SelectListItem>)ViewBag.Roles).Add(new SelectListItem
                    {
                        Value = dr["RoleId"].ToString(),
                        Text = dr["RoleName"].ToString()
                    });
                }
            }

            ViewBag.List = GetHierarchy(); // لازم ترجع Id كمان

            return View();
        }
        [HttpPost]
        public IActionResult EmployeeHierarchy(int ChildRoleId, int ParentRoleId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        INSERT INTO HR_EmployeeHierarchy
        (ChildRoleId, ParentRoleId, IsActive)
        VALUES (@Child, @Parent, 1)";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Child", ChildRoleId);
                cmd.Parameters.AddWithValue("@Parent", ParentRoleId);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("EmployeeHierarchy");
        }
        private void LoadRoles()
        {
            var roles = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RoleId, RoleName FROM HR_Roles";
                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    roles.Add(new SelectListItem
                    {
                        Value = dr["RoleId"].ToString(),
                        Text = dr["RoleName"].ToString()
                    });
                }
            }

            ViewBag.Roles = roles;
        }
        public IActionResult EditHierarchy(int id)
        {
            EmployeeHierarchyVM model = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"SELECT Id, ChildRoleId, ParentRoleId 
                     FROM HR_EmployeeHierarchy
                     WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new EmployeeHierarchyVM
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        ChildRoleId = Convert.ToInt32(dr["ChildRoleId"]),
                        ParentRoleId = Convert.ToInt32(dr["ParentRoleId"])
                    };
                }
            }

            LoadRoles();
            return View(model);
        }
        [HttpPost]
        public IActionResult EditHierarchy(EmployeeHierarchyVM model)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"UPDATE HR_EmployeeHierarchy
                     SET ChildRoleId = @Child,
                         ParentRoleId = @Parent
                     WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Child", model.ChildRoleId);
                cmd.Parameters.AddWithValue("@Parent", model.ParentRoleId);
                cmd.Parameters.AddWithValue("@Id", model.Id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("EmployeeHierarchy");
        }
        public IActionResult DeleteHierarchy(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "DELETE FROM HR_EmployeeHierarchy WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("EmployeeHierarchy");
        }
        private List<dynamic> GetHierarchy()
        {
            var list = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        SELECT 
            h.Id,
            c.RoleName AS ChildRole,
            p.RoleName AS ParentRole
        FROM HR_EmployeeHierarchy h
        JOIN HR_Roles c ON h.ChildRoleId = c.RoleId
        JOIN HR_Roles p ON h.ParentRoleId = p.RoleId";

                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        Child = dr["ChildRole"].ToString(),
                        Parent = dr["ParentRole"].ToString()
                    });
                }
            }

            return list;
        }        // =========================
        #endregion
    }
}
