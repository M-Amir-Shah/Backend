using Backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using static System.Net.Mime.MediaTypeNames;

namespace FinancialAidAllocation.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class CommitteeController : ApiController
    {
        FAAToolEntities db = new FAAToolEntities();

        [HttpGet]
        public HttpResponseMessage CommitteeInfo(int id)
        {
            try 
            { 
                return Request.CreateResponse(HttpStatusCode.OK, db.Faculties.Where(f => f.facultyId == id).FirstOrDefault());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetApplication(int id)
        {
            try
            {
    
                var result = db.Applications
            .GroupJoin(
                db.Suggestions.Where(s => s.committeeId == id),
        application => application.applicationID,
        suggestion => suggestion.applicationId,
        (application, suggestions) => new
        {
            Application = application,
            Suggestions = suggestions
        }).Where(joinResult => !joinResult.Suggestions.Any()).Select
        (joinResult => joinResult.Application).Distinct().Join(db.Students,
                            application => application.studentId,
                            student => student.student_id,
                    (application, student) => new
                    {
                        student.arid_no,
            student.name,
            student.student_id,
            student.father_name,
            student.gender,
            student.degree,
            student.cgpa,
            student.semester,
            student.section,
            student.profile_image,
            application.applicationDate,
            application.reason,
            application.requiredAmount,
            application.EvidenceDocuments,
            application.applicationID,
            application.session,
            application.father_status,
            application.jobtitle,
            application.salary,
            application.guardian_contact,
            application.house,
            application.guardian_name,
        });
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }
        [HttpGet]
        public HttpResponseMessage GetBalance()
        {
            var paisa = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();

            return Request.CreateResponse(HttpStatusCode.OK, paisa.remainingAmount);
        }

        [HttpGet]
        public HttpResponseMessage CommitteeMembers(int id)
        {
            try
            {
                var members = db.Committees.Where(c=>c.committeeId==id).Join
                    (
                    db.Faculties,
                    c => c.facultyId,
                    f => f.facultyId,
                    (c, f) => new
                    {
                        c.committeeId,
                        f.name,
                        f.contactNo,
                        f.profilePic,
                    }
                    ).FirstOrDefault();
                return Request.CreateResponse(HttpStatusCode.OK, members);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage GiveSuggestion(int committeeId, String status, int applicationId, String comment, int amount)
        {
            try
            {
                var sug = db.Suggestions.Where(su => su.committeeId == committeeId && su.applicationId == applicationId).FirstOrDefault();
                String amt = null;
                if (amount > 0)
                {
                    amt = amount.ToString();
                }

                if (sug == null)
                {
                    Suggestion s = new Suggestion();
                    s.comment = comment;
                    s.committeeId = committeeId;
                    s.applicationId = applicationId;
                    s.status = status;
                    s.amount = amt;
                    db.Suggestions.Add(s);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK,"SuccessFully.");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage DecideMeritBaseApplication(int id, String status)
        {
            try
            {
                var budget = db.Budgets.OrderByDescending(b => b.budgetId).FirstOrDefault();

                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var record = db.MeritBases.Where(mr => mr.studentId == id && mr.session == session.session1).FirstOrDefault();
                var result = db.FinancialAids.Where(fn => fn.applicationId == record.studentId).FirstOrDefault();
                if (record != null && status == "Accepted")
                {
                    result.amount = "15000";
                    result.applicationStatus = status;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Accepted");
                }
                else if (status == "Rejected")
                {

                    var reservetoper = db.Students.Join
                        (
                            db.Reserves.Where(r => r.session == session.session1),
                            st => st.student_id,
                            res => res.student_id,
                            (st, res) => new
                            {
                                st,
                                res
                            }
                        );

                    var alternateToper = reservetoper.OrderByDescending(rt => rt.st.cgpa).Select(s => new
                    {
                        s.st,
                    }).FirstOrDefault();

                    if (reservetoper != null)
                    {

                        MeritBase m = new MeritBase();
                        m.session = session.session1;
                        m.position = record.position;
                        m.studentId = alternateToper.st.student_id;
                        db.MeritBases.Add(m);
                        db.SaveChanges();

                        FinancialAid f = new FinancialAid();
                        f.session = session.session1;
                        f.amount = result.amount;
                        f.applicationStatus = "Pending";
                        f.aidtype = "MeritBase";
                        f.applicationId = alternateToper.st.student_id;
                        db.FinancialAids.Add(f);
                        db.SaveChanges();
                        result.applicationStatus = status;
                        db.SaveChanges();

                        var del = db.Reserves.Where(rev => rev.student_id == alternateToper.st.student_id).FirstOrDefault();
                        db.Reserves.Remove(del);
                        db.SaveChanges();
                    }
                    else
                    {
                        result.applicationStatus = status;
                        db.SaveChanges();
                        var del = db.Reserves.Where(rev => rev.student_id == alternateToper.st.student_id).FirstOrDefault();
                        db.Reserves.Remove(del);
                        db.SaveChanges();
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "Rejected");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Not Found");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        private void AddStudentToDatabaseAndFinancialAid(Student student, string session, int position, double amount)
        {
            int semes = int.Parse(student.semester.ToString());
            String aridno = student.arid_no;

            if (!db.Students.Any(a => a.arid_no == aridno))
            {
                Student s = new Student
                {
                    arid_no = student.arid_no,
                    name = student.name,
                    semester = semes,
                    cgpa = student.cgpa,
                    prev_cgpa = student.prev_cgpa,
                    section = student.section,
                    degree = student.degree,
                    father_name = student.father_name,
                    gender = student.gender
                };
                db.Students.Add(s);
                db.SaveChanges();
            }

            var studentinfo = db.Students.FirstOrDefault(st => st.arid_no == aridno);


            String aridNo = aridno.Split('-')[2];

            if (!db.Users.Any(du => du.userName == aridNo))
            {
                User u = new User();
                u.userName = aridno;
                u.password = aridno;
                u.role = 4;
                u.profileId = studentinfo.student_id;

                db.Users.Add(u);
                db.SaveChanges();
            }

            var meritBase = new MeritBase
            {
                session = session,
                position = position,
                studentId = studentinfo.student_id
            };
            db.MeritBases.Add(meritBase);
            db.SaveChanges();

            var financialAid = new FinancialAid
            {
                applicationStatus = "Pending",
                session = session,
                aidtype = "MeritBase",
                applicationId = studentinfo.student_id,
                amount = amount.ToString()
            };
            db.FinancialAids.Add(financialAid);
            db.SaveChanges();
        }


    }
}
