﻿using Api.Socioboard.Model;
using Domain.Socioboard.Models.Mongo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Socioboard.GoogleLib.App.Core;
using Socioboard.GoogleLib.Authentication;
using Socioboard.GoogleLib.GAnalytics.Core.AnalyticsMethod;
using Socioboard.GoogleLib.Youtube.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Socioboard.Repositories
{
    public static class GplusRepository
    {

        public static Domain.Socioboard.Models.Googleplusaccounts getGPlusAccount(string GPlusUserId, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            try
            {
                Domain.Socioboard.Models.Googleplusaccounts inMemGplusAcc = _redisCache.Get<Domain.Socioboard.Models.Googleplusaccounts>(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + GPlusUserId);
                if (inMemGplusAcc != null)
                {
                    return inMemGplusAcc;
                }
            }
            catch { }

            List<Domain.Socioboard.Models.Googleplusaccounts> lstGPlusAcc = dbr.Find<Domain.Socioboard.Models.Googleplusaccounts>(t => t.GpUserId.Equals(GPlusUserId) && t.IsActive).ToList();
            if (lstGPlusAcc != null && lstGPlusAcc.Count() > 0)
            {
                _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + GPlusUserId, lstGPlusAcc.First());
                return lstGPlusAcc.First();
            }
            else
            {
                return null;
            }



        }

        public static Domain.Socioboard.Models.GoogleAnalyticsAccount getGAAccount(string GaAccountId, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            try
            {
                Domain.Socioboard.Models.GoogleAnalyticsAccount inMemGAAcc = _redisCache.Get<Domain.Socioboard.Models.GoogleAnalyticsAccount>(Domain.Socioboard.Consatants.SocioboardConsts.CacheGAAccount + GaAccountId);
                if (inMemGAAcc != null)
                {
                    return inMemGAAcc;
                }
            }
            catch { }

            List<Domain.Socioboard.Models.GoogleAnalyticsAccount> lstGAAcc = dbr.Find<Domain.Socioboard.Models.GoogleAnalyticsAccount>(t => t.GaProfileId.Equals(GaAccountId)).ToList();
            if (lstGAAcc != null && lstGAAcc.Count() > 0)
            {
                _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + GaAccountId, lstGAAcc.First());
                return lstGAAcc.First();
            }
            else
            {
                return null;
            }



        }
        public static int AddGplusAccount(JObject profile,  Model.DatabaseRepository dbr, Int64 userId, Int64 groupId, string accessToken,string refreshToken ,Helper.Cache _redisCache, Helper.AppSettings settings, ILogger _logger)
        {
            int isSaved = 0;
            Domain.Socioboard.Models.Googleplusaccounts gplusAcc = GplusRepository.getGPlusAccount(Convert.ToString(profile["id"]), _redisCache, dbr);
            oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus(settings.GoogleConsumerKey,settings.GoogleConsumerSecret,settings.GoogleRedirectUri);
           
            if (gplusAcc != null && gplusAcc.IsActive == false)
            {
                gplusAcc.IsActive = true;
                gplusAcc.UserId = userId;
                gplusAcc.AccessToken = accessToken;
                gplusAcc.RefreshToken = refreshToken;
                gplusAcc.EntryDate = DateTime.UtcNow;
                try
                {
                    gplusAcc.GpUserName = profile["displayName"].ToString();
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpUserName = profile["name"].ToString();
                    }
                    catch { }
                }
                try
                {
                    gplusAcc.GpProfileImage = Convert.ToString(profile["image"]["url"]);
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpProfileImage = Convert.ToString(profile["picture"]);
                    }
                    catch { }

                }
                gplusAcc.AccessToken = accessToken;
                try
                {
                    gplusAcc.about = Convert.ToString(profile["tagline"]);
                }
                catch 
                {
                    gplusAcc.about = "";
                }
                try
                {
                    gplusAcc.college = Convert.ToString(profile["organizations"][0]["name"]);
                }
                catch
                {
                    gplusAcc.college = "";
                }
                try
                {
                    gplusAcc.coverPic = Convert.ToString(profile["cover"]["coverPhoto"]["url"]);
                }
                catch
                {
                    gplusAcc.coverPic = "";
                }
                try
                {
                    gplusAcc.education = Convert.ToString(profile["organizations"][0]["type"]);
                }
                catch
                {
                    gplusAcc.education = "";
                }
                try
                {
                    gplusAcc.EmailId = Convert.ToString(profile["emails"][0]["value"]);
                }
                catch
                {
                    gplusAcc.EmailId = "";
                }
                try
                {
                    gplusAcc.gender = Convert.ToString(profile["gender"]);
                }
                catch
                {
                    gplusAcc.gender = "";
                }
                try
                {
                    gplusAcc.workPosition = Convert.ToString(profile["occupation"]);
                }
                catch
                {
                    gplusAcc.workPosition = "";
                }
                gplusAcc.LastUpdate = DateTime.UtcNow;
                #region Get_InYourCircles
                try
                {
                    string _InyourCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleList.Replace("[userId]", gplusAcc.GpUserId).Replace("[collection]", "visible") + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_InyourCircles = JObject.Parse(_InyourCircles);
                    gplusAcc.InYourCircles = Convert.ToInt32(J_InyourCircles["totalItems"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.InYourCircles = 0;
                }
                #endregion

                #region Get_HaveYouInCircles
                try
                {
                    string _HaveYouInCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleProfile + gplusAcc.GpUserId + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_HaveYouInCircles = JObject.Parse(_HaveYouInCircles);
                    gplusAcc.HaveYouInCircles = Convert.ToInt32(J_HaveYouInCircles["circledByCount"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.HaveYouInCircles = 0;
                }
                #endregion




                dbr.Update<Domain.Socioboard.Models.Googleplusaccounts>(gplusAcc);
            }
            else
            {
                gplusAcc = new Domain.Socioboard.Models.Googleplusaccounts();
                gplusAcc.UserId = userId;
                gplusAcc.GpUserId = profile["id"].ToString();
                try {
                    gplusAcc.GpUserName = profile["displayName"].ToString();
                }
                catch {
                    try {
                        gplusAcc.GpUserName = profile["name"].ToString();
                    }
                    catch { }
                }
                gplusAcc.IsActive = true;
                gplusAcc.AccessToken = accessToken;
                gplusAcc.RefreshToken = refreshToken;
                gplusAcc.EntryDate = DateTime.UtcNow;
                try {
                    gplusAcc.GpProfileImage = Convert.ToString(profile["image"]["url"]);
                }
                catch
                {
                    try
                    {
                        gplusAcc.GpProfileImage = Convert.ToString(profile["picture"]);
                    }
                    catch { }
                    
                }
                gplusAcc.AccessToken = accessToken;
                try
                {
                    gplusAcc.about = Convert.ToString(profile["tagline"]);
                }
                catch
                {
                    gplusAcc.about = "";
                }
                try
                {
                    gplusAcc.college = Convert.ToString(profile["organizations"][0]["name"]);
                }
                catch
                {
                    gplusAcc.college = "";
                }
                try
                {
                    gplusAcc.coverPic = Convert.ToString(profile["cover"]["coverPhoto"]["url"]);
                }
                catch
                {
                    gplusAcc.coverPic = "";
                }
                try
                {
                    gplusAcc.education = Convert.ToString(profile["organizations"][0]["type"]);
                }
                catch
                {
                    gplusAcc.education = "";
                }
                try
                {
                    gplusAcc.EmailId = Convert.ToString(profile["emails"][0]["value"]);
                }
                catch
                {
                    try {
                        try
                        {
                            gplusAcc.EmailId = Convert.ToString(profile["email"]);
                        }
                        catch { }
                    } catch { }
                    gplusAcc.EmailId = "";
                }
                try
                {
                    gplusAcc.gender = Convert.ToString(profile["gender"]);
                }
                catch
                {
                    gplusAcc.gender = "";
                }
                try
                {
                    gplusAcc.workPosition = Convert.ToString(profile["occupation"]);
                }
                catch
                {
                    gplusAcc.workPosition = "";
                }
                gplusAcc.LastUpdate = DateTime.UtcNow;


                #region Get_InYourCircles
                try
                {
                    string _InyourCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleList.Replace("[userId]", gplusAcc.GpUserId).Replace("[collection]", "visible") + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_InyourCircles = JObject.Parse(_InyourCircles);
                    gplusAcc.InYourCircles = Convert.ToInt32(J_InyourCircles["totalItems"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.InYourCircles = 0;
                }
                #endregion

                #region Get_HaveYouInCircles
                try
                {
                    string _HaveYouInCircles = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetPeopleProfile + gplusAcc.GpUserId + "?key=" + settings.GoogleApiKey, accessToken);
                    JObject J_HaveYouInCircles = JObject.Parse(_HaveYouInCircles);
                    gplusAcc.HaveYouInCircles = Convert.ToInt32(J_HaveYouInCircles["circledByCount"].ToString());
                }
                catch (Exception ex)
                {
                    gplusAcc.HaveYouInCircles = 0;
                }
                #endregion

                 isSaved = dbr.Add<Domain.Socioboard.Models.Googleplusaccounts>(gplusAcc);
            }
           
            if (isSaved == 1)
            {
                List<Domain.Socioboard.Models.Googleplusaccounts> lstgplusAcc = dbr.Find<Domain.Socioboard.Models.Googleplusaccounts>(t => t.GpUserId.Equals(gplusAcc.GpUserId)).ToList();
                if (lstgplusAcc != null && lstgplusAcc.Count() > 0)
                {
                    isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstgplusAcc.First().GpUserId, lstgplusAcc.First().GpUserName, userId, lstgplusAcc.First().GpProfileImage, Domain.Socioboard.Enum.SocialProfileType.GPlus, dbr);
                    //codes to delete cache
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);

                    
                    if (isSaved == 1)
                    {
                        new Thread(delegate ()
                        {
                            GetUserActivities(gplusAcc.GpUserId,gplusAcc.AccessToken,settings,_logger);

                        }).Start();


                    }
                }

            }
            return isSaved;

        }


        public static void GetUserActivities(string ProfileId, string AcessToken, Helper.AppSettings settings, ILogger _logger)
        {
            oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus(settings.GoogleConsumerKey,settings.GoogleConsumerSecret,settings.GoogleRedirectUri);
            try
            {
                //Domain.Socioboard.Domain.GooglePlusActivities _GooglePlusActivities = null;
                MongoGplusFeed _GooglePlusActivities;
                string _Activities = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetActivitiesList.Replace("[ProfileId]", ProfileId) + "?key=" + settings.GoogleApiKey, AcessToken);
                JObject J_Activities = JObject.Parse(_Activities);
                foreach (var item in J_Activities["items"])
                {
                    _GooglePlusActivities = new MongoGplusFeed();
                    _GooglePlusActivities.Id = ObjectId.GenerateNewId();
                    //_GooglePlusActivities.UserId = Guid.Parse(UserId);
                    _GooglePlusActivities.GpUserId = ProfileId;
                    try
                    {
                        _GooglePlusActivities.FromUserName = item["actor"]["displayName"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.FromId = item["actor"]["id"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.ActivityId = item["id"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.ActivityUrl = item["url"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.Title = item["title"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.FromProfileImage = item["actor"]["image"]["url"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.Content = item["object"]["content"].ToString();
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.PublishedDate = Convert.ToDateTime(item["published"].ToString()).ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.PlusonersCount = Convert.ToInt32(item["object"]["plusoners"]["totalItems"].ToString());
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.RepliesCount = Convert.ToInt32(item["object"]["replies"]["totalItems"].ToString());
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.ResharersCount = Convert.ToInt32(item["object"]["resharers"]["totalItems"].ToString());
                    }
                    catch { }
                    try
                    {
                        _GooglePlusActivities.AttachmentType = item["object"]["attachments"][0]["objectType"].ToString();
                        if (_GooglePlusActivities.AttachmentType == "video")
                        {
                            _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["embed"]["url"].ToString();
                        }
                        else if (_GooglePlusActivities.AttachmentType == "photo")
                        {
                            _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["image"]["url"].ToString();
                        }
                        else if (_GooglePlusActivities.AttachmentType == "album")
                        {
                            _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["thumbnails"][0]["image"]["url"].ToString();
                        }
                        else if (_GooglePlusActivities.AttachmentType == "article")
                        {
                            try
                            {
                                _GooglePlusActivities.Attachment = item["object"]["attachments"][0]["image"]["url"].ToString();
                            }
                            catch { }
                            try
                            {
                                _GooglePlusActivities.ArticleDisplayname = item["object"]["attachments"][0]["displayName"].ToString();
                            }
                            catch { }
                            try
                            {
                                _GooglePlusActivities.ArticleContent = item["object"]["attachments"][0]["content"].ToString();
                            }
                            catch { }
                            try
                            {
                                _GooglePlusActivities.Link = item["object"]["attachments"][0]["url"].ToString();
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        _GooglePlusActivities.AttachmentType = "note";
                        _GooglePlusActivities.Attachment = "";
                    }
                    MongoRepository gplusFeedRepo = new MongoRepository("MongoGplusFeed", settings);
                    var ret = gplusFeedRepo.Find<MongoGplusFeed>(t => t.ActivityId.Equals(_GooglePlusActivities.ActivityId));
                    var task = Task.Run(async () => {
                        return await ret;
                    });
                    int count = task.Result.Count;
                    if (count < 1)
                    {
                        gplusFeedRepo.Add(_GooglePlusActivities);

                    }
                    else
                    {
                        FilterDefinition<BsonDocument> filter = new BsonDocument("ActivityId", _GooglePlusActivities.ActivityId);
                        var update = Builders<BsonDocument>.Update.Set("PlusonersCount", _GooglePlusActivities.PlusonersCount).Set("RepliesCount", _GooglePlusActivities.RepliesCount).Set("ResharersCount", _GooglePlusActivities.ResharersCount);
                        gplusFeedRepo.Update<MongoGplusFeed>(update, filter);
                    }
                    new Thread(delegate () {
                       GetGooglePlusComments(_GooglePlusActivities.ActivityId, AcessToken, ProfileId,settings,_logger);
                    }).Start();

                    new Thread(delegate ()
                    {
                        //GetGooglePlusLikes(_GooglePlusActivities.ActivityId, AcessToken, ProfileId, status);
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUserActivities => " + ex.Message);
            }
        }

        public static int AddGaSites(string profiledata, long userId, long groupId, Helper.Cache _redisCache, Helper.AppSettings _appSettings, Model.DatabaseRepository dbr, IHostingEnvironment _appEnv)
        {
            int isSaved = 0;
            Analytics _Analytics = new Analytics(_appSettings.GoogleConsumerKey, _appSettings.GoogleConsumerSecret, _appSettings.GoogleRedirectUri);
            Domain.Socioboard.Models.GoogleAnalyticsAccount _GoogleAnalyticsAccount;
            string[] GAdata = Regex.Split(profiledata, "<:>");
            _GoogleAnalyticsAccount = Repositories.GplusRepository.getGAAccount(GAdata[5], _redisCache, dbr);

            if (_GoogleAnalyticsAccount != null && _GoogleAnalyticsAccount.IsActive == false)
            {
                try
                {
                    _GoogleAnalyticsAccount.UserId = userId;
                    _GoogleAnalyticsAccount.IsActive = true;
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;
                    _GoogleAnalyticsAccount.EmailId = GAdata[4];
                    _GoogleAnalyticsAccount.GaAccountId = GAdata[2];
                    _GoogleAnalyticsAccount.GaAccountName = GAdata[3];
                    _GoogleAnalyticsAccount.GaWebPropertyId = GAdata[7];
                    _GoogleAnalyticsAccount.GaProfileId = GAdata[5];
                    _GoogleAnalyticsAccount.GaProfileName = GAdata[6];
                    _GoogleAnalyticsAccount.AccessToken = GAdata[0];
                    _GoogleAnalyticsAccount.RefreshToken = GAdata[1];
                    _GoogleAnalyticsAccount.WebsiteUrl = GAdata[8];
                    string visits = string.Empty;
                    string pageviews = string.Empty;
                    try
                    {
                        string analytics = _Analytics.getAnalyticsData(GAdata[5], "ga:visits,ga:pageviews", DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"), GAdata[0]);
                        JObject JData = JObject.Parse(analytics);
                        visits = JData["totalsForAllResults"]["ga:visits"].ToString();
                        pageviews = JData["totalsForAllResults"]["ga:pageviews"].ToString();
                    }
                    catch (Exception ex)
                    {
                        visits = "0";
                        pageviews = "0";
                    }
                    _GoogleAnalyticsAccount.Views = Double.Parse(pageviews);
                    _GoogleAnalyticsAccount.Visits = Double.Parse(visits);
                    _GoogleAnalyticsAccount.ProfilePicUrl = "https://www.socioboard.com/Contents/Socioboard/images/analytics_img.png";
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;


                }
                catch (Exception ex)
                {

                }
                dbr.Update<Domain.Socioboard.Models.GoogleAnalyticsAccount>(_GoogleAnalyticsAccount);
            }
            else
            {
                try
                {
                    _GoogleAnalyticsAccount = new Domain.Socioboard.Models.GoogleAnalyticsAccount();
                    _GoogleAnalyticsAccount.UserId = userId;
                    _GoogleAnalyticsAccount.IsActive = true;
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;
                    _GoogleAnalyticsAccount.EmailId = GAdata[4];
                    _GoogleAnalyticsAccount.GaAccountId = GAdata[2];
                    _GoogleAnalyticsAccount.GaAccountName = GAdata[3];
                    _GoogleAnalyticsAccount.GaWebPropertyId = GAdata[7];
                    _GoogleAnalyticsAccount.GaProfileId = GAdata[5];
                    _GoogleAnalyticsAccount.GaProfileName = GAdata[6];
                    _GoogleAnalyticsAccount.AccessToken = GAdata[0];
                    _GoogleAnalyticsAccount.RefreshToken = GAdata[1];
                    _GoogleAnalyticsAccount.WebsiteUrl = GAdata[8];
                    string visits = string.Empty;
                    string pageviews = string.Empty;
                    try
                    {
                        string analytics = _Analytics.getAnalyticsData(GAdata[5], "ga:visits,ga:pageviews", DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"), GAdata[0]);
                        JObject JData = JObject.Parse(analytics);
                        visits = JData["totalsForAllResults"]["ga:visits"].ToString();
                        pageviews = JData["totalsForAllResults"]["ga:pageviews"].ToString();
                    }
                    catch (Exception ex)
                    {
                        visits = "0";
                        pageviews = "0";
                    }
                    _GoogleAnalyticsAccount.Views = Double.Parse(pageviews);
                    _GoogleAnalyticsAccount.Visits = Double.Parse(visits);
                    _GoogleAnalyticsAccount.ProfilePicUrl = "https://www.socioboard.com/Themes/Socioboard/Contents/img/analytics_img.png";
                    _GoogleAnalyticsAccount.EntryDate = DateTime.UtcNow;


                }
                catch (Exception ex)
                {

                }
                isSaved = dbr.Add<Domain.Socioboard.Models.GoogleAnalyticsAccount>(_GoogleAnalyticsAccount);
            }

            if (isSaved == 1)
            {
                List<Domain.Socioboard.Models.GoogleAnalyticsAccount> lstgaAcc = dbr.Find<Domain.Socioboard.Models.GoogleAnalyticsAccount>(t => t.GaProfileId.Equals(_GoogleAnalyticsAccount.GaProfileId)).ToList();
                if (lstgaAcc != null && lstgaAcc.Count() > 0)
                {
                    isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstgaAcc.First().GaProfileId, lstgaAcc.First().GaProfileName, userId, lstgaAcc.First().ProfilePicUrl, Domain.Socioboard.Enum.SocialProfileType.GoogleAnalytics, dbr);
                    //codes to delete cache
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);


                }

            }
            return isSaved;
        }


        public static void GetGooglePlusComments(string feedId, string AccessToken, string profileId, Helper.AppSettings settings, ILogger _logger)
        {
            MongoRepository gplusCommentRepo = new MongoRepository("GoogleplusComments",settings);
            oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus(settings.GoogleConsumerKey,settings.GoogleConsumerSecret,settings.GoogleRedirectUri);

            MongoGoogleplusComments _GoogleplusComments = new MongoGoogleplusComments();
            try
            {
                string _Comments = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strGetCommentListByActivityId.Replace("[ActivityId]", feedId) + "?key=" + settings.GoogleApiKey, AccessToken);
                JObject J_Comments = JObject.Parse(_Comments);
                List<MongoGoogleplusComments> lstGoogleplusComments = new List<MongoGoogleplusComments>();
                foreach (var item in J_Comments["items"])
                {
                    try
                    {
                        _GoogleplusComments.Id = ObjectId.GenerateNewId();
                        _GoogleplusComments.Comment = item["object"]["content"].ToString();
                        _GoogleplusComments.CommentId = item["id"].ToString();
                        _GoogleplusComments.CreatedDate = Convert.ToDateTime(item["published"]).ToString("yyyy/MM/dd HH:mm:ss");
                        _GoogleplusComments.FeedId = feedId;
                        _GoogleplusComments.FromId = item["actor"]["id"].ToString();
                        _GoogleplusComments.FromImageUrl = item["actor"]["image"]["url"].ToString();
                        _GoogleplusComments.FromName = item["actor"]["url"].ToString();
                        _GoogleplusComments.FromUrl = item["actor"]["url"].ToString();
                        _GoogleplusComments.GplusUserId = profileId;

                        lstGoogleplusComments.Add(_GoogleplusComments);

                        //if (!objGoogleplusCommentsRepository.IsExist(_GoogleplusComments.CommentId))
                        //{
                        //    objGoogleplusCommentsRepository.Add(_GoogleplusComments);
                        //}

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }

                gplusCommentRepo.AddList(lstGoogleplusComments);

            }
            catch (Exception ex)
            {
            }

        }

        //public void GetGooglePlusLikes(string feedId, string AccessToken, string ProfileId, int Status, Helper.AppSettings settings, ILogger _logger)
        //{
        //    oAuthTokenGPlus ObjoAuthTokenGPlus = new oAuthTokenGPlus();

        //    Domain.Socioboard.Domain.GoogleplusLike _GoogleplusLike = new Domain.Socioboard.Domain.GoogleplusLike();
        //    try
        //    {
        //        string _Likes = ObjoAuthTokenGPlus.APIWebRequestToGetUserInfo(Globals.strLike.Replace("[ActivityId]", feedId) + "?key=" + ConfigurationManager.AppSettings["Api_Key"].ToString(), AccessToken);
        //        JObject J_Likes = JObject.Parse(_Likes);

        //        foreach (var item in J_Likes["items"])
        //        {
        //            try
        //            {
        //                _GoogleplusLike.Id = Guid.NewGuid();
        //                _GoogleplusLike.FromId = item["id"].ToString();
        //                _GoogleplusLike.FromImageUrl = item["image"]["url"].ToString();
        //                _GoogleplusLike.FromName = item["displayName"].ToString();
        //                _GoogleplusLike.ProfileId = ProfileId;
        //                _GoogleplusLike.FromUrl = item["url"].ToString();
        //                _GoogleplusLike.FeedId = feedId;

        //                if (!objGoogleplusCommentsRepository.IsLikeExist(_GoogleplusLike.FromId, feedId))
        //                {
        //                    objGoogleplusCommentsRepository.AddLikes(_GoogleplusLike);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.Error(ex.Message);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //    }

        //}

        public static string DeleteProfile(Model.DatabaseRepository dbr, string profileId, long userId, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.GoogleAnalyticsAccount fbAcc = dbr.Find<Domain.Socioboard.Models.GoogleAnalyticsAccount>(t => t.GaProfileId.Equals(profileId) && t.UserId == userId && t.IsActive).FirstOrDefault();
            if (fbAcc != null)
            {
                fbAcc.IsActive = false;
                dbr.Update<Domain.Socioboard.Models.GoogleAnalyticsAccount>(fbAcc);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGAAccount + profileId);
                return "Deleted";
            }
            else
            {
                return "Account Not Exist";
            }
        }

        public static string DeleteGplusProfile(Model.DatabaseRepository dbr, string profileId, long userId, Helper.Cache _redisCache, Helper.AppSettings _appSettings)
        {
            Domain.Socioboard.Models.Googleplusaccounts fbAcc = dbr.Find<Domain.Socioboard.Models.Googleplusaccounts>(t => t.GpUserId.Equals(profileId) && t.UserId == userId && t.IsActive).FirstOrDefault();
            if (fbAcc != null)
            {
                fbAcc.IsActive = false;
                dbr.Update<Domain.Socioboard.Models.Googleplusaccounts>(fbAcc);
                _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusAccount + profileId);
                return "Deleted";
            }
            else
            {
                return "Account Not Exist";
            }
        }
        public static List<Domain.Socioboard.Models.Mongo.MongoGplusFeed> getgoogleplusActivity(string profileId,Helper.Cache _redisCache,Helper.AppSettings _appSettings)
        {
            MongoRepository gplusFeedRepo = new MongoRepository("MongoGplusFeed", _appSettings);
            List<Domain.Socioboard.Models.Mongo.MongoGplusFeed> iMmemMongoGplusFeed = _redisCache.Get<List<Domain.Socioboard.Models.Mongo.MongoGplusFeed>>(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusRecent100Feeds + profileId);
            if(iMmemMongoGplusFeed!=null && iMmemMongoGplusFeed.Count>0)
            {
                return iMmemMongoGplusFeed;
            }
            else
            {
                var builder = Builders<MongoGplusFeed>.Sort;
                var sort = builder.Descending(t => t.PublishedDate);
                var ret = gplusFeedRepo.FindWithRange<Domain.Socioboard.Models.Mongo.MongoGplusFeed>(t => t.GpUserId.Equals(profileId), sort, 0, 100);
                var task=Task.Run(async() =>{
                    return await ret;
                });
                IList<Domain.Socioboard.Models.Mongo.MongoGplusFeed> lstMongoGplusFeed = task.Result.ToList();
                if (lstMongoGplusFeed.Count>0)
                {
                    _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheGplusRecent100Feeds + profileId, lstMongoGplusFeed.ToList());
                }
                return lstMongoGplusFeed.ToList();
            }
        }


        #region Repository codes for Youtube Channel.....


        public static Domain.Socioboard.Models.YoutubeChannel getYTChannel(string YtChannelId, Helper.Cache _redisCache, Model.DatabaseRepository dbr)
        {
            try
            {
                Domain.Socioboard.Models.YoutubeChannel inMemYTChannel = _redisCache.Get<Domain.Socioboard.Models.YoutubeChannel>(Domain.Socioboard.Consatants.SocioboardConsts.CacheYTChannel + YtChannelId);
                if (inMemYTChannel != null)
                {
                    return inMemYTChannel;
                }
            }
            catch { }

            List<Domain.Socioboard.Models.YoutubeChannel> lstYTChannel = dbr.Find<Domain.Socioboard.Models.YoutubeChannel>(t => t.YtubeChannelId.Equals(YtChannelId)).ToList();
            if (lstYTChannel != null && lstYTChannel.Count() > 0)
            {
                _redisCache.Set(Domain.Socioboard.Consatants.SocioboardConsts.CacheYTChannel + YtChannelId, lstYTChannel.First());
                return lstYTChannel.First();
            }
            else
            {
                return null;
            }



        }


        public static int AddYoutubeChannels(string profiledata, long userId, long groupId, Helper.Cache _redisCache, Helper.AppSettings _appSettings, Model.DatabaseRepository dbr, IHostingEnvironment _appEnv)
        {
            int isSaved = 0;
            Channels _Channels = new Channels("575089347457-74q0u81gj88ve5bfdmbklcf2dnc0353q.apps.googleusercontent.com", "JRtS_TaeYpKOJWBCqt9h8-iG", "http://localhost:9821/GoogleManager/Google");
            Domain.Socioboard.Models.YoutubeChannel _YoutubeChannel;
            string[] YTdata = Regex.Split(profiledata, "<:>");
            _YoutubeChannel = Repositories.GplusRepository.getYTChannel(YTdata[2], _redisCache, dbr);


            if (_YoutubeChannel != null)
            {
                try
                {

                    _YoutubeChannel.UserId = userId;
                    _YoutubeChannel.YtubeChannelId = YTdata[2];
                    _YoutubeChannel.YtubeChannelName = YTdata[3];
                    _YoutubeChannel.ChannelpicUrl = YTdata[9];
                    _YoutubeChannel.WebsiteUrl = "https://www.youtube.com/channel/" + YTdata[2];
                    _YoutubeChannel.EntryDate = DateTime.UtcNow;
                    _YoutubeChannel.YtubeChannelDescription = YTdata[4];
                    _YoutubeChannel.IsActive = true;
                    _YoutubeChannel.AccessToken = YTdata[0];
                    _YoutubeChannel.RefreshToken = YTdata[1];
                    _YoutubeChannel.PublishingDate = Convert.ToDateTime(YTdata[5]);
                    _YoutubeChannel.VideosCount = Convert.ToDouble(YTdata[8]);
                    _YoutubeChannel.CommentsCount = Convert.ToDouble(YTdata[7]);
                    _YoutubeChannel.SubscribersCount = Convert.ToDouble(YTdata[10]);
                    _YoutubeChannel.ViewsCount = Convert.ToDouble(YTdata[6]);

                }
                catch (Exception ex)
                {

                }
                isSaved = dbr.Update<Domain.Socioboard.Models.YoutubeChannel>(_YoutubeChannel);
            }
            else
            {
                _YoutubeChannel = new Domain.Socioboard.Models.YoutubeChannel();
                try
                {
                    _YoutubeChannel.UserId = userId;
                    _YoutubeChannel.YtubeChannelId = YTdata[2];
                    _YoutubeChannel.YtubeChannelName = YTdata[3];
                    _YoutubeChannel.ChannelpicUrl = YTdata[9];
                    _YoutubeChannel.WebsiteUrl = "https://www.youtube.com/channel/" + YTdata[2];
                    _YoutubeChannel.EntryDate = DateTime.UtcNow;
                    _YoutubeChannel.YtubeChannelDescription = YTdata[4];
                    _YoutubeChannel.IsActive = true;
                    _YoutubeChannel.AccessToken = YTdata[0];
                    _YoutubeChannel.RefreshToken = YTdata[1];
                    _YoutubeChannel.PublishingDate = Convert.ToDateTime(YTdata[5]);
                    _YoutubeChannel.VideosCount = Convert.ToDouble(YTdata[8]);
                    _YoutubeChannel.CommentsCount = Convert.ToDouble(YTdata[7]);
                    _YoutubeChannel.SubscribersCount = Convert.ToDouble(YTdata[10]);
                    _YoutubeChannel.ViewsCount = Convert.ToDouble(YTdata[6]);

                }
                catch (Exception ex)
                {

                }
                isSaved = dbr.Add<Domain.Socioboard.Models.YoutubeChannel>(_YoutubeChannel);
            }

            if (isSaved == 1)
            {
                List<Domain.Socioboard.Models.YoutubeChannel> lstytChannel = dbr.Find<Domain.Socioboard.Models.YoutubeChannel>(t => t.YtubeChannelId.Equals(_YoutubeChannel.YtubeChannelId)).ToList();
                if (lstytChannel != null && lstytChannel.Count() > 0)
                {
                    isSaved = GroupProfilesRepository.AddGroupProfile(groupId, lstytChannel.First().YtubeChannelId, lstytChannel.First().YtubeChannelName, userId, lstytChannel.First().ChannelpicUrl, Domain.Socioboard.Enum.SocialProfileType.YouTube, dbr);
                    //codes to delete cache
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheUserProfileCount + userId);
                    _redisCache.Delete(Domain.Socioboard.Consatants.SocioboardConsts.CacheGroupProfiles + groupId);

                }

            }
            return isSaved;
        }

        #endregion

    }
}
