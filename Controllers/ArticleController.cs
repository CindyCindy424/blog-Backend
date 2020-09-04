using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;
using Microsoft.EntityFrameworkCore;
using Temperature.Models;
using System.Diagnostics;



  namespace Temperature.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    //[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    
   
    public class ArticleController : Controller
    {
        private blogContext entity = new blogContext(); //整体数据库类型
        /// <summary>
        /// 创建文章
        /// 名为nick_name用户创建一篇题目为title内容为content的文章
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="articleName"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult createArticleByNickName(string nick_name, string title,string content)
        {
            var userid =
                   (from c in entity.User
                    where c.NickName == nick_name
                    select c.UserId).Distinct();
            var id = userid.FirstOrDefault();  //在数据库中根据key找到相应记录
            if (id == default)
            {
                Response.StatusCode = 410;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            var checkArticle =
                (from c in entity.Article
                 where (c.Title== title && c.UserId == id)
                 select c.ArticleId).Distinct();
            var check = checkArticle.FirstOrDefault();
            if (check != default)
            {
                Response.StatusCode = 400;//该用户已存在同名文章
                return Json(new { UserName = nick_name, Article_Name = title, result = "Already Exists!" });
            }

            var article = new Article();
            article.Title = title;
            article.UserId = id;
            article.ReadNum = 0;
            article.CollectNum = 0;
            article.ArticleLikes = 0;
            article.ArticleUploadTime = DateTime.Now;
            article.ArticleContent = content;
            entity.Article.Add(article);
            entity.SaveChanges();

            Response.StatusCode = 200;
            return Json(new { user = nick_name, article = title });
        }

        /// <summary>
        /// 新增文章评论
        /// 名为nick_name用户为一篇题目为title的文章写评论，内容为content
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <param name="articlecommentID"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult createArticleCommentByNickName(string nick_name, string title, string content)
        {
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            var articleid =
                (from c in entity.Article
                 where (c.Title == title)
                 select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文章
                return Json(new { article = title, result = "Article NOT FOUND" });
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
            item.ArticleCrTime = DateTime.Now;
            item.UserId = id;
            entity.ArticleCommentReply.Add(item);
            entity.SaveChanges();

            Response.StatusCode = 200;//成功
            return Json(new { articleID = A_id, articlecomment = content });
        }

        /// <summary>
        /// 查看文章信息
        /// 名为nick_name用户查看一篇题目为title的文章
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult getArticleInfoByTitle(string nick_name, string title)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //根据用户ID和文章名找到对应文章
            var articleid =
                    (from c in entity.Article
                     where (c.Title == title )
                     select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { ArticleID = A_id, result = "NOT FOUND" });
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

            var info = entity.Favourite.Find(A_id);
            Response.StatusCode = 200;//成功
            return Json(info);
        }

        /// <summary>
        /// 删除文章
        /// 名为nick_name用户删除自己的题目为title的文章
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult deleteArticleByTitle(string nick_name, string title)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到对应文章
            var articleid =
                    (from c in entity.Article
                     where (c.Title == title && c.UserId == id)
                     select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { articleID = A_id, result = "NOT FOUND" });
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

            Response.StatusCode = 200;
            return Json(new { UserName = nick_name, ArticleName = title, result = "success!" });
        }


        /// <summary>
        /// 删除评论
        /// 名为nick_name用户删除自己的id为articlecommentID的评论
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <param name="articlecommentID"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult deleteArticleCommentByID(string nick_name,int articlecommentID)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到对应评论
            var crid =
                   (from c in entity.ArticleCommentReply
                    where (c.ArticleCrId== articlecommentID && c.UserId == id)
                    select c.ArticleCrId).Distinct();
            var A_id = crid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { crID = A_id, result = "NOT FOUND" });
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

            Response.StatusCode = 200;
            return Json(new { result = "successful deleted" });
        }

        /// <summary>
        /// 查看文章评论
        /// 名为nick_name用户查看一篇题目为title的文章的所有评论
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult getArticleCommentByTitle(string nick_name, string title)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { articleID = A_id, result = "NOT FOUND" });
            }

            var item =
                (from u in entity.ArticleCommentReply
                 where u.ArticleId == A_id
                 select u).Distinct();

            Response.StatusCode = 200;//成功
            return Json(item);

        }

        /// <summary>
        /// 删除文章浏览记录
        /// 名为nick_name用户删除自己浏览题目为title的文章的记录
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult DeleteArticleVisitByTitle(string nick_name, string title)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到对应文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { articleID = A_id, result = "NOT FOUND" });
            }

            //找到对应浏览记录
            var visitid =
                   (from c in entity.ArticleVisit
                    where (c.ArticleId == A_id && c.UserId == id)
                    select c.ArticleId).Distinct();
            var V_id = visitid.FirstOrDefault();
            if (V_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { VID = V_id, result = "NOT FOUND" });
            }

            var item = entity.ArticleVisit.Find(id,A_id);
  
            //ARTICLEVISIT表中删除
            entity.Entry(item).State = EntityState.Deleted;
            entity.SaveChanges();

            Response.StatusCode = 200;
            return Json(new { result = "successful deleted" });

        }

        /// <summary>
        /// 获取文章浏览记录
        /// 名为nick_name用户查看一篇题目为title的文章的所有浏览记录
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult getArticleVisitByTitle(string nick_name, string title)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { articleID = A_id, result = "NOT FOUND" });
            }

            var item =
                (from u in entity.ArticleVisit
                 where u.ArticleId == A_id
                 select u).Distinct();

            Response.StatusCode = 200;//成功
            return Json(item);

        }


        /// <summary>
        /// 获取文章浏览排行
        /// 名为nick_name用户查看一篇题目为title的文章的排行
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult getArticleRankByTitle(string nick_name, string title)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title )
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { articleID = A_id, result = "NOT FOUND" });
            }

            var item =
                (from u in entity.ArticleRank
                 where u.ArticleId == A_id
                 select u).Distinct();

            Response.StatusCode = 200;//成功
            return Json(item);

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
        [HttpPost]
        public JsonResult updateArticleByTtile(string nick_name, string title, string newName,string newContent)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var articleid =
                   (from c in entity.Article
                    where (c.Title == title && c.UserId == id)
                    select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            if (A_id == default)
            {
                Response.StatusCode = 404;//没找到该文件夹
                return Json(new { articleID = A_id, result = "NOT FOUND" });
            }

            var info = entity.Article.Find(A_id);
            info.Title = newName;
            info.ArticleContent = newContent;
            entity.Entry(info).State = EntityState.Modified;
            entity.SaveChanges();

            Response.StatusCode = 200;//成功
            return Json(new { UserName = nick_name, OldName = title, NewName = newName,NewContent=newContent });
        }

        /// <summary>
        /// 更新评论
        /// 名为nick_name用户更新id为articlecrid的评论的内容
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="articlecrid"></param>
        /// <param name="newContent"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult updateArticleCommentByID(string nick_name, int articlecrid, string newContent)
        {
            //根据用户名找到用户ID
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            //找到文章
            var crid =
                   (from c in entity.ArticleCommentReply
                    where (c.ArticleCrId == articlecrid && c.UserId == id)
                    select c.ArticleCrId).Distinct();
            var cr_id = crid.FirstOrDefault();
            if (cr_id == default)
            {
                Response.StatusCode = 404;//没找到该评论
                return Json(new { crID = cr_id, result = "NOT FOUND" });
            }

            var info = entity.ArticleCommentReply.Find(cr_id);
            info.ArticleCrContent = newContent;
            entity.Entry(info).State = EntityState.Modified;
            entity.SaveChanges();

            Response.StatusCode = 200;//成功
            return Json(new { UserName = nick_name, crID=articlecrid,NewContent = newContent });
        }

        /// <summary>
        /// 评论评论
        /// 名为nick_name用户评论id为articlecrid的评论
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="articlecommentID1"></param>
        /// <param name="articlecommentID2"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult createCommentCommentByID(string nick_name, int articlecommentID1, string content)
        {
            var userid =
                    (from c in entity.User
                     where c.NickName == nick_name
                     select c.UserId).Distinct();
            var id = userid.FirstOrDefault();
            if (id == default)
            {
                Response.StatusCode = 405;//没找到该用户
                return Json(new { UserID = id, result = "NOT FOUND" });
            }

            var articleid =
                (from c in entity.ArticleCommentReply
                 where (c.ArticleCrId == articlecommentID1)
                 select c.ArticleId).Distinct();
            var A_id = articleid.FirstOrDefault();
            var crid =
                (from c in entity.ArticleCommentReply
                 where (c.ArticleCrId== articlecommentID1 )
                 select c.ArticleCrId).Distinct();
            var CR_id = crid.FirstOrDefault();
            if (CR_id == default)
            {
                Response.StatusCode = 404;
                return Json(new { aclid = articlecommentID1, result = " NOT FOUND" });
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

            Response.StatusCode = 200;//成功
            return Json(new { articleID = A_id, articlecomment = content });
        }

        /*
         public IActionResult Index()
        {
            return View();
        }

       private string GetDebuggerDisplay()
        {
            return ToString();
       }
         */
        
    }
  
}

 
 
 
 
