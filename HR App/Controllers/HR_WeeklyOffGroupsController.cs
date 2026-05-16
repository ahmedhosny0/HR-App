using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class HR_WeeklyOffGroupsController : BaseController
    {
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        // =========================================
        // LIST
        // =========================================
        public IActionResult Index()
        {
            List<WeeklyOffGroupVM> list = new List<WeeklyOffGroupVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string q = @"
                SELECT
                    WeeklyOffGroupId,
                    GroupName,
                    IsActive,
                    CreatedDate
                FROM HR_WeeklyOffGroups
                ORDER BY WeeklyOffGroupId DESC";

                SqlCommand cmd = new SqlCommand(q, con);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new WeeklyOffGroupVM
                    {
                        WeeklyOffGroupId = Convert.ToInt32(dr["WeeklyOffGroupId"]),
                        GroupName = dr["GroupName"].ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        CreatedDate = Convert.ToDateTime(dr["CreatedDate"])
                    });
                }
            }

            return View(list);
        }

        // =========================================
        // CREATE GET
        // =========================================
        [HttpGet]
        public IActionResult Create()
        {
            LoadDays();

            WeeklyOffGroupVM model = new WeeklyOffGroupVM();

            return View(model);
        }

        // =========================================
        // CREATE POST
        // =========================================
        [HttpPost]
        public IActionResult Create(WeeklyOffGroupVM model)
        {
            if (string.IsNullOrWhiteSpace(model.GroupName))
            {
                TempData["Error"] = "اسم المجموعة مطلوب";

                LoadDays();

                return View(model);
            }

            if (model.SelectedDays == null || !model.SelectedDays.Any())
            {
                TempData["Error"] = "اختر يوم واحد على الأقل";

                LoadDays();

                return View(model);
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    // ==========================
                    // INSERT GROUP
                    // ==========================
                    string insertGroup = @"
                    INSERT INTO HR_WeeklyOffGroups
                    (
                        GroupName,
                        IsActive,
                        CreatedDate
                    )
                    VALUES
                    (
                        @GroupName,
                        1,
                        GETDATE()
                    );

                    SELECT SCOPE_IDENTITY();";

                    SqlCommand cmd = new SqlCommand(insertGroup, con, trans);

                    cmd.Parameters.AddWithValue("@GroupName", model.GroupName);

                    int groupId = Convert.ToInt32(cmd.ExecuteScalar());

                    // ==========================
                    // INSERT DETAILS
                    // ==========================
                    foreach (var dayId in model.SelectedDays)
                    {
                        string insertDetails = @"
                        INSERT INTO HR_WeeklyOffGroupDetails
                        (
                            WeeklyOffGroupId,
                            WeekDayId
                        )
                        VALUES
                        (
                            @WeeklyOffGroupId,
                            @WeekDayId
                        )";

                        SqlCommand detailsCmd =
                            new SqlCommand(insertDetails, con, trans);

                        detailsCmd.Parameters.AddWithValue(
                            "@WeeklyOffGroupId",
                            groupId);

                        detailsCmd.Parameters.AddWithValue(
                            "@WeekDayId",
                            dayId);

                        detailsCmd.ExecuteNonQuery();
                    }

                    trans.Commit();

                    TempData["Success"] = "تم الحفظ بنجاح";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    trans.Rollback();

                    TempData["Error"] = ex.Message;

                    LoadDays();

                    return View(model);
                }
            }
        }

        // =========================================
        // EDIT GET
        // =========================================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            WeeklyOffGroupVM model = new WeeklyOffGroupVM();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // ==========================
                // GROUP
                // ==========================
                string q = @"
                SELECT *
                FROM HR_WeeklyOffGroups
                WHERE WeeklyOffGroupId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.WeeklyOffGroupId =
                        Convert.ToInt32(dr["WeeklyOffGroupId"]);

                    model.GroupName =
                        dr["GroupName"].ToString();

                    model.IsActive =
                        Convert.ToBoolean(dr["IsActive"]);
                }

                dr.Close();

                // ==========================
                // DETAILS
                // ==========================
                string details = @"
                SELECT WeekDayId
                FROM HR_WeeklyOffGroupDetails
                WHERE WeeklyOffGroupId = @Id";

                SqlCommand detailsCmd =
                    new SqlCommand(details, con);

                detailsCmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr2 = detailsCmd.ExecuteReader();

                model.SelectedDays = new List<int>();

                while (dr2.Read())
                {
                    model.SelectedDays.Add(
                        Convert.ToInt32(dr2["WeekDayId"]));
                }
            }

            LoadDays();

            return View(model);
        }

        // =========================================
        // EDIT POST
        // =========================================
        [HttpPost]
        public IActionResult Edit(WeeklyOffGroupVM model)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    // ==========================
                    // UPDATE GROUP
                    // ==========================
                    string update = @"
                    UPDATE HR_WeeklyOffGroups
                    SET
                        GroupName = @GroupName,
                        IsActive = @IsActive
                    WHERE WeeklyOffGroupId = @Id";

                    SqlCommand cmd =
                        new SqlCommand(update, con, trans);

                    cmd.Parameters.AddWithValue("@GroupName", model.GroupName);

                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                    cmd.Parameters.AddWithValue(
                        "@Id",
                        model.WeeklyOffGroupId);

                    cmd.ExecuteNonQuery();

                    // ==========================
                    // DELETE OLD DETAILS
                    // ==========================
                    string delete = @"
                    DELETE FROM HR_WeeklyOffGroupDetails
                    WHERE WeeklyOffGroupId = @Id";

                    SqlCommand deleteCmd =
                        new SqlCommand(delete, con, trans);

                    deleteCmd.Parameters.AddWithValue(
                        "@Id",
                        model.WeeklyOffGroupId);

                    deleteCmd.ExecuteNonQuery();

                    // ==========================
                    // INSERT NEW DETAILS
                    // ==========================
                    foreach (var dayId in model.SelectedDays)
                    {
                        string insert = @"
                        INSERT INTO HR_WeeklyOffGroupDetails
                        (
                            WeeklyOffGroupId,
                            WeekDayId
                        )
                        VALUES
                        (
                            @GroupId,
                            @DayId
                        )";

                        SqlCommand insertCmd =
                            new SqlCommand(insert, con, trans);

                        insertCmd.Parameters.AddWithValue(
                            "@GroupId",
                            model.WeeklyOffGroupId);

                        insertCmd.Parameters.AddWithValue(
                            "@DayId",
                            dayId);

                        insertCmd.ExecuteNonQuery();
                    }

                    trans.Commit();

                    TempData["Success"] = "تم التعديل بنجاح";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    trans.Rollback();

                    TempData["Error"] = ex.Message;

                    LoadDays();

                    return View(model);
                }
            }
        }

        // =========================================
        // DELETE
        // =========================================
        public IActionResult Delete(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    string deleteDetails = @"
                    DELETE FROM HR_WeeklyOffGroupDetails
                    WHERE WeeklyOffGroupId = @Id";

                    SqlCommand cmd1 =
                        new SqlCommand(deleteDetails, con, trans);

                    cmd1.Parameters.AddWithValue("@Id", id);

                    cmd1.ExecuteNonQuery();

                    string deleteGroup = @"
                    DELETE FROM HR_WeeklyOffGroups
                    WHERE WeeklyOffGroupId = @Id";

                    SqlCommand cmd2 =
                        new SqlCommand(deleteGroup, con, trans);

                    cmd2.Parameters.AddWithValue("@Id", id);

                    cmd2.ExecuteNonQuery();

                    trans.Commit();

                    TempData["Success"] = "تم الحذف بنجاح";
                }
                catch (Exception ex)
                {
                    trans.Rollback();

                    TempData["Error"] = ex.Message;
                }
            }

            return RedirectToAction("Index");
        }

        // =========================================
        // LOAD DAYS
        // =========================================
        private void LoadDays()
        {
            List<WeekDayVM> days = new List<WeekDayVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string q = @"
                SELECT *
                FROM HR_WeekDays
                ORDER BY WeekDayId";

                SqlCommand cmd = new SqlCommand(q, con);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    days.Add(new WeekDayVM
                    {
                        WeekDayId = Convert.ToInt32(dr["WeekDayId"]),
                        DayNameAr = dr["DayNameAr"].ToString()
                    });
                }
            }

            ViewBag.Days = days;
        }
    }
}

