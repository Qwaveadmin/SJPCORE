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

namespace SJPCORE.Controllers
{

    public class AccountController : Controller
    {

        private readonly ILogger<AccountController> _logger;
        private readonly DapperContext _context;
        private readonly HttpClient _client;
        private readonly AppDbContext _appDbContext;
        private readonly ISecretKeyHelper _secretKeyHelper;


        public AccountController(ILogger<AccountController> logger, DapperContext context, AppDbContext appDbContext, ISecretKeyHelper secretKeyHelper)
        {
            _logger = logger;
            _context = context;
            _appDbContext = appDbContext;
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

        [HttpPost]
[ActionName("SignIn")]
public IActionResult SignIn([FromBody] LoginModel model)
{
    if (ModelState.IsValid)
    {
        try
        {
            // ส่งไปคำขอเช็คกับระบบหลัก(https://demo.sjpradio.cloud/api/authentication/login) ว่ามีข้อมูลหรือไม่   
            var result = _client.PostAsync("https://demo.sjpradio.cloud/api/authentication/login", new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(model), System.Text.Encoding.UTF8, "application/json")).Result;
            if (result.IsSuccessStatusCode)
            {
                var data = result.Content.ReadAsStringAsync().Result;
                var response = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<string>>(data);
                if (response.Success)
                {
                    var SiteID = GlobalParameter.Config.Where(w => w.key == "SITE_ID").FirstOrDefault().value;
                    if (SiteID == null)
                    {
                        Response.Cookies.Append("Authorization", "danai");
                        Response.Cookies.Append("U", model.Username);
                        return RedirectToAction("SiteConfig", "SiteConfig");
                    }
                    else
                    {
                        string sql = "SELECT users_all.id , users_all.username, users_all.password, users_role.role FROM users_all INNER JOIN site_access ON users_all.id = site_access.users INNER JOIN users_role ON site_access.role = users_role.id WHERE users_all.username = @username AND site_access.site = @site";              
                        var user = _appDbContext.CreateConnection().Query<UserModel>(sql, new { username = model.Username , site = SiteID }).FirstOrDefault();
                        if (user != null)
                        {
                            // Update หรือ Insert ข้อมูลผู้ใช้ลงใน SQLite
                            using (var connection = _context.CreateConnection())
                            {
                                var userlist = connection.GetList<UserModel>().ToList();
                                var userdb = userlist.FirstOrDefault(w => w.Username.Equals(model.Username));
                                if (userdb != null)
                                {
                                    userdb.Username = model.Username;
                                    userdb.Password = user.Password;
                                    userdb.Role = user.Role;
                                    connection.Update(userdb);
                                }
                                else
                                {
                                    connection.Execute("INSERT INTO sjp_user (id, username, password, role) VALUES (@id, @username, @password, @role)", new { id = user.Id, username = user.Username, password = user.Password, role = user.Role });
                                }
                            }
                            Response.Cookies.Append("Authorization", user.Role == "Admin" ? "sutha" : "danai");
                            Response.Cookies.Append("U", model.Username);
                            return Ok(new { success = true,msg = "เข้าสู่ระบบสำเร็จ.." });
                        }
                        else
                        {
                            return Ok(new { success = false, msg = "ไม่มีสิทธิ์เข้าใช้งานระบบ.." });
                        }
                    }
                }
                else
                {
                    return Ok(new { success = false, msg = response.Error });
                }
            }
            else
            {
                return Ok(new { success = false, msg = "ไม่สามารถเชื่อมต่อกับระบบหลักได้.." });
            }
        }
        catch (Exception ex)
        {
            // ถ้าเชื่อมต่อไม่ได้ให้เข้าเงื่อนไขที่ใช้ _context เชื่อมต่อ
            using (var connection = _context.CreateConnection())
            {
                var userlist = connection.GetList<UserModel>().ToList();
                if (userlist.Any())
                {
                    var user = userlist.FirstOrDefault(w => w.Username.Equals(model.Username));
                    if (user != null)
                    {
                        if (VerifyPassword(model.Password, user.Password, GlobalParameter.Config.Where(w => w.key == "SECRETKEY").FirstOrDefault().value))
                        {
                            Response.Cookies.Append("Authorization", user.Role == "Admin" ? "sutha" : "danai");
                            Response.Cookies.Append("U", user.Username);
                            return Ok(new { success = true, msg = "เข้าสู่ระบบสำเร็จ.." });
                        }
                        else
                        {
                            return Ok(new { success = false, msg = "รหัสผ่านไม่ถูกต้อง.." });
                        }
                    }
                    else
                    {
                        return Ok(new { success = false, msg = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง.." });
                    }
                }
                else
                {
                    return Ok(new { success = false, msg = "ไม่พบชื่อผู้งานในระบบ" });
                }
            }
        }
    }
    else
    {
        return BadRequest();
    }
}


        [HttpPost]
        [ActionName("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("Authorization");
            Response.Cookies.Delete("U");
            return RedirectToAction("SignIn", "Account");
            
        }

        private bool VerifyPassword(string password, string saltedHash, string secretKey)
        {
            string computedHash = _secretKeyHelper.DecryptString(saltedHash, secretKey);
            return string.Equals(password, computedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
