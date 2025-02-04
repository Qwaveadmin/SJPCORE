using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SJPCORE.Models.Attribute;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using SJPCORE.Models;
using System.Configuration;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Linq;
using static Slapper.AutoMapper;
using System.Net.Http;
using System.Formats.Asn1;
using System.Collections.Generic;
using System;
using SJPCORE.Models.Interface;
using Newtonsoft.Json;
using PuppeteerSharp.Cdp;
using System.Text;

namespace SJPCORE.Controllers
{

    public class AccountController : Controller
    {

        private readonly ILogger<AccountController> _logger;
        private readonly DapperContext _context;
        private readonly HttpClient _client;
        private readonly ISecretKeyHelper _secretKeyHelper;


        public AccountController(ILogger<AccountController> logger, DapperContext context, ISecretKeyHelper secretKeyHelper)
        {
            _logger = logger;
            _context = context;
            _secretKeyHelper = secretKeyHelper;
            _client = new HttpClient();
        }
        [ActionName("manage")]
        public IActionResult ListUsers()
            
        {
            using (var con = _context.CreateConnection())
            {
                var station = con.GetList<UserModel>();

                return View("users", station.ToList());
            }
        }

        [ActionName("SignIn")]
        public IActionResult SignIn()
        {
            return View("SignIn");
        }

        [ActionName("Config")]
        public IActionResult Config()
        {
                return View("Config");
        }

        [HttpPost]
        [ActionName("SignIn")]
        public async Task<IActionResult> SignIn([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            // try 
            // {
                // 1. ตรวจสอบ Host URL
                var hostConfig = GlobalParameter.Config.FirstOrDefault(w => w.key == "HOST_URL");
                if (hostConfig == null || string.IsNullOrWhiteSpace(hostConfig.value))
                {
                    return Ok(new { success = false, msg = "HOST_URL is not configured properly." });
                }

                var host = hostConfig.value.Trim();
                if (!Uri.TryCreate(host, UriKind.Absolute, out Uri hostUri))
                {
                    return Ok(new { success = false, msg = "HOST_URL is not a valid absolute URI." });
                }

                // 2. เช็คการ Authentication กับ Server หลัก
                _client.BaseAddress = hostUri;
                var result = await _client.PostAsync("api/authentication/login", 
                    new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));

                if (result.IsSuccessStatusCode)
                {
                    var data = await result.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<ApiResponse<string>>(data);

                    if (response.Data == null)
                        return Ok(new { success = false, msg = "Username or Password is incorrect" });

                    // 3. ถอดรหัสข้อมูล
                    string decrypted = _secretKeyHelper.DecryptString(response.Data, GlobalParameter.secretKey);
                    var authenModel = JsonConvert.DeserializeObject<AuthorizeModel>(decrypted);

                    if (!response.Success)
                        return Ok(new { success = false, msg = $"{response.Error} กรุณาติดต่อผู้ดูแลระบบ.." });

                    // 4. ตรวจสอบสิทธิ์การเข้าถึง Site
                    using (var connection = _context.CreateConnection())
                    {
                        var SITE_ID = connection.Get<ConfigModel>("SITE_ID").value;
                        Console.WriteLine(SITE_ID);
                        Console.WriteLine(decrypted);
                        var siteAccess = authenModel.site_access.FirstOrDefault(w => w.Site == SITE_ID);
                        
                        if (siteAccess == null)
                            return Ok(new { success = false, msg = "ไม่มีสิทธิ์เข้าใช้งานระบบ.." });

                        // 5. บันทึกหรืออัพเดทข้อมูลผู้ใช้
                        await UpdateUserInDatabase(connection, model.Username, authenModel.password, siteAccess.Role);

                        // 6. สร้าง Claims สำหรับ Authentication
                        var userRole = connection.GetList<UserRoleModel>()
                            .FirstOrDefault(w => w.Id == siteAccess.Role)?.Role;

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim(ClaimTypes.Role, userRole ?? "User"), // แปลงจาก Role ID เป็น Role Name
                            new Claim("Site", SITE_ID)
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        // 7. ตั้งค่า Authentication Properties
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true, // คงอยู่แม้ปิด browser
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) // cookie หมดอายุใน 12 ชั่วโมง
                        };

                        // 8. สร้าง Authentication Cookie
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme, 
                            principal,
                            authProperties);

                        return Ok(new { success = true, msg = "เข้าสู่ระบบสำเร็จ.." });
                    }
                }

                // 9. Fallback เมื่อไม่สามารถเชื่อมต่อ Server หลักได้
                return await HandleOfflineLogin(model);
            // }
            // catch (Exception ex)
            // {
            //     return Ok(new { success = false, msg = $"เกิดข้อผิดพลาด: {ex.Message}" });
            // }
        }

        // Method สำหรับ Update ข้อมูลผู้ใช้ในฐานข้อมูล
        private async Task UpdateUserInDatabase(IDbConnection connection, string username, string password, string role)
        {
            var userlist = connection.GetList<UserModel>().ToList();
            var userdb = userlist.FirstOrDefault(w => w.Username.Equals(username));
            
            if (userdb != null)
            {
                userdb.Password = password;
                userdb.Role = role;
                connection.Update(userdb);
            }
            else
            {
                connection.Execute(
                    "INSERT INTO sjp_user (username, password, role) VALUES (@username, @password, @role)",
                    new { username, password, role }
                );
            }
        }

        // Method สำหรับจัดการ Offline Login
        private async Task<IActionResult> HandleOfflineLogin(LoginModel model)
        {
            using (var connection = _context.CreateConnection())
            {
                var user = connection.GetList<UserModel>()
                    .FirstOrDefault(w => w.Username.Equals(model.Username));

                if (user == null)
                    return Ok(new { success = false, msg = "ไม่พบชื่อผู้ใช้งานในระบบ" });

                if (!VerifyPassword(model.Password, user.Password, GlobalParameter.secretKey))
                    return Ok(new { success = false, msg = "รหัสผ่านไม่ถูกต้อง.." });

                // สร้าง Claims สำหรับ Offline Mode
                var userRole = connection.GetList<UserRoleModel>()
                    .FirstOrDefault(w => w.Id == user.Role)?.Role;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, userRole ?? "User"),
                    new Claim("LoginMode", "Offline")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                    });

                return Ok(new { success = true, msg = "เข้าสู่ระบบสำเร็จ (Offline Mode).." });
            }
        }

        [HttpPost]
        [ActionName("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("SignIn");
        }

        private bool VerifyPassword(string password, string saltedHash, string secretKey)
        {
            string computedHash = _secretKeyHelper.DecryptString(saltedHash, secretKey);
            return string.Equals(password, computedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
