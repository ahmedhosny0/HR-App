using HR_App.Models.TopSoft;
using CK.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
namespace CK.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly TopSoftContext _dbContext;
        public static string HashPassword(string password)
        {
            // Generate a salt
            var salt = new byte[16]; // 128 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with the salt
            var hashedPassword = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000, // Recommended number of iterations
                numBytesRequested: 24); // 192 bits

            // Combine the salt and hashed password
            var hashBytes = new byte[salt.Length + hashedPassword.Length];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
            Buffer.BlockCopy(hashedPassword, 0, hashBytes, salt.Length, hashedPassword.Length);

            // Convert to base64 string
            return Convert.ToBase64String(hashBytes);
        }


        public static bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            // Convert the hashed password back to bytes
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract the salt and hashed password
            var salt = new byte[16]; // Assuming 128 bits
            var storedHashedPassword = new byte[24]; // Assuming 192 bits
            Buffer.BlockCopy(hashBytes, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(hashBytes, salt.Length, storedHashedPassword, 0, storedHashedPassword.Length);

            // Hash the provided password with the extracted salt
            var hashedProvidedPassword = KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000, // Must match the iteration count used when hashing
                numBytesRequested: 24); // Must match the number of bytes requested when hashing

            // Compare the hashed passwords
            return hashedProvidedPassword.SequenceEqual(storedHashedPassword);
        }
        public LoginController(ILogger<LoginController> logger, TopSoftContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

      
        public class PasswordVerifier
        {
            public static bool VerifyPassword(string password, string encodedHash)
            {
                // Hash the password
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                    // Convert byte array to a Base64-encoded string
                    string base64Hash = Convert.ToBase64String(bytes);

                    // Compare the hashes
                    return base64Hash.Equals(encodedHash);
                }
            }
        }
        public class SessionCheckMiddleware
        {
            private readonly RequestDelegate _next;

            public SessionCheckMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public async Task InvokeAsync(HttpContext context)
            {
                // Check if the request is for the login page
                if (context.Request.Path.StartsWithSegments("/Login/Login"))
                {
                    await _next(context);
                    return;
                }

                // Check if the session contains a username
                var sessionUsername = context.Session.GetString("username");
                if (string.IsNullOrEmpty(sessionUsername))
                {
                    // Check if the response has already started
                    if (!context.Response.HasStarted)
                    {
                        // Redirect to the login page if the session is null or the user is not authenticated
                        context.Response.Redirect("/Login/Login");
                        return;
                    }
                }

                // If the session is valid or the response has already started, continue with the next middleware
                await _next(context);
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new
                {
                    success = false,
                    message = "برجاء إدخال البريد الإلكتروني"
                });
            }
            // جلب كل المستخدمين في الجداول الذين لديهم نفس الإيميل
            var officeUsers = await _dbContext.Users.Where(x => x.Email == email).ToListAsync();
            var storeUsers = await _dbContext.Storeusers.Where(x => x.Email == email).ToListAsync();

            if (!officeUsers.Any() && !storeUsers.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "❌ الإيميل غير صحيح او غير موجود , تواصل معنا لاضافة الإيميل علي السستم"
                });
            }

            // نجمع كل usernames + passwords بعد فك التشفير
            var userCredentials = new List<(string Username, string Password)>();

            foreach (var user in officeUsers)
            {
                if (!string.IsNullOrEmpty(user.Password))
                {
                    userCredentials.Add((user.User1, Decrypt(user.Password)));
                }
            }

            foreach (var user in storeUsers)
            {
                if (!string.IsNullOrEmpty(user.Password))
                {
                    userCredentials.Add((user.Username, Decrypt(user.Password)));
                }
            }

            // إرسال كل بيانات الدخول على البريد
            foreach (var cred in userCredentials)
            {
                await SendEmailWithAttachmentAKAsync(email, cred.Username, cred.Password);
            }

            return Json(new
            {
                success = true,
                message = "✅ تم إرسال بيانات الدخول على البريد الإلكتروني"
            });
        }
        private static async Task SendEmailWithAttachmentAKAsync(string Email, string UserName, string Password)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Circle K", "circlek1.eg@gmail.com"));
            message.To.Add(new MailboxAddress(UserName, Email));
            message.Subject = "🔑 Circle K - بيانات الدخول الخاصة بك";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<html>
  <body style='margin:0; padding:0; font-family:Segoe UI, Roboto, Arial, sans-serif; background-color:#f4f6f9;'>
    <div style='max-width:620px; margin:40px auto; background-color:#ffffff; border-radius:8px; box-shadow:0 4px 12px rgba(0,0,0,0.05); overflow:hidden;'>

      <div style='background-color:#007bff; padding:20px 30px; color:white; text-align:center;'>
        <h1 style='margin:0; font-size:20px;'>🔑 Circle K - كلمة المرور الخاصة بك</h1>
      </div>

      <div style='padding:30px; color:#333333;'>
        <p style='font-size:15px;'>مرحباً {UserName},</p>

        <p style='line-height:1.7; font-size:14px;'>
          تم تعيين كلمة المرور الخاصة بك لاستخدام الدخول إلى نظام Circle K. يرجى حفظها في مكان آمن وعدم مشاركتها مع أي شخص آخر.
        </p>

        <p style='line-height:1.7; font-size:16px; font-weight:bold; background-color:#e9f7ef; padding:15px; border-radius:6px; text-align:center;'>
          إسم المستخدم: <span style='color:#28a745;'>{UserName}</span>
        </p>

        <p style='line-height:1.7; font-size:16px; font-weight:bold; background-color:#e9f7ef; padding:15px; border-radius:6px; text-align:center;'>
          كلمـة المـرور الخاصـة بك: <span style='color:#28a745;'>{Password}</span>
        </p>

        <p style='line-height:1.7; font-size:14px;'>
          بعد تسجيل الدخول، يمكنك تغيير كلمة المرور الخاصة بك في أي وقت من إعدادات الحساب.
        </p>

        <p style='margin-top:30px; font-size:14px;'>مع أطيب التحيات،</p>
        <p style='font-weight:bold; font-size:14px;'>
          Ahmed Hosny Ahmed<br>
          Web Developer<br>
          Circle K
        </p>
      </div>

      <div style='background-color:#f1f3f5; padding:15px 30px; text-align:center; font-size:12px; color:#6c757d;'>
        ⓘ هذه رسالة تلقائية من نظام Circle K.<br />
        يرجى عدم الرد مباشرة على هذا البريد الإلكتروني.
      </div>
    </div>
  </body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();
            //hohdiahjjkrmdvqu

            try
            {
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Timeout = 20000; // 20 seconds timeout

                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("circlek1.eg@gmail.com", "hohdiahjjkrmdvqu"); // App password
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email send failed to {Email}: {ex.Message}");
            }
        }
        public IActionResult Login()
        {
            ViewBag.RememberMe = Request.Cookies["rememberMe"] == "true";
            ViewBag.SavedUsername = Request.Cookies["savedUsername"];
            var u = TempData["u"];
            ViewBag.u = u;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login( VMLogin modellogin)
        {
            
            if (string.IsNullOrWhiteSpace(modellogin.username) || string.IsNullOrWhiteSpace(modellogin.Password))
            {
                TempData["ValidateMessage"] = "Username and password are required.";
                ViewBag.Emptydata = "V";
                return View("Login");
            }
            var sessionUsername = HttpContext.Session.GetString("username"); // Corrected key
            Console.WriteLine($"SessionUsername: {sessionUsername}");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            string hashedProvidedPassword = encrypt(modellogin.Password!);

            var authenticatedUser = _dbContext.RptUsers
      .FirstOrDefault(u => (u.Username == modellogin.username && u.Password == hashedProvidedPassword) || (u.Username2 == modellogin.username && u.Password == hashedProvidedPassword)
      || (modellogin.username == "ahak" && modellogin.Password == "Ad#2020"));
            string connectionString = "Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=7200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;";
            // Check if the user exists and the password is correct
            if (authenticatedUser == null)
            {
                ViewBag.Usererror = "V";
                return View("Login");
            }

            if (authenticatedUser != null)//&& VerifyPassword(authenticatedUser.Spassword, modellogin.Password))
            {
                TopSoftContext db2 = new TopSoftContext();
                var username = authenticatedUser.Username;
                var username2 = authenticatedUser.Username2;
                var role = authenticatedUser.Role;
                var allowedRoles = new[] { "Manager", "HeadOfficeHR", "HRandManager", "Employee" };

                if (string.IsNullOrEmpty(role) || !allowedRoles.Contains(role))
                {
                    // بدلاً من Logout، سنرسل متغير للـ View يخبره أن الصلاحية مرفوضة
                    ViewBag.NoRole = "V";
                    return View("Login");
                }
                bool isDmanager = db2.RptUsers.Any(s => s.Dmanager == username);
                bool isUsername = db2.RptUsers.Any(s => s.Username == username && (s.Role == " " || s.Role == null) && (s.Storenumber != null || s.Storenumber != " "));
                string isuser = Convert.ToString(isUsername);
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var userPrincipal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userPrincipal,
                    new AuthenticationProperties
                    {
                        IsPersistent = modellogin.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    });
                if (modellogin.RememberMe)
                {
                    Response.Cookies.Append("rememberMe", "true", new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(30)
                    });

                    Response.Cookies.Append("savedUsername", username, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(30)
                    });
                }
                else
                {
                    Response.Cookies.Delete("rememberMe");
                    Response.Cookies.Delete("savedUsername");
                }

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            using (SqlCommand command = new SqlCommand("INSERT INTO Syslog (Username, Password,LoginDate,Role) VALUES (@Username, @Password,@Date,@Role)", connection))
                            {
                                command.Parameters.AddWithValue("@Username", modellogin.username);
                                command.Parameters.AddWithValue("@Password", modellogin.Password);
                                command.Parameters.AddWithValue("@Date", DateTime.Now);
                                command.Parameters.AddWithValue("@Role", authenticatedUser.Role);

                                connection.Open(); // Open the connection
                                command.ExecuteNonQuery(); // Execute the command
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return View();
                    }
  
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                AuthenticationProperties properties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    // Set the ExpiresUtc to a past date to clear the cookie upon closing the browser
                    //   ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
                };
                TopSoftContext TopSoftDB = new TopSoftContext();
                // Sign in the user
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), properties);
                 if (authenticatedUser.Username != "")
                {
                    HttpContext.Session.SetString("Username", authenticatedUser.Username);
                    HttpContext.Session.SetString("Password", authenticatedUser.Password ?? string.Empty);
                    HttpContext.Session.SetString("Role", authenticatedUser.Role ?? string.Empty);
                    HttpContext.Session.SetString("Server", authenticatedUser.Server ?? string.Empty);
                    HttpContext.Session.SetString("Inventlocation", authenticatedUser.Inventlocation ?? string.Empty);
                    HttpContext.Session.SetString("StoreIddynamic", authenticatedUser.Storenumber ?? string.Empty);
                    HttpContext.Session.SetString("StoreIdRms", authenticatedUser.RmsstoNumber ?? string.Empty);
                    HttpContext.Session.SetString("PriceCategory", authenticatedUser.Category ?? string.Empty);
                    HttpContext.Session.SetString("isUsername", isuser ?? string.Empty);
                    HttpContext.Session.SetString("Delivery", authenticatedUser.Delivery?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("FranchiseTMT", authenticatedUser.FranchiseTmt?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("StoreOrder", authenticatedUser.StoreOrder?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("CloudId", authenticatedUser.CloudId?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("ItemOrder", authenticatedUser.ItemOrder?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("Company", authenticatedUser.Company?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("District", authenticatedUser.District?.ToString() ?? string.Empty);

                }
                else
                {
                    HttpContext.Session.SetString("Username", authenticatedUser.Username2);
                    HttpContext.Session.SetString("Password", authenticatedUser.Password ?? string.Empty);
                    HttpContext.Session.SetString("Role", authenticatedUser.Role ?? string.Empty);
                    HttpContext.Session.SetString("Server", authenticatedUser.Server ?? string.Empty);
                    HttpContext.Session.SetString("Inventlocation", authenticatedUser.Inventlocation ?? string.Empty);
                    HttpContext.Session.SetString("StoreIddynamic", authenticatedUser.Storenumber ?? string.Empty);
                    HttpContext.Session.SetString("StoreIdRms", authenticatedUser.RmsstoNumber ?? string.Empty);
                    HttpContext.Session.SetString("PriceCategory", authenticatedUser.Category ?? string.Empty);
                    HttpContext.Session.SetString("isUsername", isuser ?? string.Empty);
                    HttpContext.Session.SetString("Delivery", authenticatedUser.Delivery?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("FranchiseTMT", authenticatedUser.FranchiseTmt?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("StoreOrder", authenticatedUser.StoreOrder?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("CloudId", authenticatedUser.CloudId?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("ItemOrder", authenticatedUser.ItemOrder?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("Company", authenticatedUser.Company?.ToString() ?? string.Empty);
                    HttpContext.Session.SetString("District", authenticatedUser.District?.ToString() ?? string.Empty);

                }
                return RedirectToAction("Home", "Home");
            }

            if (HttpContext.Session.GetString("LoggedOut") == "true")
            {
                TempData["PreventBack"] = true;
                HttpContext.Session.SetString("LoggedOut", "false"); 
            }

            return View();
        }
        [HttpGet]
        public IActionResult CreateUser()
        {
            var username = HttpContext.Session.GetString("Username");
            var Password = HttpContext.Session.GetString("Password");
            var Role = HttpContext.Session.GetString("Role");
            var StoreIddynamic = HttpContext.Session.GetString("StoreIddynamic");
            var StoreIdRms = HttpContext.Session.GetString("StoreIdRms");
            var PriceCategory = HttpContext.Session.GetString("PriceCategory");
            var Isuser = HttpContext.Session.GetString("isUsername");
            ViewBag.Username = username;
            ViewBag.Password = Password;
            ViewBag.Role = Role;
            ViewBag.StoreIdRms = StoreIdRms;
            ViewBag.StoreIddynamic = StoreIddynamic;
            ViewBag.isUsername = Isuser;
            return View();
        }
        public string encrypt(string clearText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public string Decrypt(string clearText)
        {
            string DecryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Convert.FromBase64String(clearText);
            using (Aes decryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(DecryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                decryptor.Key = pdb.GetBytes(32);
                decryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return clearText;
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("Id,User1,Password,Role,Department,UpdatedDatetime,Email")] User user)
        {
            // Encrypt the password before saving
            user.Role ??= "0";
            user.Department ??= "0";
            user.UpdatedDateTime = DateTime.Now;
            user.Password = encrypt(user.Password);

            _dbContext.Add(user);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("DisplayUsers");
        }
        public IActionResult Privacy()
        {
            return View();
        }
      
    }

}

