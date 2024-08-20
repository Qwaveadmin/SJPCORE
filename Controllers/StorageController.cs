using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SJPCORE.Models;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using SJPCORE.Util;
using SJPCORE.Models.Attribute;

namespace SJPCORE.Controllers
{
    [AuthorizationHeader]
    public class StorageController : Controller
    {
        private readonly ILogger<DashBoardController> _logger;
        private readonly DapperContext _context;
        private readonly IHostingEnvironment _environment;
        public StorageController(ILogger<DashBoardController> logger, DapperContext context, IHostingEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _environment = environment;
        }
        [HttpGet]
        [ActionName("Sound")]
        public async Task<IActionResult> Sound()
        {
            using (var con = _context.CreateConnection())
            {
                return View("sound-storage", await con.GetListAsync<StorageModel>()); 
            }
           
        }

        [HttpGet]
        [ActionName("SoundList")]
        public async Task<IActionResult> SoundList()
        {
            try
            {
                using (var con = _context.CreateConnection())
                {
                    return Json(new { success = true, message = "success" ,data= await con.GetListAsync<StorageModel>()});
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteFile(string key)
        {
            try
            {
                using (var con = _context.CreateConnection())
                {
                    try
                    {
                        string path = Path.Combine((await con.GetAsync<StorageModel>(key)).path , (await con.GetAsync<StorageModel>(key)).name_server);
   

                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                            await con.DeleteAsync<StorageModel>(key);
                            return Json(new { success = true, message = "ไฟล์ของคุณถูกลบสำเร็จ!!" });
                        }
                        else
                        {
                            throw new Exception("ไม่เจอไฟล์ดังกล่าวในระบบ");
                        }
                    }
                    catch(Exception ex)
                    {
                        return Json(new { success = false, message = ex.Message });
                    }

                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [ActionName("Download")]
        public async Task<IActionResult> DownloadFile(string key)
        {
            try
            {

                using (var con = _context.CreateConnection())
                {
                    try
                    {
                        var file = await con.GetAsync<StorageModel>(key);
                        var path = Path.Combine(file.path, file.name_server);

                        if (System.IO.File.Exists(path))
                        {
                            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                            var fileResult = new FileStreamResult(fileStream, "audio/wav");
                            fileResult.FileDownloadName = file.name_server;
                            return fileResult;
                        }
                        else
                        {
                            throw new Exception("File not found in the system.");
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 2097152000)]
        [ActionName("Sound")]
        public async Task<IActionResult> Upload(List<IFormFile> file, string category)
        {
            try
            {
                Console.WriteLine(1);
                // Get the path to the directory where the files should be saved
                var directory = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", category);

                // Create the directory if it does not exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                //loop through the files
                foreach (var f in file)
                {
                    if (f.Length > 0 && (f.ContentType == "audio/ogg" || f.ContentType == "audio/wav" || f.ContentType == "audio/mpeg"))
                    {
                        string extension = Path.GetExtension(Path.GetFileName(f.FileName));
                        string fileserver_name = StorageHelper.GenerateUuid() + extension;
                        // Get the full path to the file
                        var filePath = Path.Combine(directory, fileserver_name);

                        // Save the file to the server
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await f.CopyToAsync(fileStream);
                        }
                        using (var con = _context.CreateConnection())
                        {

                            string sql = @"INSERT INTO sjp_storage (category, key, name_defualt, name_server, path, size, upload_by, update_at, create_at)
                                           VALUES (@category, @key, @name_defualt, @name_server, @path, @size, @upload_by, @update_at, @create_at)";

                            var current_file = new StorageModel
                            {
                                category = category,
                                create_at = DateTime.Now,
                                update_at = DateTime.Now,
                                upload_by = 0,
                                size = f.Length.ToString(),
                                name_defualt = f.FileName,
                                key = StorageHelper.GenerateShortGuid(category.Substring(0, 3)),
                                path = directory,
                                name_server = fileserver_name
                            };
                            await con.ExecuteAsync(sql, current_file);

                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "ไม่รองรับรูปเเบบไฟล์ สามารถอัพโหลดได้เฉพาะไฟล์ MP3 เเละ Wav เท่านั้น!!" });
                    }
                }

                return Json(new { success = true, message = "ไฟล์ทั้งหมดถูกอัพโหลดสำเร็จ!!" });
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
           
        }

        [HttpGet("soundfile/{key}")]
        public IActionResult SoundFile([FromRoute] string key)
        {
            try
            {
                using (var con = _context.CreateConnection())
                {
                    var file = con.GetAsync<StorageModel>(key).Result;
                    var path = Path.Combine(file.path, file.name_server);

                    if (System.IO.File.Exists(path))
                    {
                        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                        var contentType = "audio/webm";
                        return new FileStreamResult(fileStream, contentType);
                    }
                    else
                    {
                        throw new Exception("File not found in the system...");
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("youtubeconvert/{key}")]
        public async Task<IActionResult> YoutubeConvertAsync([FromRoute] string key)
        {
            try{
                using (var con = _context.CreateConnection())
                {
                    var yt = await con.GetAsync<YtModel>(key);
                    var url = await SJPCORE.Util.YoutubeHelper.GetAudioStreamLinkAsync(yt.url);
                    return Json(new { success = true, message = url });
                }
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
    }
}
