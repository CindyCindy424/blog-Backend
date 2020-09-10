using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Temperature.Models;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.Drawing;

/** 备注：
 * 这里用户登录只需要输入NickName,然后自动匹配数据库中该用户的id，然后验证密码正确性
 * 用户不需要知道自己的id
 */


namespace Temperature.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiController]
    public class AccountController : Controller
    {
        private blogContext entity = new blogContext(); //整体数据库类型
        //private static int idNum = 0;//id自增
        //private static int idForAnnouncement = 0;//公告id自增
        private IWebHostEnvironment My_Environment;
        public AccountController(IWebHostEnvironment _environment)
        {
            My_Environment = _environment;
        }
        string PicsRootPath = "BlogPics\\Avator";


        //生成token
        [HttpGet]
        public string getToken(string Username)
        {
            //从数据库验证用户名，密码 
            //验证通过 否则 返回Unauthorized

            //创建claim
            try {
                var authClaims = new[] {
                    new Claim(JwtRegisteredClaimNames.Sub,Username),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                };
                IdentityModelEventSource.ShowPII = true;
                //签名秘钥 可以放到json文件中
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SecureKeySecureKeySecureKeySecureKeySecureKeySecureKey"));

                var token = new JwtSecurityToken(
                    issuer: "https://www.cnblogs.com/chengtian",
                    audience: "https://www.cnblogs.com/chengtian",
                    expires: DateTime.Now.AddHours(2),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                //返回token和过期时间
                return new JwtSecurityTokenHandler().WriteToken(token);
            } catch(Exception e) {
                Console.log(e,Message);
                return "" ;
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="nick_name">用户名</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        /// <response code="200">成功</response>
        ///<remarks>
        ///     返回内容示例
        ///     {
        ///         userid = id,
        ///         loginFlag = flag, 
        ///         token = token
        ///     }
        ///     
        ///     flag：
        ///     0：未执行
        ///     1：成功
        ///     2：密码错误
        ///     3：用户名不存在
        ///     
        /// </remarks>
        [AllowAnonymous]
        [HttpPost]
        //[SwaggerResponse(0, "文档注释", typeof(User))]
        //[SwaggerResponse(0, "文档注释",typeof(User))]
        public ActionResult Login(string nick_name, string password) {
            User user = new User();
            string token = "";
            int flag = 0;
            int id = -1;
            //JsonData jsondata = new JsonData();  //json格式的数据
            try {
                var userid =    //EF中的Linq语法
                            (from u in entity.User
                             where u.NickName == nick_name
                             select u.UserId).Distinct(); //与sql中的select distinct类同
                id = userid.FirstOrDefault();
                if (userid.FirstOrDefault() != default) {
                    //var id = userid.FirstOrDefault();
                    user = entity.User.Find(id);  //根据主键id找
                    if (user.Password != password) {
                        flag = 2;//密码错误
                    }
                    else {
                        //jsondata["userID"] = id;
                        flag = 0;//成功

                        token = getToken(nick_name);
                    }
                }
                else {
                    flag = 1;//用户名不存在
                }
            }
            catch (Exception e) {
                flag = 0;
            }
            var data = new {
                userid = id,
                loginFlag = flag,
                token = token
            };

            //jsondata["LoginFlag"] = flag.ToString();
            //var data = Json(jsondata.ToJson());
            //return Ok(data);
            //return Ok(Json(jsondata.ToJson()));
            //return Ok(data);
            return Json(data);

        }


        /// <summary>
        /// 判断用户名是否被占用
        /// </summary>
        /// <param name="username">待注册名字</param>
        /// <returns>
        ///     False: 已占用
        ///     True：未占用
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        public JsonResult nameCheck(string username)
        {
            User user = new User();
            //JsonData jsondata = new JsonData();  //json格式的数据
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == username
                     select c.UserId).Distinct();
            if (userid.FirstOrDefault() != default)
            {
                flag = 2; //用户名已经被占用
                return Json(new { result = "False" });
            }
            else
            { 
                flag = 1;//用户名可以使用。
                return Json(new { result = "True" });
            }
        }

        /*
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="nick_name">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>"RegisterFlag"用户名已经被注册为0，注册成功为1</returns>
        //[Route("Register")]
        [HttpPost]
        public ActionResult Register(string nick_name, string password)
        {
            User user = new User();
            //JsonData jsondata = new JsonData();  //json格式的数据
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            if (userid.FirstOrDefault() != null)  //这里是null吗？id不可以为null 所以即使没有也bushinull
            {
                flag = 0; //用户名名已经被占用
                //?userid 本来就不可以为空啊？ 那这个地方是表示的？
            }
            else
            {
                user.UserId = (++idNum).ToString();
                user.NickName = nick_name;
                user.Password = password;
                entity.User.Add(user); //把user这个实体加入数据库
                entity.SaveChanges();
                flag = 1; //注册成功
            }
            Dictionary<string, string> jsondata = new Dictionary<string, string>();
            jsondata.Add("RegisterFlag", flag.ToString());
            return Json(JsonConvert.SerializeObject(jsondata, Formatting.Indented));

        }*/
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <param name="tel"></param>
        /// <param name="wechat"></param>
        /// <returns></returns>
        /// <response code="200">注册成功</response>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///         RegisterFlag = flag
        ///     }
        ///     
        ///     flag:
        ///     0: 未执行
        ///     1：注册成功
        ///     2：用户名已经被占用
        /// </remarks>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult register(string nick_name, string password,string email,string tel,string wechat)
        {
            User user = new User();
            //JsonData jsondata = new JsonData();  //json格式的数据
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            if (userid.FirstOrDefault() != default)  
            {
                flag = 2; //用户名名已经被占用
               
            }
            else
            {
                //user.UserId = (++idNum).ToString();
                //这里id是自增类型
                user.NickName = nick_name;
                user.Password = password;
                user.Email = email;
                user.Tel = tel;
                user.Wechat = wechat;
                user.Avatr = "BlogPics\\Avator\\defaultAvator.png";
                entity.User.Add(user); //把user这个实体加入数据库
                entity.SaveChanges();
                flag = 1; //注册成功
            }
            //Dictionary<string, string> jsondata = new Dictionary<string, string>();
            //jsondata.Add("RegisterFlag", flag.ToString());
            var data = new
            {
                RegisterFlag = flag
            };
            /*
            switch (flag)
            {
                case 0: //用户名被占用
                    Response.StatusCode = 402;
                    break;
                case 1: //注册成功
                    Response.StatusCode = 200;
                    break;
            }*/
            return Json(data);
            //return Json(JsonConvert.SerializeObject(jsondata, Formatting.Indented));

        }


        /// <summary>
        /// 维护个人信息（不含头像维护）
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="gender"></param>
        /// <param name="location"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="email"></param>
        /// <param name="tel"></param>
        /// <param name="wechat"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///         Infoflag = flag
        ///     }
        ///     
        ///     flag:
        ///     0: 未执行
        ///     1：信息存储完成
        ///     2：没有找到该用户
        /// 
        /// </remarks>
        /// 
        [Authorize]
        [HttpPost]
        public JsonResult personalInfo(string nick_name, string gender, string location, string year, string month, string day, string email, string tel, string wechat)
        {
            //User user = new User();
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            var user = entity.User.Find(id); //在数据库中根据key找到相应记录

            if (id == default)
            {
                flag = 2; //没有找到该用户
            }
            else
            {
                //user.UserId = id;
                user.Gender = gender;
                user.Location = location;
                //DateTime dateOfBirth = new DateTime();
                //string.Format("yyyy年MM月dd日", dateOfBirth);
                //user.Dob = dateOfBirth;

                var yyyy = year;
                var mm = month;
                var dd = day;
                DateTime dateTime = new DateTime();
                DateTime.TryParse(yyyy + "-" + mm + "-" + dd, out dateTime);
                user.Dob = dateTime;

                user.Email = email;
                user.Tel = tel;
                user.Wechat = wechat;
                //entity.User.Add(user); //把user这个实体加入数据库
                entity.Entry(user).State = EntityState.Modified;
                entity.SaveChanges();
                flag = 1; //信息存储完成
            }
            var data = new
            {
                Infoflag = flag
            };
            /*
            if (flag == 0)
            {
                Response.StatusCode = 404;//没有找到该用户
                return Json(data);
            }
            else
            {
                Response.StatusCode = 200; //成功修改信息
                return Json(data);
            }*/
            return Json(data);
        }

        /*public JsonResult UpdateAvatr(HttpPostedFileBase fileData)
        {
            if (fileData != null)
            {
                try
                {
                    // 文件上传后的保存路径
                    //string filePath = Microsoft.AspNetCore.Http.HttpContext.Current.Server.MapPath("~/Uploads/");
                    string filePath = My_Environment.ContentRootPath;
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    string fileName = Path.GetFileName(fileData.FileName);// 原始文件名称
                    string fileExtension = Path.GetExtension(fileName); // 文件扩展名
                    string saveName = Guid.NewGuid().ToString() + fileExtension; // 保存文件名称

                    fileData.SaveAs(filePath + saveName);

                    return Json(new { Success = true, FileName = fileName, SaveName = saveName });
                }
                catch (Exception ex)
                {
                    return Json(new { Success = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { Success = false, Message = "请选择要上传的文件！" }, JsonRequestBehavior.AllowGet);
            }
        }*/
        /*
        /// <summary>
        /// 上传头像
        /// </summary>
        /// <param name="collection">[FromForm]头像文件</param>
        /// <param name="nick_name">昵称</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///         UploadFlag = flag
        ///     }
        ///     flag:
        ///     0: 未执行
        ///     1：成功
        ///     2：没有找到该用户
        /// </remarks>
        [HttpPost]
        public JsonResult updateAvatr([FromForm] IFormCollection collection, string nick_name)
        {
            //var files = Request.Form.Files;
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                flag = 2; //没有找到该用户
                //Response.StatusCode = 202;//没有该用户
                return Json(new {  UploadFlag = flag });
            }
            var user = entity.User.Find(id); //在数据库中根据key找到相应记录
            


            FormFileCollection files = (FormFileCollection)collection.Files;
            IFormFile file = files.FirstOrDefault();

            string filename = file.FileName;//--"360截图20191119113847612.jpg"
            string fileExtention = System.IO.Path.GetExtension(file.FileName);//--.jpg
            string path = Guid.NewGuid().ToString() + fileExtention;
            string basepath = My_Environment.ContentRootPath;//en.WebRootPath-》wwwroot的目录; .ContentRootPath到达WebApplication的项目目录
            //string basepath = "../"
            string testpath = My_Environment.WebRootPath;
            string savePath = basepath + "\\user's_avatr\\" + path;
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            //  using (FileStream fstream = System.IO.File.Create(newFile)) 也可以
            using (FileStream fstream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                file.CopyTo(fstream); // 复制文件
                fstream.Flush();//清空缓存区
                user.Avatr = savePath;
                entity.Entry(user).State = EntityState.Modified;
                entity.SaveChanges();
                flag = 1;
                //Response.StatusCode = 201;//成功

            }
            return Json(new {  UploadFlag = flag ,result = testpath});
        }*/
        /*
        /// <summary>
        /// 上传头像图片
        /// </summary>
        /// <param name="files">图片文件</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回：
        ///     
        ///     {UploadFlag = flag}
        ///     
        ///     flag:
        ///     
        ///     0:未操作
        ///     1：成功
        ///     2：没有找到该用户
        /// </remarks>
        [HttpPost("Photos")]
        public async Task<IActionResult> updateAvatr(IFormFileCollection files,string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                flag = 2; //没有找到该用户
                //Response.StatusCode = 202;//没有该用户
                return Json(new { UploadFlag = flag });
            }
            var user = entity.User.Find(id); //在数据库中根据key找到相应记录




            long size = files.Sum(f => f.Length);
            //var fileFolder = Path.Combine(My_Environment.WebRootPath, "Photos");
            var fileFolder = Path.Combine("../BlogPics/", "Avator");

            if (!Directory.Exists(fileFolder))
                Directory.CreateDirectory(fileFolder);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileName = DateTime.Now.ToString("yyyyMMddHHmmss") +nick_name+Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(fileFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    //地址存入信息
                    user.Avatr = fileName;
                    entity.Entry(user).State = EntityState.Modified;
                    entity.SaveChanges();
                    flag = 1;
                }
                
            }

            return Ok(new { UploadFlag = flag });
        }*/

        /// <summary>
        /// 上传头像( 注：修改了接口名字）
        /// </summary>
        /// <param name="uploadedPhoto">图片文件</param>
        /// <param name="nick_name">用户名</param>
        /// <response code="200">成功</response>
        /// <returns></returns>
        /// <remarks>
        ///     返回：
        ///     {uploadPaths = JsonConvert.SerializeObject(allFilePath), createPhotoFlag = createPhotoFlag}
        /// </remarks>
        
        [HttpPost]
        public ActionResult createAvatorByName(IFormFileCollection uploadedPhoto, string nick_name)
        {
            DateTime dateTime = DateTime.Now;
            var msg = "";
            int createPhotoFlag = 0;
            string uploadsFolder = "";
            string uniqueFileName = "";
            string filePath = "";
            //string root = "http:///139.224.255.43:7779/";
            List<string> allFilePath = new List<string>();
            //List<string> allPhotoID = new List<string>();

            if (uploadedPhoto == null) return BadRequest();

            //找到用户
            //int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                createPhotoFlag = 2; //没有找到该用户
                //Response.StatusCode = 202;//没有该用户
                return Json(new { UploadFlag = createPhotoFlag });
            }
            var user = entity.User.Find(id); //在数据库中根据key找到相应记录


            for (int i = 0; i < uploadedPhoto.Count; i++)
            {
                try
                {
                    //fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + nick_name + Path.GetExtension(file.FileName);
                    //filePath = Path.Combine(fileFolder, fileName);


                    //uploadsFolder = Path.Combine(PicsRootPath, userID, albumID); //计算存储路径
                    uploadsFolder = Path.Combine(PicsRootPath);
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + uploadedPhoto[i].FileName;
                    filePath =Path.Combine(uploadsFolder, uniqueFileName);
                   // string myPath = Path.Combine(root, filePath);

                    /* Photo photo = new Photo();
                     photo.AlbumId = int.Parse(albumID);
                     photo.PhotoLikes = 0;
                     photo.VisitNum = 0;
                     photo.PhotoAddress = filePath;
                     photo.PhotoUploadTime = dateTime;
                     photo.UserId = int.Parse(userID);*/

                   

                    user.Avatr = filePath;
                    entity.Entry(user).State = EntityState.Modified;
                    entity.SaveChanges();
                    //flag = 1;
                    /*entity.Photo.Add(photo); //将图片信息添加到数据库中
                    entity.SaveChanges();
                    entity.Entry(photo); //获取新插入的photo的photoID*/

                    uploadedPhoto[i].CopyTo(new FileStream(filePath, FileMode.Create)); //存储图片到本地 持久化

                    allFilePath.Add(filePath);
                    //allPhotoID.Add(photo.PhotoId.ToString());

                    //Response.StatusCode = 200;
                    createPhotoFlag = 1;
                }
                catch (Exception e)
                {
                    Response.StatusCode = 403;
                    createPhotoFlag = 0;
                    msg = e.Message;
                    Console.WriteLine(e.Message);
                }
            }

            var resultJson = new
            {
                uploadPaths = JsonConvert.SerializeObject(allFilePath),
                createPhotoFlag = createPhotoFlag,
                //getmsg = msg,
            };

            return Json(resultJson);
        }




        /// <summary>
        /// 返回头像,用户名错误/没设头像 返回默认头像
        /// </summary>
        /// <param name="nick_name">用户名</param>
        /// <returns>图片</returns>
       
        [HttpPost]
        //[Route("file/image/{width}/{name}")]
        // <param name="width">所访问图片的宽度,高度自动缩放,大于原图尺寸或者小于等于0返回原图</param>
        public IActionResult getAvatrResource(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                flag = 2; //没有找到该用户
                //Response.StatusCode = 202;//没有该用户
                return Json(new { UploadFlag = flag });
            }
            var user = entity.User.Find(id); //在数据库中根据key找到相应记录

            var name = user.Avatr;

            //var appPath = AppContext.BaseDirectory.Split("\\bin\\")[0] + "/image/";
            flag = 1;
            //var appPath =  "../BlogPics/Avator";
            //var errorImage = "http://139.224.255.43:7779/"+PicsRootPath +"\\default\\"+ "defaultAvator.png";//没有找到图片
            //var imgPath = string.IsNullOrEmpty(name) ? errorImage : "http://139.224.255.43:7779/" + name;

            var errorImage = PicsRootPath + "\\default\\" + "defaultAvator.png";//没有找到图片
            var imgPath = string.IsNullOrEmpty(name) ? errorImage : name;

            //var imgPath = appPath + "/"+name;
            //获取图片的返回类型
            return Json(new { Flag = flag,Path = imgPath });
            /*var contentTypDict = new Dictionary<string, string> {
                {"jpg","image/jpeg"},
                {"jpeg","image/jpeg"},
                {"jpe","image/jpeg"},
                {"png","image/png"},
                {"gif","image/gif"},
                {"ico","image/x-ico"},
                {"tif","image/tiff"},
                {"tiff","image/tiff"},
                {"fax","image/fax"},
                {"wbmp","image//vnd.wap.wbmp"},
                {"rp","image/vnd.rn-realpix"}
            };
            var contentTypeStr = "image/jpeg";
            var imgTypeSplit = name.Split('.');
            var imgType = imgTypeSplit[imgTypeSplit.Length - 1].ToLower();
            //未知的图片类型
            if (!contentTypDict.ContainsKey(imgType))
            {
                imgPath = errorImage;
            }
            else
            {
                contentTypeStr = contentTypDict[imgType];
            }
            //图片不存在
            if (!new FileInfo(imgPath).Exists)
            {
                imgPath = errorImage;
            }
            //原图
            if (width <= 0)
            {
                using (var sw = new FileStream(imgPath, FileMode.Open))
                {
                    var bytes = new byte[sw.Length];
                    sw.Read(bytes, 0, bytes.Length);
                    sw.Close();
                    return new FileContentResult(bytes, contentTypeStr);
                }
            }
            //缩小图片
            using (var imgBmp = new Bitmap(imgPath))
            {
                //找到新尺寸
                var oWidth = imgBmp.Width;
                var oHeight = imgBmp.Height;
                var height = oHeight;
                if (width > oWidth)
                {
                    width = oWidth;
                }
                else
                {
                    height = width * oHeight / oWidth;
                }
                var newImg = new Bitmap(imgBmp, width, height);
                newImg.SetResolution(72, 72);
                var ms = new MemoryStream();
                newImg.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                var bytes = ms.GetBuffer();
                ms.Close();
                return new FileContentResult(bytes, contentTypeStr);
            }
        }*/


            /*

            /// <summary>
            /// 返回用户头像（存储的地址）
            /// </summary>
            /// <param name="nick_name">用户名</param>
            /// <returns></returns>
            /// <remarks>
            ///     返回实例
            ///     {
            ///         ReturnFlag = flag, 
            ///         UserAvatr = user.Avatr
            ///     }
            ///     
            ///     flag:
            ///     0: 未执行
            ///     1：成功
            ///     2：没有找到该用户
            /// </remarks>
            [HttpPost]
            public JsonResult getAvatrResource(string nick_name)
            {
                var userid =
                        (from c in entity.User
                         where c.NickName == nick_name
                         select c.UserId).Distinct();
                var id = userid.FirstOrDefault();
                var user = entity.User.Find(id); //在数据库中根据key找到相应记录
                var flag = 0;
                if (user != default)
                    flag = 2; //寻找用户失败
                else
                    flag = 1; //寻找成功
                return Json(new { ReturnFlag = flag, UserAvatr = user.Avatr });
            */
        }




        /// <summary>
        /// 查询用户所有基本信息
        /// </summary>
        /// <param name="nick_name">用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///         uploadFlag = flag,
        ///         userInfo = user
        ///     }
        ///     flag:
        ///     0: 未执行
        ///     1：成功
        ///     2：没有找到该用户
        /// </remarks>
        [HttpPost]
        public JsonResult getUserInfoByNickName(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                flag = 2; //没有找到该用户
                //Response.StatusCode = 404;//没有该用户
                return Json(new {  UploadFlag = flag });
            }
            var user = entity.User.Find(id); //在数据库中根据key找到相应记录
            flag = 1;//找到该用户
           // Response.StatusCode = 200;//成功
            return Json(new { uploadFlag = flag, userInfo = user });
        }







        /// <summary>
        /// 添加/更新 公告内容
        /// </summary>
        /// <param name="nick_name">用户名</param>
        /// <param name="content">公告内容</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///         Userid = id,
        ///         CreateFlag = flag 
        ///     }
        /// 
        ///     flag:
        ///     0: 未执行
        ///     1：成功
        ///     2：没有找到该用户
        /// </remarks>
        [HttpPost]
        public JsonResult createAnnouncementByNickName(string nick_name,string content)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();

            if (id == default)
            {
                flag = 2; //没有找到该用户
                //Response.StatusCode = 404;//没有找到该用户
            }
            else
            {
                var announcementRecordID =
                (from c in entity.Announcement
                 where c.UserId == id
                 select c.AnnouncementId).Distinct(); //根据userid找到记录(userid不是主码，不能直接find)
                var announcement = entity.Announcement.Find(announcementRecordID.FirstOrDefault()); //在数据库中根据key找到相应记录

                if (announcement == null)
                {
                    flag = 1; //该用户没有添加过公告，新建公告
                    var announcement2 = new Announcement();
                    announcement2.UserId = id;
                    //announcement2.AnnouncementId = (++idForAnnouncement).ToString();
                    //这里id也会自增
                    announcement2.AnnouncementContent = content;
                    announcement2.AnnouncementUploadTime = DateTime.Now;
                    entity.Announcement.Add(announcement2); //把announcement这个实例加入数据库
                    entity.SaveChanges();

                    //Response.StatusCode = 200;//成功新建
                }
                else
                {
                    //该用户已经创建过公告，需要rewrite
                    flag = 1;
                    //announcement.UserId = id;
                    //announcement.AnnouncementId = (++idForAnnouncement).ToString(); 不修改公告id
                    announcement.AnnouncementContent = content;
                    announcement.AnnouncementUploadTime = DateTime.Now;

                    entity.Entry(announcement).State = EntityState.Modified;
                    entity.SaveChanges();

                    //Response.StatusCode = 200;//成功重写公告
                }

               
            }
            return Json(new { Userid = id,CreateFlag = flag });
        }

        /// <summary>
        /// 删除公告
        /// </summary>
        /// <param name="nick_name">用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     
        ///     {
        ///         DeleteFlag = flag
        ///     }
        ///     
        ///     flag:
        ///     
        ///     0: 未执行
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：该用户没有创建过公告
        /// </remarks>
        [HttpPost]
        public JsonResult deleteAnnouncementByNickName(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                flag = 2; //没有找到该用户
                //Response.StatusCode = 404;//没有找到该用户
                return Json(new { DeleteFlag = flag });
            }
            var announcementRecordID =
                (from c in entity.Announcement
                 where c.UserId == id
                 select c.AnnouncementId).Distinct(); //根据userid找到记录(userid不是主码，不能直接find)
            var announcement = entity.Announcement.Find(announcementRecordID.FirstOrDefault()); //在数据库中根据key找到相应记录
            if (announcement == null)
            {
                flag = 3;//该用户没有创建过公告
               // Response.StatusCode = 403;//该用户没有创建过公告
                return Json(new { DeleteFlag = flag });
            }


            /*announcement.UserId = null;
            announcement.AnnouncementUploadTime = null;
            announcement.AnnouncementContent = null;*/

            entity.Entry(announcement).State = EntityState.Deleted;//删除该项
            entity.SaveChanges();

            flag = 1;
            //Response.StatusCode = 200;//成功删除公告
            return Json(new { DeleteFlag = flag });
        }

        /// <summary>
        /// 获取用户公告内容
        /// </summary>
        /// <param name="nick_name">用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///         GetFlag = flag,
        ///         content = announcement
        ///     }
        /// 
        ///     flag:
        ///     0: 未执行
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：该用户没有创建过公告
        /// </remarks>
        [HttpPost]
        public JsonResult getAnnouncementByNickName(string nick_name)
        {
            var flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                flag = 2; //没有找到该用户
                //Response.StatusCode = 404;//没有找到该用户
                return Json(new { GetFlag = flag });
                //return null;
            }
            var announcementRecordID =
                (from c in entity.Announcement
                 where c.UserId == id
                 select c.AnnouncementId).Distinct(); //根据userid找到记录(userid不是主码，不能直接find)
            if (announcementRecordID == null)
            {
                flag = 3;//该用户没有创建的公告，无法返回
                //Response.StatusCode = 403;

                //return null;
                return Json(new { GetFlag = flag });
            }
            flag = 1;
            var announcement = entity.Announcement.Find(announcementRecordID.FirstOrDefault());
            //Response.StatusCode = 200;//成功返回
            return Json(new { GetFlag = flag, content = announcement });
        }


        /// <summary>
        /// 创建关注关系
        /// </summary>
        /// <param name="nameOfBlogger">被关注者</param>
        /// <param name="nameOfFans">关注者</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容:
        ///     {
        ///         CreateFlag = flag
        ///     }
        ///     
        ///     flag:
        ///     0: 未执行
        ///     1：成功
        ///     2：两个用户都不存在
        ///     3：博主用户不存在
        ///     4：粉丝用户不存在
        /// </remarks>
        [HttpPost]
        public JsonResult createFollowByNickNames(string nameOfBlogger,string nameOfFans)
        {
            int flag = 0;
            var BloggerId =
                    (from c in entity.User
                     where c.NickName == nameOfBlogger
                     select c.UserId).Distinct();
            var id_1 = BloggerId.FirstOrDefault();
            var FansId =
                    (from c in entity.User
                     where c.NickName == nameOfFans
                     select c.UserId).Distinct();
            var id_2 = FansId.FirstOrDefault();
            if (id_1 == default || id_2 ==default)
            {
                if (id_1 == default && id_2 == default)
                    flag = 2; //两个用户都不存在
                else if (id_1 == default)
                    flag = 3; //博主用户不存在
                else
                    flag = 4; //粉丝用户不存在

               // Response.StatusCode = 404;//用户不存在
                return Json(new { CreateFlag = flag });
            }

            var newFollow = new UserFollow{ ActiveUserId = id_2, PassiveUserId = id_1 };
            entity.UserFollow.Add(newFollow); 
            entity.SaveChanges();

            var user = entity.User.Find(id_1);  //user里面更新被关注数量
            var num = user.FollowNum;
            if (num == default)
                user.FollowNum = 1;
            else
                user.FollowNum = num + 1;
            entity.Entry(user).State = EntityState.Modified;
            entity.SaveChanges();


            //Response.StatusCode = 200;//关注成功
            flag = 1;//成功
            return Json(new { CreateFlag = flag});
        }

        /// <summary>
        /// 删除关注关系
        /// </summary>
        /// <param name="nameOfBlogger">被关注者</param>
        /// <param name="nameOfFans">关注者</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回消息内容:
        ///     {
        ///         GetFlag = flag 
        ///     }
        ///     
        ///     flag：
        ///     0: 未执行
        ///     1：成功
        ///     2：两个用户都不存在
        ///     3：博主用户不存在
        ///     4：粉丝用户不存在
        /// </remarks>
        [HttpPost]
        public JsonResult deleteFollowByNickName(string nameOfBlogger, string nameOfFans)
        {
            int flag = 0;
            var BloggerId =
                    (from c in entity.User
                     where c.NickName == nameOfBlogger
                     select c.UserId).Distinct();
            var id_1 = BloggerId.FirstOrDefault();
            var FansId =
                    (from c in entity.User
                     where c.NickName == nameOfFans
                     select c.UserId).Distinct();
            var id_2 = FansId.FirstOrDefault();
            if (id_1 == default || id_2 == default)
            {
                if (id_1 == default && id_2 == default)
                    flag = 2; //两个用户都不存在
                else if (id_1 == default)
                    flag = 3; //博主用户不存在
                else
                    flag = 4; //粉丝用户不存在

                //Response.StatusCode = 404;//用户不存在
                return Json(new { GetFlag = flag });
            }

            var follow = entity.UserFollow.Find(id_2,id_1);
            entity.Entry(follow).State = EntityState.Deleted;//删除该项
            entity.SaveChanges();
           // Response.StatusCode = 200;//成功
            flag = 1; //成功
            return Json(new { GetFlag = flag });

        }


        struct FanInfo
        {
            public string FanName;
            public string FanAvator;
        }
        /*
        /// <summary>
        /// 返回粉丝列表
        /// </summary>
        /// <param name="nick_name">博主用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容:
        ///     {
        ///         returnFlag = flag,
        ///         fansList = fansName
        ///     }
        ///     
        ///     flag:
        ///     0: 未执行
        ///     1：成功
        ///     2：用户寻找失败
        ///     3：该用户没有粉丝
        ///     
        /// </remarks>
        [HttpPost]
        public JsonResult getFansListByNickName(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            //var user = entity.User.Find(id); //在数据库中根据key找到相应记录
            if (id ==default )
            {
                flag = 2;//用户寻找失败
                return Json(new { returnFlag = flag });
            }
            var fansID =
                (from u in entity.UserFollow
                 where u.PassiveUserId == id
                 select u.ActiveUserId).Distinct();

            var fansID =
                (from u in entity.UserFollow
                 where u.PassiveUserId == id
                 select u.ActiveUserId).ToList();
            //Dictionary<string, string> userList = new Dictionary<string, string>();

            

            foreach (var fan in fansID)
            {
                var info =
                    (from u in entity.User
                     where u.UserId == fan
                     select new { u.NickName, u.Avatr } ).Distinct(); //查出用户名和头像

                userList.Add("Username", name.FirstOrDefault());
            }


            if (fansID.FirstOrDefault()==default)
            {
                flag = 3;//该用户没有粉丝
                return Json(new { returnFlag = flag });
            }
            flag = 1;
            //Response.StatusCode = 200;
            return Json(new { returnFlag = flag,fansList = fansID});
        }


        /// <summary>
        /// 返回粉丝列表(xiugai)
        /// </summary>
        /// <param name="nick_name">博主用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容:
        ///     {
        ///         returnFlag = flag,
        ///         fansList = fansName
        ///     }
        ///     
        ///     flag:
        ///     0: 未执行
        ///     1：成功
        ///     2：用户寻找失败
        ///     3：该用户没有粉丝
        ///     
        /// </remarks>
        [HttpPost]
        public JsonResult getFansListByNickName2(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            //var user = entity.User.Find(id); //在数据库中根据key找到相应记录
            if (id == default)
            {
                flag = 2;//用户寻找失败
                return Json(new { returnFlag = flag });
            }
            var fansID =
                (from u in entity.UserFollow
                 where u.PassiveUserId == id
                 select u.ActiveUserId).Distinct();

            var fansID =
                (from u in entity.UserFollow
                 where u.PassiveUserId == id
                 select u.ActiveUserId).ToList();
            Dictionary<string, string> userList = new Dictionary<string, string>();
            if (fansID.FirstOrDefault() == default)
            {
                flag = 3;//该用户没有粉丝
                return Json(new { returnFlag = flag });
            }

            //Array list = [];
           // FanInfo[] list = new FanInfo[100];
            var cnt = 0;
            List<FanInfo> FansList = new List<FanInfo>();

            // foreach (var fan in fansID)
            //{
            cnt++;
            var fan = fansID.First();
                //var fans = fansID.FirstOrDefault();
                var name =
                    (from u in entity.User
                     where u.UserId == fan
                     select u.NickName).Distinct(); //查出用户名
                var Name = name.FirstOrDefault();
                var avator =
                    (from u in entity.User
                     where u.UserId == fan
                     select u.Avatr).Distinct(); //头像
                var Avator = avator.FirstOrDefault();
            if (Name == default)
            {
                flag = 4; //break; }
            }

                //userList.Add("Fan" + cnt, new {Name,Avator });

                FanInfo item = new FanInfo();
                item.FanName = Name;
                item.FanAvator = Avator;

                FansList.Add(item);
            //list[cnt] = item;
               // userList.Add("Username", name.FirstOrDefault());
           // }


            
            flag = 1;
            //Response.StatusCode = 200;
            return Json(new { returnFlag = flag, fansList = FansList,Thename = item.FanName, theA = item.FanAvator,ITEM= item });
        }*/

        /// <summary>
        /// 返回粉丝列表
        /// </summary>
        /// <param name="nick_name">博主用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容:(成功时）
        ///     {
        ///         returnFlag = flag,
        ///         fansList = {Name,Avator}
        ///     }
        ///     
        ///     flag:
        ///     
        ///     0: 未执行
        ///     
        ///     1：成功
        ///     
        ///         返回：{ returnFlag = flag, List = {Name,Avator} }
        ///     
        ///     2：用户寻找失败
        ///     
        ///         返回：{ returnFlag = flag }
        ///     
        ///     3：该用户没有粉丝
        ///     
        ///         返回：{ returnFlag = flag }
        ///     
        /// </remarks>
        [HttpPost]
        public JsonResult getFansListByNickName(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            //var user = entity.User.Find(id); //在数据库中根据key找到相应记录
            if (id == default)
            {
                flag = 2;//用户寻找失败
                return Json(new { returnFlag = flag });
            }
            /*var fansID =
                (from u in entity.UserFollow
                 where u.PassiveUserId == id
                 select u.ActiveUserId).Distinct();*/

            var fansID =
                (from  u in entity.UserFollow
                            join right in entity.User
                            on u.ActiveUserId equals right.UserId
                 where u.PassiveUserId == id
                 select new { Name = right.NickName,Avator  = right.Avatr}).Distinct();
            if(fansID.FirstOrDefault() == default)
            {
                flag = 3;//该用户没有粉丝
                return Json(new { returnFlag = flag });
            }

            flag = 1;
            //Response.StatusCode = 200;
            // return Json(new { returnFlag = flag, fansList = FansList, Thename = item.FanName, theA = item.FanAvator, ITEM = item });
            return Json(new { returnFlag = flag, List = fansID });
        
        }



        /*
        /// <summary>
        /// 返回关注的博主列表
        /// </summary>
        /// <param name="nick_name">博主用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容:
        ///     {
        ///         returnFlag = flag, 
        ///         FollowList =  followID
        ///     }
        ///     
        ///     flag:
        ///     
        ///     0: 未执行
        ///     
        ///     1：成功
        ///         返回：{ returnFlag = flag, List = fansID }
        ///     
        ///     2：没有该用户
        ///         返回：{ returnFlag = flag }
        ///     
        ///     3：该用户关注的博主为空
        ///     
        ///         返回：{ returnFlag = flag }
        /// </remarks>
        [HttpPost]
        public JsonResult getFollowListByNickName(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if(id == default)
            {
                flag = 2;//没有该用户
                return Json(new{ returnFlag = flag});
            }
            //var user = entity.User.Find(id); //在数据库中根据key找到相应记录

            var followID =
                (from u in entity.UserFollow
                 where u.ActiveUserId == id
                 select u.PassiveUserId).Distinct();
            if (followID == default)
            {
                flag = 3;//该用户关注的博主为空
            }
            else
                flag = 1;//成功
            //Response.StatusCode = 200; //成功
            return Json(new { returnFlag = flag,FollowList =  followID });
        }*/

        /// <summary>
        /// 返回关注的博主列表
        /// </summary>
        /// <param name="nick_name">博主用户名</param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容:
        ///     {
        ///         returnFlag = flag, 
        ///         FollowList =  {Name, Avator}
        ///     }
        ///     
        ///     flag:
        ///     
        ///     0: 未执行
        ///     
        ///     1：成功
        ///     
        ///         返回：{returnFlag = flag, FollowList ={Name, Avator}}
        ///     
        ///     2：没有该用户
        ///     
        ///         返回：{ returnFlag = flag }
        ///     
        ///     3：该用户关注的博主为空
        ///     
        ///         返回：{ returnFlag = flag }
        /// </remarks>
        [HttpPost]
        public JsonResult getFollowListByNickName(string nick_name)
        {
            int flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                flag = 2;//没有该用户
                return Json(new { returnFlag = flag });
            }
            //var user = entity.User.Find(id); //在数据库中根据key找到相应记录

            /*
             var fansID =
                (from  u in entity.UserFollow
                            join right in entity.User
                            on u.ActiveUserId equals right.UserId
                 where u.PassiveUserId == id
                 select new { Name = right.NickName,Avator  = right.Avatr}).Distinct();
            if(fansID.FirstOrDefault() == default)
            {
                flag = 3;//该用户没有粉丝
                return Json(new { returnFlag = flag });
            }
             */

            var followID =
                (from u in entity.UserFollow
                            join right in entity.User
                            on u.PassiveUserId equals right.UserId
                 where u.ActiveUserId == id
                 select new { Name = right.NickName,Avator = right.Avatr}).Distinct();
            if (followID.FirstOrDefault() == default)
            {
                flag = 3;//该用户关注的博主为空
                return Json(new { returnFlag = flag });
            }
            
            
                flag = 1;//成功
            //Response.StatusCode = 200; //成功
            return Json(new { returnFlag = flag, FollowList = followID });
        }



    }    



}
    

