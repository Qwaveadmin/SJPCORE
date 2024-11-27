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
using Newtonsoft.Json;
using PuppeteerSharp.Cdp;

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
        public IActionResult SignIn([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {

                    //Get HOST_URL from setting table
       
                    var host = GlobalParameter.Config.Where(w => w.key == "HOST_URL").FirstOrDefault().value;
                    // ส่งไปคำขอเช็คกับระบบหลัก(https://demo.sjpradio.cloud/api/authentication/login) ว่ามีข้อมูลหรือไม่   
                    var result = _client.PostAsync($"{host}api/authentication/login", new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(model), System.Text.Encoding.UTF8, "application/json")).Result;
         
                    if (result.IsSuccessStatusCode) 
                    {
                        var data = result.Content.ReadAsStringAsync().Result;
                        var response = JsonConvert.DeserializeObject<ApiResponse<string>>(data);

                        // check null
                        if (response.Data == null)
                        {
                            return Ok(new { success = false, msg = "Username or Password is incorrect" });
                        }
                        string decrypted = _secretKeyHelper.DecryptString(response.Data, GlobalParameter.secretKey);
                        var authenModel = JsonConvert.DeserializeObject<AuthorizeModel>(decrypted);
          
                        if (response.Success)
                        {
                            using (var connection = _context.CreateConnection())
                            {
                                var SITE_ID = connection.Get<ConfigModel>("SITE_ID").value;
                                // ตรวจสอบว่าใน authenModel มีข้อมูล Site ที่ตรงกับ SITE_ID หรือไม่                            
                                if (authenModel.site_access.Any(w => w.Site == SITE_ID))
                                {
                                    // Update หรือ Insert ข้อมูลผู้ใช้ลงใน SQLite
                                    
                                        var userlist = connection.GetList<UserModel>().ToList();
                                        var userdb = userlist.FirstOrDefault(w => w.Username.Equals(model.Username));
                                        if (userdb != null)
                                        {
                                            userdb.Username = model.Username;
                                            userdb.Password = authenModel.password;
      
                                            userdb.Role = authenModel.site_access.FirstOrDefault(w => w.Site == SITE_ID).Role;
    
                                            connection.Update(userdb);
                                        }
                                        else
                                        {
                                            connection.Execute("INSERT INTO sjp_user (username, password, role) VALUES (@username, @password, @role)", new {username = model.Username, password = authenModel.password, role = authenModel.site_access.FirstOrDefault(w => w.Site == SITE_ID).Role });
                                        }
                                        Response.Cookies.Append("Authorization" , connection.GetList<UserRoleModel>().FirstOrDefault(w => w.Id == authenModel.site_access.FirstOrDefault(w => w.Site == SITE_ID).Role).Role == "Admin" ? "sutha" : "danai");
                                        Response.Cookies.Append("U", model.Username);
                                        return Ok(new { success = true,msg = "เข้าสู่ระบบสำเร็จ.." });
                                    }
                                
                                // else if (string.IsNullOrEmpty(SITE_ID))
                                // {
                                //     // Set Cookie and Redirect to Config Page
                                //     Response.Cookies.Append("Authorization", "config");
                                //     Response.Cookies.Append("U", model.Username);
                                //     return Ok(new { success = true, msg = "กรุณาตั้งค่า Site ID ก่อนใช้งาน..", redirectUrl = "/Account/Config" });
                                // }
                                else
                                {
                                    return Ok(new { success = false, msg = "ไม่มีสิทธิ์เข้าใช้งานระบบ.." });
                                }
                            }
                        }
                        else
                        {
                            return Ok(new { success = false, msg = response.Error + " กรุณาติดต่อผู้ดูแลระบบ.." });
                        }
                    }
                    else
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
                                    if (VerifyPassword(model.Password, user.Password, GlobalParameter.secretKey))
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
