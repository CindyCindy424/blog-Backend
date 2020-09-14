﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;
using Microsoft.EntityFrameworkCore;
using Temperature.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;

//20200907 订正后的articlecontroller
namespace Temperature.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiController]
    //[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]


    public class ArticleController : Controller
    {
        private blogContext entity = new blogContext(); //整体数据库类型
        /// <summary>
        /// 创建文章
        /// 创建文章名为nick_name用户创建一篇题目为title内容为content的文章
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="zoneid"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///             ReturnFlag = flag,
        ///             UserName = nick_name, 
        ///             Article_Name = title, 
        ///             result = result
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：该用户已存在同名文章
        /// </remarks>
        [HttpPost]
        public JsonResult createArticleByNickName(string nick_name, string title, string content,int zoneid)
        {
            var flag = 0;
            var userid =
                   (from c in entity.User
                    where c.NickName == nick_name
                    select c.UserId).Distinct();
            var id = userid.FirstOrDefault();  //在数据库中根据key找到相应记录
            if (id == default)
            {
                //Response.StatusCode = 410;//没找到该用户
                flag = 2;//没有找到该用户
                return Json(new { UserID = id, ReturnFlag = flag, result = "NOT FOUND" });
            }

            var checkArticle =
                (from c in entity.Article
                 where (c.Title == title && c.UserId == id)
                 select c.ArticleId).Distinct();
            var check = checkArticle.FirstOrDefault();
            if (check != default)
            {
                // Response.StatusCode = 400;
                flag = 3;//该用户已存在同名文章
                return Json(new { ReturnFlag = flag, UserName = nick_name, Article_Name = title, result = "Already Exists!" });
            }

            //Zone里面更新文章数量
            var zone = entity.Zone.Find(zoneid);
            var num = zone.ZoneArticleNum;
            if (num == default)
                zone.ZoneArticleNum = 1;
            else
                zone.ZoneArticleNum = num + 1;
            entity.Entry(zone).State = EntityState.Modified;
            //entity.SaveChanges();

            //Article里新增文章
            var article = new Article();
            article.Title = title;
            article.UserId = id;
            article.ReadNum = 0;
            article.ZoneId=zoneid;
            article.CollectNum = 0;
            article.ArticleLikes = 0;
            article.ArticleUploadTime = DateTime.Now;
            article.ArticleContent = content;
            entity.Article.Add(article);
            entity.SaveChanges();

            //Response.StatusCode = 200;
            flag = 1;
            return Json(new { ReturnFlag = flag, user = nick_name, article = title });
        }


        /// <summary>
        /// 新增文章评论
        /// 名为nick_name用户为一篇题目为title的文章写评论，内容为content
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///             ReturnFlag = flag,
        ///             articleID = A_id,
        ///             articlecomment = content
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult createArticleCommentByNickName(string nick_name, string title, string content)
        {
            var flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            var articleid =
                (from c in entity.Article
                 where (c.Title == title)
                 select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3; //没找到该文章
                return Json(new { ReturnFlag = flag, article = title, result = "Article NOT FOUND" });
            }

            //ARTICLE表更新评论
            // var article = entity.Article.Find(A_id);
            //article.ArticleCommentReply = articlecommentID;          
            //entity.Entry(article).State = EntityState.Modified;
            // entity.SaveChanges();

            //加入ARTICLE_COMMENT_REPLY表
            var item = new ArticleCommentReply();
            //  item.ArticleCrId = articlecommentID;
            item.ArticleId = A_id;
            item.ArticleCrContent = content;
           // item.ParentCrId = -1;
            item.ArticleCrTime = DateTime.Now;
            item.UserId = id;
            entity.ArticleCommentReply.Add(item);
            entity.SaveChanges();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, articleID = A_id, articlecomment = content });
        }

        /// <summary>
        /// 查看文章信息
        /// 名为nick_name用户查看一篇题目为title的文章
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///             ReturnFlag = flag, 
        ///             INFO = info
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult getArticleInfoByTitle(string nick_name, string title)
        {
            //根据用户名找到用户ID
            var flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //根据文章名找到对应文章
            var articleid =
                    (from c in entity.Article
                     where c.Title == title 
                     select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, ArticleID = A_id, result = "NOT FOUND" });
            }

            //修改Article表
            var article = entity.Article.Find(A_id);
            article.ReadNum++;
            entity.Entry(article).State = EntityState.Modified;
            entity.SaveChanges();

            //修改ArticleVisit表
            var item = new ArticleVisit();
            item.ArticleId = A_id;
            item.ArticleVisitTime = DateTime.Now;
            item.UserId = id;
            entity.ArticleVisit.Add(item);
            entity.SaveChanges();
            var info = entity.Article.Find(A_id);
            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag,INFO = info });
        }

        /// <summary>
        /// 删除文章
        /// 名为nick_name用户删除自己的题目为title的文章
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///            eturnFlag = flag,
        ///            UserName = nick_name,
        ///            ArticleName = title,
        ///            result = "success!"
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult deleteArticleByTitle(string nick_name, string title)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到对应文章
            var articleid =
                    (from c in entity.Article
                     where (c.Title == title && c.UserId == id)
                     select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            //获得ArticleCommentReply表中记录
            var A_CR_record =
                (from c in entity.ArticleCommentReply
                 where c.ArticleId == A_id
                 select c).ToList();
            foreach (var record in A_CR_record)
            {
                var crID = record.ArticleCrId;

                entity.Entry(record).State = EntityState.Deleted;
                entity.SaveChanges();
            }

            //获得ArticleRank表中记录
            var A_Rank_record =
                (from c in entity.ArticleRank
                 where c.ArticleId == A_id
                 select c).ToList();
            foreach (var record in A_Rank_record)
            {
                var RID = record.ArticleRank1;

                entity.Entry(record).State = EntityState.Deleted;
                entity.SaveChanges();
            }

            //ArticleVisit删除
            var A_V_record =
                (from c in entity.ArticleVisit
                 where c.ArticleId == A_id
                 select c).ToList();
            foreach (var record in A_V_record)
            {
                // var VID = record.ArticleId;

                entity.Entry(record).State = EntityState.Deleted;
                entity.SaveChanges();
            }


            //ARTICLE表删除
            var info = entity.Article.Find(A_id);
            entity.Entry(info).State = EntityState.Deleted;
            entity.SaveChanges();

            //Response.StatusCode = 200;
            flag = 1;
            return Json(new { ReturnFlag = flag, UserName = nick_name, ArticleName = title, result = "success!" });
        }


        /// <summary>
        /// 删除评论
        /// 名为nick_name用户删除自己的id为articlecommentID的评论
        /// <param name="nick_name"></param>
        /// <param name="articlecommentID"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           result = "successful deleted"
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该用户评论
        /// </remarks>
        [HttpPost]
        public JsonResult deleteArticleCommentByID(string nick_name, int articlecommentID)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到对应评论
            var crid =
                   (from c in entity.ArticleCommentReply
                    where (c.ArticleCrId == articlecommentID && c.UserId == id)
                    select c.ArticleCrId).Distinct();
            var A_id = crid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章评论
                flag = 3;
                return Json(new { ReturnFlag = flag, crID = A_id, result = "NOT FOUND" });
            }

            // var article = entity.Article.Find(A_id);

            var item = entity.ArticleCommentReply.Find(A_id);
            //if (item == default)
            //{
            //    Response.StatusCode = 406;//文章无该评论
            //    return Json(new { articleID = A_id, ArticleCommentID = articlecommentID, result = "NOT FOUND" });
            //}

            //ARTICLE_COMMENT_REPLY表中更新
            //var articlecomment = entity.ArticleCommentReply.Find(articlecommentID);
            // entity.Entry(articlecomment).State = EntityState.Modified;
            //entity.SaveChanges();
            //FAVORITE_ARTICLE表删除
            //var item = entity.FavouriteArticle.Find(F_id, articleID);
            entity.Entry(item).State = EntityState.Deleted;
            entity.SaveChanges();

            // Response.StatusCode = 200;
            flag = 1;
            return Json(new { ReturnFlag = flag, result = "successful deleted" });
        }


        /// <summary>
        /// 查看文章评论，分页
        /// 名为nick_name用户查看一篇题目为title的文章的所有评论
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           Item = item //内容里user_id 修改为 Nick_Name
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult getArticleCommentByTitle(string nick_name, string title,int pageNum,int pageSize)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            var item =
                (from u in entity.ArticleCommentReply
                 join right in entity.User
                 on u.UserId equals right.UserId //into UserComment
              //   from a in UserComment
               //  join left in entity.User
              //   on u.ParentCrId equals left.UserId into result
               //  from resulti in result.DefaultIfEmpty()
                 where u.ArticleId == A_id
                 orderby u.ArticleCrTime descending
                 select new { Article_cr_id = u.ArticleCrId, 
                     Article_cr_content = u.ArticleCrContent,
                     Article_id = u.ArticleId,
                     Nick_name = right.NickName, 
                     Article_cr_time = u.ArticleCrTime, 
                     Parent_cr_id = u.ParentCrId,
                    // Parent_cr_name =resulti.NickName,
                   //  Parent_cr_avatr=resulti.Avatr,
                     /*
                      Parent_cr_name=(from c in entity.User
                                      where c.UserId==u.ParentCrId
                                      select c.NickName).Distinct(),
  /*
                      Parent_cr_avatr = (from a in entity.User
                                        where a.UserId == u.ParentCrId
                                        select a.Avatr),
                      */
                     Avatr =right.Avatr }).Skip((pageNum-1)*pageSize).Take(pageSize);
            /*
            commentnum = (from u in entity.ArticleCommentReply
                          where u.ArticleId == c.ArticleId
                          select u).Count()
            */
            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, Item = item });

        }

        

        /// <summary>
        /// 删除文章浏览记录
        /// 名为nick_name用户删除自己浏览题目为title的文章的记录
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag,
        ///           result = "successful deleted"
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        ///     4：没有找到浏览记录
        /// </remarks>
        [HttpPost]
        public JsonResult DeleteArticleVisitByTitle(string nick_name, string title)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到对应文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            //找到对应浏览记录
            var visitid =
                   (from c in entity.ArticleVisit
                    where (c.ArticleId == A_id && c.UserId == id)
                    select c.ArticleId).Distinct();
            var V_id = visitid.FirstOrDefault();
            if (V_id == default)
            {
                //Response.StatusCode = 404;//没找到该文件夹
                flag = 4;
                return Json(new { ReturnFlag = flag, VID = V_id, result = "NOT FOUND" });
            }

            var item = entity.ArticleVisit.Find(id, A_id);

            //ARTICLEVISIT表中删除
            entity.Entry(item).State = EntityState.Deleted;
            entity.SaveChanges();

            //Response.StatusCode = 200;
            flag = 1;
            return Json(new { ReturnFlag = flag, result = "successful deleted" });

        }

        /// <summary>
        /// 获取文章浏览记录
        /// 名为nick_name用户查看一篇题目为title的文章的所有浏览记录
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           Item = item
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult getArticleVisitByTitle(string nick_name, string title)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            var item =
                (from u in entity.ArticleVisit
                 where u.ArticleId == A_id
                 select u).OrderByDescending(u => u.ArticleVisitTime).Distinct();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, Item = item });

        }

        /// <summary>
        /// 返回父评论信息
        /// </summary>
        /// <param name="parentid"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult getparentCRByID(int parentid)
        {
            var flag = 0;
 
            var item =
                (from u in entity.User
                 
                // on u.UserId equals right.UserId
                 where u.UserId == parentid
                 select u).Distinct();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, Item = item });

        }

        /// <summary>
        /// 获取文章浏览排行
        /// 名为nick_name用户查看一篇题目为title的文章的排行
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           Itme = item
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult getArticleRankByTitle(string nick_name, string title)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            var item =
                (from u in entity.ArticleRank
                 where u.ArticleId == A_id
                 select u).Distinct();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, Itme = item });

        }

        /// <summary>
        /// 更新文章
        /// 名为nick_name用户更新一篇题目为title的文章的题目和内容
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <param name="newName"></param>
        /// <param name="newContent"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           UserName = nick_name, 
        ///           OldName = title, 
        ///           NewName = newName,
        ///           NewContent=newContent
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult updateArticleByTtile(string nick_name, string title, string newName, string newContent)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title && c.UserId == id)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            var info = entity.Article.Find(A_id);
            info.Title = newName;
            info.ArticleContent = newContent;
            entity.Entry(info).State = EntityState.Modified;
            entity.SaveChanges();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, UserName = nick_name, OldName = title, NewName = newName, NewContent = newContent });
        }

        /// <summary>
        /// articlelikes加一
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           result = "successful"
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult addArticleLikeByTitle(string nick_name, string title)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            var info = entity.Article.Find(A_id);
            info.ArticleLikes++;
            entity.Entry(info).State = EntityState.Modified;
            entity.SaveChanges();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, result = "successful" });
        }

        /// <summary>
        /// collectNum加1
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           result = "successful"
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该文章
        /// </remarks>
        [HttpPost]
        public JsonResult addArticleCollectByTitle(string nick_name, string title)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                //Response.StatusCode = 404;//没找到该文章
                flag = 3;
                return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
            }

            var info = entity.Article.Find(A_id);
            info.CollectNum++;
            entity.Entry(info).State = EntityState.Modified;
            entity.SaveChanges();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, result = "successful" });
        }

        /// <summary>
        /// 更新评论
        /// 名为nick_name用户更新id为articlecrid的评论的内容
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="articlecrid"></param>
        /// <param name="newContent"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag,
        ///           UserName = nick_name, 
        ///           crID=articlecrid,
        ///           NewContent = newContent
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到该评论
        /// </remarks>
        [HttpPost]
        public JsonResult updateArticleCommentByID(string nick_name, int articlecrid, string newContent)
        {
            var flag = 0;
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            /* var articleid =
                    (from c in entity.Article
                     where (c.Title == title)
                     select c.ArticleId).Distinct();
             var A_id = articleid.FirstOrDefault();
             if (A_id == default)
             {
                 //Response.StatusCode = 404;//没找到该文件夹
                 flag = 3;
                 return Json(new { ReturnFlag = flag, articleID = A_id, result = "NOT FOUND" });
             }*/
            //找到文章
            var crid =
                   (from c in entity.ArticleCommentReply
                    where (c.ArticleCrId == articlecrid && c.UserId == id)
                    select c.ArticleCrId).Distinct();
            var cr_id = crid.FirstOrDefault();
            if (cr_id == default)
            {
                //Response.StatusCode = 404;//没找到该评论
                flag = 3;
                return Json(new { ReturnFlag = flag, crID = cr_id, result = "NOT FOUND" });
            }

            var info = entity.ArticleCommentReply.Find(cr_id);
            info.ArticleCrContent = newContent;
            entity.Entry(info).State = EntityState.Modified;
            entity.SaveChanges();


            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, UserName = nick_name, crID = articlecrid, NewContent = newContent });
        }

        /// <summary>
        /// 评论评论
        /// 名为nick_name用户评论id为articlecrid的评论
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="articlecommentID1"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///           ReturnFlag = flag, 
        ///           aclid = articlecommentID1, 
        ///           result = " NOT FOUND"
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：没有找到该用户
        ///     3：没找到要评论的评论
        /// </remarks>
        [HttpPost]
        public JsonResult createCommentCommentByID(string nick_name, int articlecommentID1, string content)
        {
            var flag = 0;
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }

            var articleid =
                (from c in entity.ArticleCommentReply
                 where (c.ArticleCrId == articlecommentID1)
                 select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            var crid =
                (from c in entity.ArticleCommentReply
                 where (c.ArticleCrId == articlecommentID1)
                 select c.ArticleCrId).Distinct();
            var CR_id = crid.FirstOrDefault();
            if (CR_id == default)
            {
                //Response.StatusCode = 404;
                flag = 3;
                return Json(new { ReturnFlag = flag, aclid = articlecommentID1, result = " NOT FOUND" });
            }


            //加入ARTICLE_COMMENT_REPLY表
            var item = new ArticleCommentReply();
            // item.ArticleCrId = articlecommentID2;
            item.ArticleId = A_id;
            item.ArticleCrContent = content;
            item.ArticleCrTime = DateTime.Now;
            item.UserId = id;
            item.ParentCrId = articlecommentID1;
            entity.ArticleCommentReply.Add(item);
            entity.SaveChanges();

            //Response.StatusCode = 200;//成功
            flag = 1;
            return Json(new { ReturnFlag = flag, articleID = A_id, articlecomment = content });
        }

        /// <summary>
        /// 分页获取zone的article
        /// </summary>
        /// <param name="pageNum"></param>
        /// <param name="pageSize"></param>
        /// <param name="zoneID"></param>
        /// <returns></returns>
        ///<remarks>
        ///     返回内容：
        ///     {
        ///          getFlag = getArticleFlag, 
        ///          
        ///           result = contentJson
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        /// </remarks>
        //[SwaggerResponse(200, "文档注释", typeof(Topic))]
        [HttpPost]
        public JsonResult getArticleByPage(int pageNum, int pageSize, string zoneID)
        {
            int getArticleFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");

            try
            {
                var content = (from c in entity.Article
                               join right in entity.User
                               on c.UserId equals right.UserId
                               where c.ZoneId == int.Parse(zoneID)
                               orderby c.ArticleUploadTime descending
                               select new
                               {
                                   ArticleId = c.ArticleId,
                                   Title = c.Title,
                                   UserId = c.UserId,
                                   Nick_name = right.NickName,
                                   Avatr = right.Avatr,
                                   ArticleContent = c.ArticleContent,
                                   ArticleLikes = c.ArticleLikes,
                                   CollectNum = c.CollectNum,
                                   ReadNum = c.ReadNum,
                                   ArticleUploadTime = c.ArticleUploadTime,
                                   Zoneid = c.ZoneId,
                                   commentnum = (from u in entity.ArticleCommentReply
                                                 where u.ArticleId == c.ArticleId
                                                 select u).Count()
                               }                              
                               ).Skip((pageNum - 1) * pageSize).Take(pageSize);
                /*
                from u in entity.ArticleCommentReply
                join right in entity.User
                on u.UserId equals right.UserId
                where u.ArticleId == A_id
                orderby u.ArticleCrTime descending
                select new
                {
                    Article_cr_id = u.ArticleCrId,
                    Article_cr_content = u.ArticleCrContent,
                    Article_id = u.ArticleId,
                    Nick_name = right.NickName,
                    Article_cr_time = u.ArticleCrTime,
                    Parent_cr_id = u.ParentCrId,
                    Avatr = right.Avatr
                }).Skip((pageNum - 1) * pageSize).Take(pageSize);
                */
                //string contentJson = JsonConvert.SerializeObject(content); //序列化对象
                //returnJson["Result"] = contentJson;
                getArticleFlag = 1;
                return Json(new { articleDetail = content, flag = getArticleFlag });

            }
            catch (Exception e)
            {
                getArticleFlag = 0;

            }
            finally
            {
                returnJson.Add("getFlag", getArticleFlag.ToString());
            }
            return Json(returnJson);
        }

        /// <summary>
        /// 分页获取user的article
        /// </summary>
        /// <param name="pageNum"></param>
        /// <param name="pageSize"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        /// <remarks>
        ///     返回内容：
        ///     {
        ///          getFlag = getFlag,        
        ///           result = contentJson
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        /// </remarks>
        [HttpPost]
        public JsonResult getpersonalArticlebypage(int pageNum, int pageSize, int userId)
        {
            int getFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");
         //   var user_name =
          //         (from c in entity.User
         //           where c.UserId ==userId
         //           select c.NickName).Distinct();
         //   var username = user_name.FirstOrDefault();

            try
            {
                // var ArticleCnt =
                //      (from u in entity.Article
                //     where u.UserId == id
                //   select u).Distinct().Count();
                var content = (from c in entity.Article
                               join right in entity.User on c.UserId equals right.UserId
                               where c.UserId == userId
                               orderby c.ArticleUploadTime descending
                               select new
                               {
                                   User_name=right.NickName,
                                  // authorname=right.NickName,
                                   article_id = c.ArticleId,
                                   title = c.Title,
                                   userid = c.UserId,
                                   articlcontent = c.ArticleContent,
                                   articlelike = c.ArticleLikes,
                                   collectnum = c.CollectNum,
                                   readnum = c.ReadNum,
                                   uploadtime = c.ArticleUploadTime,
                                   zoneid = c.ZoneId,
                                   commentnum = (from u in entity.ArticleCommentReply
                                                 where u.ArticleId == c.ArticleId
                                                 select u).Count(),//.Distinct().Count(),                   
                               }).Skip((pageNum - 1) * pageSize).Take(pageSize); 
             

                string contentJson = JsonConvert.SerializeObject(content); //序列化对象
                returnJson["Result"] = contentJson;
                getFlag = 1;

            }
            catch (Exception e)
            {
                getFlag = 0;

            }
            finally
            {
                returnJson.Add("getFlag", getFlag.ToString());
            }
            return Json(returnJson);
        }

        /// <summary>
        /// 分页获取别人的article
        /// </summary>
        /// <param name="pageNum"></param>
        /// <param name="pageSize"></param>
        /// <param name="userId"></param>
        /// <param name="authorid"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///          getFlag = getFlag,        
        ///           result = contentJson
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        /// </remarks>
        [HttpPost]
        public JsonResult getotherArticlebypage(int pageNum, int pageSize, int userId, int authorid)
        {
            int getFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");
            var user_name =
                   (from c in entity.User
                    where c.UserId == userId
                    select c.NickName).Distinct();
            var username = user_name.FirstOrDefault();

            try
            {
                // var ArticleCnt =
                //      (from u in entity.Article
                //     where u.UserId == id
                //   select u).Distinct().Count();
                var content = (from c in entity.Article
                               join right in entity.User on c.UserId equals right.UserId
                               where c.UserId == authorid
                               orderby c.ArticleUploadTime descending
                               select new
                               {
                                   User_name = username,
                                   authorname = right.NickName,
                                   article_id = c.ArticleId,
                                   title = c.Title,
                                   userid = c.UserId,
                                   articlcontent = c.ArticleContent,
                                   articlelike = c.ArticleLikes,
                                   collectnum = c.CollectNum,
                                   readnum = c.ReadNum,
                                   uploadtime = c.ArticleUploadTime,
                                   zoneid = c.ZoneId,
                                   commentnum = (from u in entity.ArticleCommentReply
                                                 where u.ArticleId == c.ArticleId
                                                 select u).Count(),//.Distinct().Count(),                   
                               }).Skip((pageNum - 1) * pageSize).Take(pageSize);


                string contentJson = JsonConvert.SerializeObject(content); //序列化对象
                returnJson["Result"] = contentJson;
                getFlag = 1;

            }
            catch (Exception e)
            {
                getFlag = 0;

            }
            finally
            {
                returnJson.Add("getFlag", getFlag.ToString());
            }
            return Json(returnJson);
        }

        /// <summary>
        /// 按阅读量获取文章
        /// </summary>
        /// <param name="takeArticleNum"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///          getFlag = getFlag,        
        ///           result = contentJson
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        /// </remarks>
        [HttpPost]
        public JsonResult getArticlebyreadnum(int takeArticleNum,int userid)
        {
            int flag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            var user_name =
                 (from c in entity.User
                  where c.UserId == userid
                  select c.NickName).Distinct();
            var username = user_name.FirstOrDefault();

            try
            {
               // var content = entity.Article.OrderByDescending(c => c.ReadNum).Take(takeArticleNum);
                var content = (from c in entity.Article
                                join right in entity.User on c.UserId equals right.UserId
                                //where c.UserId == authorid
                                orderby c.ReadNum descending
                                select new 
                                {
                                    NickName=right.NickName,
                                    ArticleCommentReply=c.ArticleCommentReply,
                                    ArticleContent=c.ArticleContent,
                                    ArticleId=c.ArticleId,
                                    ArticleLikes=c.ArticleLikes,
                                    ArticleRank=c.ArticleRank,
                                    ArticleUploadTime = c.ArticleUploadTime,
                                    ArticleVisit=c.ArticleVisit,
                                    CollectNum=c.CollectNum,
                                    FavouriteArticle=c.FavouriteArticle,
                                    ReadNum=c.ReadNum,
                                    Title=c.Title,
                                   // User=c.User,
                                    UserId=c.UserId,
                                    ZoneId=c.ZoneId,
                                    Username=username
                                 //   username,
                             //   c,
                          //     right.NickName
                                }).Take(takeArticleNum);

                 returnJson.Add("articless", JsonConvert.SerializeObject(content));
                flag = 1;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                flag = 0;
            }

            returnJson.Add("flag", flag.ToString());
            return Json(returnJson);
        }
        /*
        /// <summary>
        /// 获取最新评论
        /// 根据articleID获取本文章最新评论
        /// </summary>
        /// <param name="articleid"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///          flag = flag, 
        ///          
        ///         comment = content
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        /// </remarks>
        [HttpPost]
        public JsonResult getNewestcomment(int articleid)
        {
            int flag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();

            try
            {
                var content = (from c in entity.ArticleCommentReply
                               where c.ArticleId == articleid 
                               orderby  c.ArticleCrTime descending
                               select c).Distinct();
               // var content1 = entity.ArticleCommentReply.OrderByDescending(c => c.ArticleCrTime ).Take(takeTopicNum);

                returnJson.Add("comment", JsonConvert.SerializeObject(content));
                flag = 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                flag = 0;
            }

            returnJson.Add("flag", flag.ToString());
            return Json(returnJson);
        }
        */

        /// <summary>
        /// 获取一个用户所有文章的最新评论
        /// </summary>
        /// <param name="takeCommentNum"></param>
        /// <param name="nick_name"></param>
        /// <returns></returns>
        ///  <remarks>
        ///     返回内容：
        ///     {
        ///          flag = flag, 
        ///          
        ///         comment = content
        ///     }
        ///     
        ///     flag:
        ///     0：未操作
        ///     1：成功
        ///     2：找不到用户
        /// </remarks>
        [HttpPost]
        public JsonResult getNewestComment(int takeCommentNum,string nick_name)
        {
            int flag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            //找用户
            var userid =
        (from c in entity.User
         where c.NickName == nick_name
         select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                //Response.StatusCode = 405;//没找到该用户
                flag = 2;
                return Json(new { ReturnFlag = flag, UserID = id, result = "NOT FOUND" });
            }
            try
            {    
             var content =
                (from c in entity.ArticleCommentReply 
                 join right in entity.Article on c.ArticleId equals right.ArticleId
                 join left in entity.User on c.UserId equals left.UserId         
                 where (right.UserId==id)
                 orderby c.ArticleCrTime descending
                 select new { Article_cr_id = c.ArticleCrId, 
                     Article_cr_content = c.ArticleCrContent, 
                     Article_id = c.ArticleId, Nick_name = left.NickName, 
                     Article_cr_time = c.ArticleCrTime,
                     Parent_cr_id = c.ParentCrId }).Take(takeCommentNum);
            //    var item =
          //     (from u in entity.ArticleCommentReply
          //      join right in entity.User
          //      on u.UserId equals right.UserId
         //       where u.ArticleId == A_id
          //      select new { Article_cr_id = u.ArticleCrId, Article_cr_content = u.ArticleCrContent, Article_id = u.ArticleId, Nick_name = right.NickName, Article_cr_time = u.ArticleCrTime, Parent_cr_id = u.ParentCrId }).Distinct();

                //   var content = entity.ArticleCommentReply.OrderByDescending(c => c.TopicUploadTime).Take(takeTopicNum);
                returnJson.Add("comments", JsonConvert.SerializeObject(content));
                flag = 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                flag = 0;
            }

            returnJson.Add("flag", flag.ToString());
            return Json(returnJson);
        }

        /// <summary>
        /// 分页返回所有文章——按照（浏览量+点赞量）从大到小的顺序
        /// </summary>
        /// <param name="pageNum">页号</param>
        /// <param name="pageSize">一页大小</param>
        /// <returns></returns>
        /// <remarks>
        ///     flag:
        ///     0:未操作
        ///     1：成功
        ///     
        ///     返回：{Result = content,getArticleFlag = flag}
        /// </remarks>
        [HttpPost]
        public JsonResult getRecommandedArticle(int pageNum, int pageSize)
        {
            int getArticleFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");

            try
            {
                var content = (from c in entity.Article
                               orderby (c.ReadNum + c.ArticleLikes) descending  //按照文章（浏览量+点赞量）从大到小的顺序进行排序  
                               select c).Skip((pageNum - 1) * pageSize).Take(pageSize);

                string contentJson = JsonConvert.SerializeObject(content); //序列化对象
                returnJson["Result"] = contentJson;
                getArticleFlag = 1;

            }
            catch (Exception e)
            {
                getArticleFlag = 0;

            }
            finally
            {
                returnJson.Add("getArticleFlag", getArticleFlag.ToString());
            }
            return Json(returnJson);
        }

        /// <summary>
        /// 得到一个用户按照（浏览量+点赞量）前getarticleNum篇文章
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="getarticleNum"></param>
        /// <returns></returns>
        /// <remarks>
        ///     flag:
        ///     0:未操作
        ///     1：成功
        ///     
        ///     返回：{Result = content,flag = getArticleFlag}
        /// </remarks>
        [HttpPost]
        public JsonResult getPersonalhottestArticle(int userid,int getarticleNum)
        {
            int getArticleFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");

            try
            {
                var content = (from c in entity.Article
                               where c.UserId==userid
                               orderby (c.ReadNum + c.ArticleLikes) descending  //按照文章（浏览量+点赞量）从大到小的顺序进行排序  
                               select c).Take(getarticleNum);

               // string contentJson = JsonConvert.SerializeObject(content); //序列化对象
               // returnJson["Result"] = contentJson;
                getArticleFlag = 1;
                return Json(new { articleDetail = content, flag = getArticleFlag });
            }
            catch (Exception e)
            {
                getArticleFlag = 0;

            }
            finally
            {
                returnJson.Add("getArticleFlag", getArticleFlag.ToString());
            }
            return Json(returnJson);
        }

        /// <summary>
        /// 得到一个用户按照上传时间前getarticleNum篇文章
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="getarticleNum"></param>
        /// <returns></returns>
        /// <remarks>
        ///     flag:
        ///     0:未操作
        ///     1：成功
        ///     
        ///     返回：{Result = content,flag = getArticleFlag}
        /// </remarks>
        [HttpPost]
        public JsonResult getPersonalnewestArticle(int userid, int getarticleNum)
        {
            int getArticleFlag = 0;
            Dictionary<string, string> returnJson = new Dictionary<string, string>();
            returnJson.Add("Result", "");

            try
            {
                var content = (from c in entity.Article
                               where c.UserId == userid
                               orderby c.ArticleUploadTime descending  //按照文章（浏览量+点赞量）从大到小的顺序进行排序  
                               select c).Take(getarticleNum);

                // string contentJson = JsonConvert.SerializeObject(content); //序列化对象
                // returnJson["Result"] = contentJson;
                getArticleFlag = 1;
                return Json(new { articleDetail = content, flag = getArticleFlag });
            }
            catch (Exception e)
            {
                getArticleFlag = 0;

            }
            finally
            {
                returnJson.Add("getArticleFlag", getArticleFlag.ToString());
            }
            return Json(returnJson);
        }

        /// <summary>
        /// article关键字搜索
        /// </summary>
        /// <param name="searchContent"></param>
        /// <returns></returns>
        /// <remarks>
        ///     flag:
        ///     0:未操作
        ///     1：成功
        ///     
        ///     返回：{Result = content,flag = flag}
        /// </remarks>
        [HttpPost]
        public JsonResult getSearchedArticle(string searchContent,int pageNum,int pageSize)
        {
            int flag = 0;
            string tt = string.Format(@"({0})", searchContent);

            try
            {
                var content = (from c in entity.Article
                               join d in entity.Zone on c.ZoneId equals d.ZoneId
                               where c.Title.Contains(searchContent)
                               select new
                               {
                                   ArticleTitle = c.Title,
                                   articleid = c.ArticleId,
                                   articleContent = c.ArticleContent,
                                   articlelike=c.ArticleLikes,
                                   collectnum=c.CollectNum,
                                   readnum=c.ReadNum,
                                 //  answerNum = c.AnswerNum,
                                   userId = c.UserId,
                                   articleUploadTime = c.ArticleUploadTime,
                                   zoneId = c.ZoneId,
                                   zoneName = d.ZoneName,
                               }).Skip((pageNum - 1) * pageSize).Take(pageSize);
                flag = 1;
                return Json(new { articleDetail = content, flag = flag });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                flag = 0;
                return Json(new { flag = flag });
            }
        }

        /// <summary>
        /// 个人article关键字搜索
        /// </summary>
        /// <param name="searchContent"></param>
        /// <param name="pageNum"></param>
        /// <param name="pageSize"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult getSearchedpersonalArticle(string searchContent, int pageNum, int pageSize,int userid)
        {
            int flag = 0;
            string tt = string.Format(@"({0})", searchContent);

            try
            {
                var content = (from c in entity.Article
                               where c.UserId==userid
                               join d in entity.Zone on c.ZoneId equals d.ZoneId
                               where c.Title.Contains(searchContent)
                               select new
                               {
                                   ArticleTitle = c.Title,
                                   articleid = c.ArticleId,
                                   articleContent = c.ArticleContent,
                                   articlelike = c.ArticleLikes,
                                   collectnum = c.CollectNum,
                                   readnum = c.ReadNum,
                                   //  answerNum = c.AnswerNum,
                                   userId = c.UserId,
                                   articleUploadTime = c.ArticleUploadTime,
                                   zoneId = c.ZoneId,
                                   zoneName = d.ZoneName,
                               }).Skip((pageNum - 1) * pageSize).Take(pageSize);
                flag = 1;
                return Json(new { articleDetail = content, flag = flag });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                flag = 0;
                return Json(new { flag = flag });
            }
        }

    }

}




