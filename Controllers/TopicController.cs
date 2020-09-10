using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using Temperature.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860


namespace Temperature.Controllers {
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiController]
    public class TopicController : Controller {
        blogContext entity = new blogContext();

        /// <summary>
        /// 创建话题回复
        /// </summary>
        /// <param name="content"></param>
        /// <param name="topicID"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [HttpPost]
        public ActionResult createTopicAnswerByID(string content, string topicID, string userID, string parentID = "-1") {
            DateTime dateTime = DateTime.Now; //获取当前时间
            int cerateTopicAnswerFlag = 0;
            try {
                //新建answer
                TopicAnswerReply topicAnswerReply = new TopicAnswerReply {
                    TopicId = int.Parse(topicID),
                    AnswerLikes = 0,
                    AnswerContent = content,
                    UserId = int.Parse(userID),
                    AnswerUploadTime = dateTime,
                    ParentAnswerId = int.Parse(parentID),
                };
                entity.TopicAnswerReply.Add(topicAnswerReply);
                int nums = entity.SaveChanges();

                //更新相应topic answer数量
                Topic topic = entity.Topic.Single(c => c.TopicId == int.Parse(topicID));
                topic.AnswerNum += 1;
                entity.Topic.Update(topic);
                entity.SaveChanges();
                

                cerateTopicAnswerFlag = 1;
            } catch (Exception e) {
                cerateTopicAnswerFlag = 0;
                Console.WriteLine(e.Message);
            }

            return Json(new { cerateTopicAnswerFlag = cerateTopicAnswerFlag });
        }

        /// <summary>
        /// 创建话题
        /// </summary>
        /// <param name="content"></param>
        /// <param name="title"></param>
        /// <param name="userID"></param>
        /// <param name="zoneID"></param>
        /// <response code="200">成功</response>
        /// <response code="403">无法创建</response>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [SwaggerResponse(200, "文档注释", typeof(Json))]
        [HttpPost]
        public ActionResult createTopicByID(string content, string title, string userID, string zoneID) {
            DateTime dateTime = DateTime.Now; //获取当前时间
            Topic topic = new Topic(); //新建topic条目项
            int createTopicFlag = 0;

            //向条目中插入数据
            topic.TopicContent = content;
            topic.TopicTitle = title;
            topic.AnswerNum = 0;
            topic.UserId = int.Parse(userID);
            topic.TopicUploadTime = dateTime;
            topic.ZoneId = int.Parse(zoneID);

            entity.Add(topic);
            try {
                createTopicFlag = entity.SaveChanges(); //存入数据库 返回值为1表示成功
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }


            return Json(new { createTopicFlag = createTopicFlag });
        }


        /// <summary>
        /// 根据ID删除话题评论
        /// </summary>
        /// <param name="answerID"></param>
        /// <response code="200">成功</response>
        /// <response code="403">无法删除，出现错误/异常</response>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [SwaggerResponse(200, "文档注释")]
        [HttpPost]
        public ActionResult deleteTopicAnswerByID(string answerID) {
            int deleteTopicAnswerFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();

            try {
                TopicAnswerReply topicAnswerReply = entity.TopicAnswerReply.Single(c => c.TopicAnswerId == int.Parse(answerID)); //查找
                entity.TopicAnswerReply.Remove(topicAnswerReply); //建立删除语句
                entity.SaveChanges(); //提交删除
                deleteTopicAnswerFlag = 1; //成功

                returnJson.Add("deleteTopicAnswerID", topicAnswerReply.TopicAnswerId.ToString());
            } catch (Exception e) {
                deleteTopicAnswerFlag = 0; //失败

            } finally {
                returnJson.Add("deleteTopicAnswerFlag", deleteTopicAnswerFlag.ToString());

            }
            return Json(returnJson);
        }

        /// <summary>
        /// 根据用户ID删除相应用户
        /// </summary>
        /// <param name="topicID"></param>
        /// <response code="200">成功</response>
        /// <response code="403">无法删除，出现错误/异常</response>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///         topicID : topidID
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult deleteTopicByID(string topicID) {
            int deleteTopicFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();

            try {
                Topic topic = entity.Topic.Single(c => c.TopicId == int.Parse(topicID));
                entity.Topic.Remove(topic);
                entity.SaveChanges();

                //更新分区topic数量
                var zone = entity.Zone.Single(c => c.ZoneId == topic.ZoneId);
                zone.ZoneTopicNum -= 1;
                entity.Zone.Update(zone);
                entity.SaveChanges();

                returnJson.Add("deleteTopic", topic.TopicId.ToString());
            } catch (Exception e) {
                deleteTopicFlag = 0; //失败

            } finally {
                returnJson.Add("deleteTopicAnswerFlag", deleteTopicFlag.ToString());
            }
            return Json(returnJson);
        }

        //[HttpPost]
        //public void getBriefAnnouncementByID(int pageNum, int pageSize) {

        //}

        /// <summary>
        /// 分页获取topic
        /// </summary>
        /// <param name="pageNum">页号</param>
        /// <param name="pageSize">每页大小</param>
        /// <response code="200">成功</response>
        /// <response code="403">无法获取，出现错误/异常</response>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   Result : [
        ///     {topicID:, topicContent:, answerNum:, userID:, topicUpdateTime:, zoneID:},
        ///     {......}
        /// ]
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [SwaggerResponse(200, "文档注释", typeof(Topic))]
        [HttpPost]
        public JsonResult getTopicByPage(int pageNum, int pageSize, string zoneID) {
            int getTopicFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");

            try {
                var content = (from c in entity.Topic
                               where c.ZoneId == int.Parse(zoneID)
                               select c).Skip((pageNum - 1) * pageSize).Take(pageSize);

                string contentJson = JsonConvert.SerializeObject(content); //序列化对象
                returnJson["Result"] = contentJson;
                getTopicFlag = 1;

            } catch(Exception e) {
                getTopicFlag = 0;

            } finally {
                returnJson.Add("getTopicFlag", getTopicFlag.ToString());
            }
            return Json(returnJson);
        }

        /// <summary>
        /// 获取topicID话题下的评论
        /// </summary>
        /// <param name="topicID"></param>
        /// <response code="200">成功</response>
        /// <response code="403">无法获取，出现错误/异常</response>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   Result:{
        ///       {topicAnswerID:, topicID:, answerLikes:, answerContent:, userID:, answerUploadTime:, parentAnswerID:}
        /// }
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult getTopicCommentByID(string topicID) {
            int getTopicCommentFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");

            try {
                var content = (from c in entity.TopicAnswerReply
                               where c.TopicId == int.Parse(topicID)
                               select c);
                string contentJson = JsonConvert.SerializeObject(content);
                returnJson["Result"] = contentJson;

            } catch(Exception e) {
                getTopicCommentFlag = 0;

            } finally {
                returnJson.Add("getTopicCommentFlag", getTopicCommentFlag.ToString());
            }
            return Json(returnJson);
        }


        /// <summary>
        /// 根据zoneID返回topic总共的数量
        /// </summary>
        /// <param name="zoneID"></param>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   totalNumber: ,
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult getTopicNumberByZoneID(string zoneID) {
            int getTopicNumberFlag = 0;
            int totalNumber = -1;

            try {
                var topic = (from c in entity.Topic
                             where c.ZoneId == int.Parse(zoneID)
                             select c);
                totalNumber = topic.Count();
                getTopicNumberFlag = 1;
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                getTopicNumberFlag = 0;
            }
            return Json(new { getTopicNumberFlag = getTopicNumberFlag, totalNumber = totalNumber });
        }

        /// <summary>
        /// 获取最新发布的topic
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   topics:{
        ///       {topicAnswerID:, topicID:, answerLikes:, answerContent:, userID:, answerUploadTime:, parentAnswerID:}
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult getNewestTopic(int takeTopicNum) {
            int flag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();

            try {
                var content = entity.Topic.OrderByDescending(c => c.TopicUploadTime).Take(takeTopicNum);

                returnJson.Add("topics", JsonConvert.SerializeObject(content));
                flag = 1;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                flag = 0;
            }

            returnJson.Add("flag", flag.ToString());
            return Json(returnJson);
        }

        /// <summary>
        /// 获取最热的topic
        /// </summary>
        /// <param name="takeTopicNum"></param>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   topics:{
        ///       {topicAnswerID:, topicID:, answerLikes:, answerContent:, userID:, answerUploadTime:, parentAnswerID:}
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult getHotestTopic(int takeTopicNum) {
            int flag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();

            try {
                var content = entity.Topic.OrderByDescending(c => c.AnswerNum).Take(takeTopicNum);

                returnJson.Add("topics", JsonConvert.SerializeObject(content));
                flag = 1;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                flag = 0;
            }

            returnJson.Add("flag", flag.ToString());
            return Json(returnJson);
        }

        /// <summary>
        /// 获取相应topicID的内容、评论、评论者信息
        /// </summary>
        /// <param name="topicID"></param>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1
        ///   answerUserList: [
        ///   {"topicInfo":"{"TopicAnswerID":1,"TopicID":3,"AnswerLikes":2,"Content":"hello","UserID":3,"UploadTime":"2020-09-09T11:28:07","ParentAnswerID":-1}","userComments":[]},
        ///   {......},
        ///     ],
        ///   失败：flag = 2
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult getTopicDetailByID(string topicID) {
            int flag = 0;
            List<string> answerUserList = new List<string>();

            try {
                Topic topic = entity.Topic.Single(c => c.TopicId == int.Parse(topicID));

                //选取此topic的所有一级评论
                var topicAnswer = (from c in entity.TopicAnswerReply
                                   where c.TopicId == topic.TopicId && c.ParentAnswerId == -1
                                   select new {
                                       TopicAnswerID = c.TopicAnswerId,
                                       TopicID = c.TopicId,
                                       AnswerLikes = c.AnswerLikes,
                                       Content = c.AnswerContent,
                                       UserID = c.UserId,
                                       UploadTime = c.AnswerUploadTime,
                                       ParentAnswerID = c.ParentAnswerId
                                   });

                //每个一级评论中
                foreach(var t in topicAnswer.ToList()) {
                    var a = new { topicInfo = JsonConvert.SerializeObject(t),
                                  userComments = new List<string>()};

                    var allSecondLevelAnswer = (from c in entity.TopicAnswerReply
                                                where c.ParentAnswerId == t.TopicAnswerID
                                                select new {
                                                    TopicAnswerID = c.TopicAnswerId,
                                                    TopicID = c.TopicId,
                                                    AnswerLikes = c.AnswerLikes,
                                                    Content = c.AnswerContent,
                                                    UserID = c.UserId,
                                                    UploadTime = c.AnswerUploadTime,
                                                    ParentAnswerID = c.ParentAnswerId
                                                });
                    foreach (var tt in allSecondLevelAnswer.ToList()) {
                        var answerUser2 = (from c in entity.User
                                          where c.UserId == tt.UserID
                                          select new {
                                               userID = c.UserId,
                                               nickName = c.NickName,
                                               avatr = c.Avatr,
                                               gender = c.Gender
                                           }).FirstOrDefault();
                        var aa = new { userComment = JsonConvert.SerializeObject(tt),
                                       userInfo = JsonConvert.SerializeObject(answerUser2) };
                        a.userComments.Add(JsonConvert.SerializeObject(aa) );
                    }
                    answerUserList.Add(JsonConvert.SerializeObject(a) );
                }
                flag = 1;
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                flag = 0;
            }
            var returnJson = new { answerUserList = answerUserList,
                                   flag = flag};

            return Json(returnJson);
        }

        /// <summary>
        /// 返回用户发表的所有话题数量
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1,
        ///   topicCount = 1,
        ///   
        ///   失败：flag = 0
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult getUserTopicNum(string userID) {
            int flag = 0;
            int count = -1;

            try {
                count = (from r in entity.Topic
                         where r.UserId == int.Parse(userID)
                         select r).Count();
                flag = 1;
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                flag = 0;
            }

            var returnJson = new {
                topicCount = count,
                flag = flag
            };

            return Json(returnJson);
        }

        /// <summary>
        /// 返回用户获得的所有回答数量
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        /// <remarks>
        /// return {
        ///   成功：flag = 1,
        ///   userTopicAnswerCount = 1,
        ///   
        ///   失败：flag = 0
        /// }
        /// </remarks>
        [HttpPost]
        public JsonResult getAnswerNumOfUser(string userID) {
            int flag = 0;
            int count = -1;

            try {
                var content = (from r in entity.Topic
                               where r.UserId == int.Parse(userID)
                               select r);

                count = 0;
                foreach(var t in content) {
                    count += (int)t.AnswerNum;
                }

                flag = 1;
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                flag = 0;
            }

            var returnJson = new { 
                userTopicAnswerCount = count,
                flag = flag
            };
            return Json(returnJson);
        }

        /// <summary>
        /// 返回某个topic的详情信息
        /// </summary>
        /// <param name="topicID"></param>
        /// <returns></returns>
        /// <remarks>
        /// {
        ///    "topicDetail": {
        ///        "topicId": 2,
        ///        "topicContent": "222222222222222222222222",
        ///        "answerNum": 5,
        ///        "userId": 2,
        ///        "topicUploadTime": "2020-08-26T14:34:00",
        ///        "zoneId": 3,
        ///        "topicTitle": null,
        ///        "user": null,
        ///        "zone": null,
        ///        "topicAnswerReply": [],
        ///        "topicRank": []
        ///    },
        ///    "flag": 1
        ///}
        /// </remarks>
        [HttpPost]
        public JsonResult getSingleTopicDetail(string topicID) {
            int flag = 0;
            try {
                var content = entity.Topic.Single(c => c.TopicId == int.Parse(topicID));
                flag = 1;

                return Json(new { topicDetail = content, flag = flag });
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                flag = 0;
                return Json(new { flag = flag });
            }
        }
    }
}