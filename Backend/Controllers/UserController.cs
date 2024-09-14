using Backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web;

namespace FinancialAidAllocation.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class UserController : ApiController
    {
        FAAToolEntities db = new FAAToolEntities();

        [HttpGet]
        public HttpResponseMessage Login(String username, String password)
        {
            try
            {
                var account = db.Users.
                       Where(s => s.userName == username && s.password == password).FirstOrDefault();
                if (account != null)
                {
                        return Request.CreateResponse(HttpStatusCode.OK, account);
                }
                else
                {

                    return Request.CreateResponse(HttpStatusCode.NoContent);
                }
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

        }

        [HttpPost]
        public HttpResponseMessage SwitchRole(int memberid)
        {
            try
            {
                var user = db.Users.Where(u => u.profileId == memberid).FirstOrDefault();
                if (user.role == 3)
                {
                    var faculty = db.Committees.Where(c => c.facultyId == user.profileId).FirstOrDefault();
                    if (faculty != null)
                    {
                        user.role = 2;
                        user.profileId = faculty.committeeId;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK,user);
                    }
                    else 
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized,"you are not a committee member");
                    }
                }
                else if (user.role == 2)
                {
                    var committee = db.Committees.Where(f => f.committeeId==user.profileId).FirstOrDefault();
                    if (committee != null)
                    {
                        user.role = 3;
                        user.profileId = committee.facultyId;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK,user);
                    }
                    else 
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
