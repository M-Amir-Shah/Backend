using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.OleDb;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Backend.Models;
using System.Web.Http.Cors;

namespace FinancialAidAllocation.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AdminController : ApiController
    {
        FAAToolEntities db = new FAAToolEntities();


        [HttpGet]
        public HttpResponseMessage getAdminInfo(int id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Admins.Where(a => a.AdminID == id).FirstOrDefault());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddPreviousCgpa()
        {
            try
            {
                Random rnd = new Random();
                int minValue = 50;
                int maxValue = 100;

                var student = db.Students.ToList();
                for (int i = 0; i < student.Count; i++)
                {
                    int randomNumber = rnd.Next(minValue, maxValue);
                    int id = student[i].student_id;
                    var std = db.Students.Where(st => st.student_id == id).FirstOrDefault();
                    if (std.semester == 1)
                    {
                        std.prev_cgpa = double.Parse(randomNumber.ToString());
                    }
                    else if (std.semester >= 2)
                    {
                        if (std.cgpa > 3.9)
                        {
                            std.prev_cgpa = std.cgpa;
                        }
                        else if (std.cgpa >= 3.7 && std.cgpa < 3.9)
                        {
                            std.prev_cgpa = std.cgpa - 0.0001;

                        }
                        else if (std.cgpa >= 3.5 && std.cgpa < 3.7)
                        {
                            std.prev_cgpa = std.cgpa - 0.0008;
                        }
                        else if (std.cgpa >= 3.3 && std.cgpa < 3.5)
                        {
                            std.prev_cgpa = std.cgpa - 0.010;
                        }
                        else if (std.cgpa >= 3.0 && std.cgpa < 3.3)
                        {
                            std.prev_cgpa = std.cgpa - 0.02;
                        }
                        else if (std.cgpa >= 2.7 && std.cgpa < 3.0)
                        {
                            std.prev_cgpa = std.cgpa - 0.05;
                        }
                        else
                        {
                            std.prev_cgpa = std.cgpa - 0.15;
                        }
                    }
                }
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpPost]
        public HttpResponseMessage AddBudget(int amount)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                if (amount > 0)
                {
                    var paisa = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();
                    Budget b;
                    if (paisa != null)
                    {
                        b = new Budget();
                        b.budgetAmount = amount;
                        b.remainingAmount = paisa.remainingAmount + amount;
                        b.status = "A";
                        b.budget_session = session.session1;
                    }
                    else
                    {
                        b = new Budget();
                        b.budgetAmount = amount;
                        b.remainingAmount = amount;
                        b.status = "A";
                        b.budget_session = session.session1;

                    }
                    db.Budgets.Add(b);
                    db.SaveChanges();
                    var Remainbalance = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();
                    return Request.CreateResponse(HttpStatusCode.OK, Remainbalance.remainingAmount);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Add Some Ammount");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage FacultyMembers()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Faculties);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetMeritBaseShortListedStudent()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var students = db.MeritBases.Where(mr => mr.session == session.session1).Join(
                    db.Students,
                    m => m.studentId,
                    s => s.student_id,
                    (m, s) => new
                    {
                        s.student_id,
                        s.arid_no,
                        s.name,
                        s.profile_image,
                        s.gender,
                        s.degree,
                        s.semester,
                        s.section,
                        s.cgpa,
                        m.position
                    }
                    );
                if (students.ToList().Count < 1)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, students);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        private string GetAmount(int position, int first, int second, int third)
        {
            switch (position)
            {
                case 1:
                    return first.ToString();
                case 2:
                    return second.ToString();
                case 3:
                    return third.ToString();
                default:
                    return "0";
            }
        }

        [HttpGet]
        public HttpResponseMessage AcceptedApplication()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var applications = db.Applications.Where(ap => ap.session == session.session1)
                                          .GroupJoin(db.Suggestions,
                                              application => application.applicationID,
                                              suggestion => suggestion.applicationId,
                                              (application, suggestion) => new
                                              {
                                                  application,
                                                  suggestion
                                              });

                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (appplication, student) => new
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
                        appplication.application.applicationDate,
                        appplication.application.reason,
                        appplication.application.requiredAmount,
                        appplication.application.EvidenceDocuments,
                        appplication.application.applicationID,
                        appplication.application.session,
                        appplication.application.father_status,
                        appplication.application.jobtitle,
                        appplication.application.salary,
                        appplication.application.guardian_contact,
                        appplication.application.house,
                        appplication.application.guardian_name,
                        appplication.suggestion,
                    });

                var pendingApplications = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus,
                        f.amount
                    }
                );

                var acceptedApplications = pendingApplications.Where(p => p.applicationStatus.ToLower().Equals("accepted")).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, acceptedApplications);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }



        [HttpGet]
        public HttpResponseMessage CommitteeMembers()
        {
            try
            {

                var members = db.Committees.Where(com => com.status == "1").Join
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
                    );
                return Request.CreateResponse(HttpStatusCode.OK, members);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage BudgetHistory()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Budgets);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage getAllStudent()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Students);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage getAllMeritBase()
        {
            try
            {
                // Filter the results where aidtype is 'MeritBase'
                var meritBaseAids = db.FinancialAids.Where(aid => aid.aidtype == "MeritBase").ToList();

                return Request.CreateResponse(HttpStatusCode.OK, meritBaseAids);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage getAllBudget()
        {
            try
            {

                return Request.CreateResponse(HttpStatusCode.OK, db.Budgets);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage getPolicies()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var pol = db.Policies.Join(
                    db.Criteria,
                    p => p.id,
                    c => c.policy_id,
                    (p, c) => new
                    {
                        p,
                        c
                    }
                    );
                return Request.CreateResponse(HttpStatusCode.OK, pol.Where(po => po.p.session == session.session1));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage AddUser(String username, String password, int role, int? profileId)
        {
            /*
               1 student
               2 faculty
               3 committee
               4 Admin
             */
            try
            {
                var user = db.Users.Where(us => us.userName == username & us.password == password).FirstOrDefault();

                if (user == null)
                {
                    User u = new User();
                    u.userName = username;
                    u.password = password;
                    u.role = role;
                    u.profileId = profileId;
                    db.Users.Add(u);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Found, "Already Exist");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage AddFacultyMember()
        {
            try
            {
                var request = HttpContext.Current.Request;
                string name = request["name"];
                string contact = request["contact"];
                string password = request["password"];

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(contact) || string.IsNullOrEmpty(password))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Name, contact, and password are required.");
                }

                int Role = 3;
                var photo = request.Files["pic"];

                if (photo == null || photo.ContentLength == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Profile picture is required.");
                }
                string picpath = name + "." + photo.FileName.Split('.')[1];
                string uploadPath = HttpContext.Current.Server.MapPath("~/Content/ProfileImages/" + picpath);
                photo.SaveAs(uploadPath);

                Faculty f = new Faculty
                {
                    name = name,
                    contactNo = contact,
                    profilePic = picpath
                };

                db.Faculties.Add(f);
                db.SaveChanges();

                var facultyId = db.Faculties.Where(fa => fa.name == name && fa.contactNo == contact).Select(fa => fa.facultyId).FirstOrDefault();

                AddUser(name, password, Role, facultyId);

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Added Succeed");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddStudent()
        {
            try
            {
                var request = HttpContext.Current.Request;
                String name = request["name"];
                String cgpa = request["cgpa"];
                String semester = request["semester"];
                String aridno = request["aridno"];
                String gender = request["gender"];
                String fathername = request["fathername"];
                String degree = request["degree"];
                String section = request["section"];
                String password = request["password"];
                int Role = 4;
                var photo = request.Files["pic"];
                var provider = new MultipartMemoryStreamProvider();
                String picpath = name + "." + photo.FileName.Split('.')[1];
                photo.SaveAs(HttpContext.Current.Server.MapPath("~/Content/ProfileImages/" + picpath));
                Student s = new Student();
                s.section = section;
                s.name = name;
                s.gender = gender;
                s.arid_no = aridno;
                s.degree = degree;
                s.father_name = fathername;
                s.semester = int.Parse(semester);
                s.cgpa = double.Parse(cgpa);
                s.profile_image = picpath;
                db.Students.Add(s);
                db.SaveChanges();
                var studentId = db.Students.Where(sa => sa.arid_no == aridno).FirstOrDefault();
                AddUser(aridno, password, Role, studentId.student_id);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage getStudentApplicationStatus(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var count = db.Applications.Where(c => c.studentId == 1d && c.session == session.session1).FirstOrDefault();
                if (count != null)
                {
                    var result = db.Applications.Where(ap => ap.studentId == id).Join(
                    db.FinancialAids,
                    a => a.applicationID,
                    f => f.applicationId,
                    (a, f) => new
                    {
                        //                        a.applicationID,
                        f.applicationStatus,
                    }
                    );
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Not Submitted");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage RejectedApplication()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var applications = db.Applications.Where(ap => ap.session == session.session1)
                                          .GroupJoin(db.Suggestions,
                                              application => application.applicationID,
                                              suggestion => suggestion.applicationId,
                                              (application, suggestion) => new
                                              {
                                                  application,
                                                  suggestion
                                              });
                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (appplication, student) => new
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
                        appplication.application.applicationDate,
                        appplication.application.reason,
                        appplication.application.requiredAmount,
                        appplication.application.EvidenceDocuments,
                        appplication.application.applicationID,
                        appplication.application.session,
                        appplication.application.father_status,
                        appplication.application.jobtitle,
                        appplication.application.salary,
                        appplication.application.guardian_contact,
                        appplication.application.house,
                        appplication.application.guardian_name,
                        appplication.suggestion,
                    });

                var pendingapplication = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus
                    }
                    );

                return Request.CreateResponse(HttpStatusCode.OK, pendingapplication.Where(p => p.applicationStatus.ToLower().Equals("rejected")));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage MeritBaseRejectedApplication()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var rejectedApplication = db.MeritBases.Where(mr => mr.session == session.session1).Join(
                    db.FinancialAids.Where(fn => fn.session == session.session1 && fn.applicationStatus.ToLower() == "rejected"),
                    m => m.studentId,
                    f => f.applicationId,
                    (m, f) => new
                    {
                        m,
                        f
                    }
                    );
                var result = rejectedApplication.Join
                    (
                    db.Students,
                    ra => ra.m.studentId,
                    s => s.student_id,
                    (re, s) => new
                    {
                        re.f.aidtype,
                        re.f.applicationStatus,
                        s.name,
                        s.arid_no,
                        s.profile_image,
                        s.cgpa,
                        s.prev_cgpa,
                        s.degree,
                        s.gender,
                        s.father_name,
                        s.section,
                        s.semester,
                        s.student_id,
                    }
                    );
                return Request.CreateResponse(HttpStatusCode.OK, result);


            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        //[HttpPost]
        //public HttpResponseMessage AddCommitteeMember(int id)
        //{
        //    try
        //    {
        //        // Check if the faculty member is already in the committee
        //        var existingCommitteeMember = db.Committees.FirstOrDefault(c => c.facultyId == id);
        //        if (existingCommitteeMember == null)
        //        {
        //            // Add new committee member
        //            var newCommitteeMember = new Committee
        //            {
        //                facultyId = id,
        //                status = "1"
        //            };
        //            db.Committees.Add(newCommitteeMember);
        //            db.SaveChanges();

        //            // Update the user's role
        //            var user = db.Users.FirstOrDefault(u => u.profileId == id);
        //            if (user != null)
        //            {
        //                var committee = db.Committees.FirstOrDefault(cm => cm.facultyId == id);
        //                if (committee != null)
        //                {
        //                    user.role = 2;
        //                    user.profileId = committee.committeeId;
        //                    db.SaveChanges();
        //                }
        //            }

        //            return Request.CreateResponse(HttpStatusCode.OK);
        //        }
        //        else
        //        {
        //            // Faculty member is already in the committee
        //            return Request.CreateResponse(HttpStatusCode.Found, "Already Exist");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception details (consider using a logging framework)
        //        // Return an InternalServerError response with exception details
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}
        [HttpPost]
        public HttpResponseMessage AddCommitteeMember(int id)
        {
            try
            {
                // Check if the faculty member is already in the committee
                var existingCommitteeMember = db.Committees.FirstOrDefault(c => c.facultyId == id);
                if (existingCommitteeMember == null)
                {
                    // Add new committee member
                    var newCommitteeMember = new Committee
                    {
                        facultyId = id,
                        status = "1"
                    };
                    db.Committees.Add(newCommitteeMember);
                    db.SaveChanges();

                    // Update the user's role
                    var user = db.Users.FirstOrDefault(u => u.profileId == id);
                    if (user != null)
                    {
                        var committee = db.Committees.FirstOrDefault(cm => cm.facultyId == id);
                        if (committee != null)
                        {
                            user.role = 2;
                            user.profileId = committee.committeeId;
                            db.SaveChanges();
                        }
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, id);
                }
                else
                {
                    // Faculty member is already in the committee
                    return Request.CreateResponse(HttpStatusCode.Found, "Already Exists");
                }
            }
            catch (Exception ex)
            {
                // Log the exception details (consider using a logging framework)
                // Return an InternalServerError response with exception details
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }



        [HttpPost]
        public HttpResponseMessage AssignGrader(int facultyId, int studentId)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                // Check if the student is already assigned a grader for the current session
                var existingGrader = db.graders.FirstOrDefault(gr => gr.studentId == studentId && gr.session == session.session1);
                if (existingGrader != null)
                {
                    // Student is already assigned a grader
                    return Request.CreateResponse(HttpStatusCode.Found, "Already Assigned");
                }

                // Assign the grader to the student
                var newGrader = new grader
                {
                    studentId = studentId,
                    facultyId = facultyId,
                    session = session.session1
                };
                db.graders.Add(newGrader);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK,"Successfuly Assigned");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage gradersInformation(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var graders = db.Faculties.Where(fal => fal.facultyId == id).Join
                    (
                        db.graders.Where(gr => gr.facultyId == id && gr.session == session.session1),
                        f => f.facultyId,
                        g => g.facultyId,
                        (f, g) => new
                        {
                            g.Student.name,
                            g.Student.arid_no,
                            g.studentId,
                            g.Student.profile_image,
                            g.Student.gender,
                            f.facultyId,
                        }
                    );
                if (graders.ToList().Count < 1)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, graders);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Removegrader(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var graders = db.graders.Where(gr => gr.studentId == id && gr.session == session.session1).FirstOrDefault();
                db.graders.Remove(graders);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage ApplicationSuggestions()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var totalCommitteeMembers = db.Committees.Where(c => c.status == "1").ToList().Count();

                var applications = db.Applications.Where(app => app.session == session.session1)
                                           .GroupJoin(db.Suggestions,
                                               application => application.applicationID,
                                               suggestion => suggestion.applicationId,
                                               (application, suggestion) => new
                                               {
                                                   application,
                                                   suggestion
                                               })
                    .Where(ap => ap.suggestion.ToList().Count == totalCommitteeMembers);

                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (appplication, student) => new
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
                        appplication.application.applicationDate,
                        appplication.application.reason,
                        appplication.application.requiredAmount,
                        appplication.application.EvidenceDocuments,
                        appplication.application.applicationID,
                        appplication.application.session,
                        appplication.application.father_status,
                        appplication.application.jobtitle,
                        appplication.application.salary,
                        appplication.application.guardian_contact,
                        appplication.application.house,
                        appplication.application.guardian_name,
                        appplication.suggestion,
                    });

                var pendingapplication = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus
                    }
                    );

                return Request.CreateResponse(HttpStatusCode.OK, pendingapplication.Where(p => p.applicationStatus.ToLower() == "pending"));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }




        [HttpPost]
        public HttpResponseMessage UpdatePassword(int id, String username, String password)
        {
            try
            {
                var userprofile = db.Users.Where(u => u.userName == username).FirstOrDefault();

                if (userprofile == null)
                {
                    User user = new User();
                    user.role = 4;
                    user.password = password;
                    user.userName = username;
                    user.profileId = id;
                    db.Users.Add(user);
                    db.SaveChanges();
                }
                else
                {
                    userprofile.password = password;
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        
        /*[HttpGet]
        public HttpResponseMessage ToperStudents(double cgpa)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Students.Where(s=>s.cgpa>=cgpa));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }*/

       
        [HttpGet]
        public HttpResponseMessage UnAssignedFaculty()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var query = from f in db.Faculties
                            join g in db.graders.Where(gr => gr.session == session.session1)
                            on f.facultyId equals g.facultyId into
                            joinedRecord
                            from g in joinedRecord.DefaultIfEmpty()
                            where g == null
                            select f;
                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage unAssignedGraders1()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                // Need-based accepted applications
                var needBaseAcceptedApplications = from fa in db.FinancialAids
                                                   where fa.applicationStatus.ToLower() == "accepted" && fa.aidtype.ToLower() == "needbase"
                                                   join ap in db.Applications.Where(app => app.session == session.session1)
                                                   on fa.applicationId equals ap.applicationID
                                                   select new { ap.studentId };

                // Need-based accepted students
                var needBaseAcceptedStudents = from app in needBaseAcceptedApplications
                                               join st in db.Students
                                               on app.studentId equals st.student_id
                                               select st;

                // Merit-based accepted applications
                var meritbaseAcceptedApplication = from fa in db.FinancialAids
                                                   where fa.applicationStatus.ToLower() == "accepted" && fa.aidtype.ToLower() == "meritbase" && fa.session == session.session1
                                                   join ap in db.MeritBases.Where(app => app.session == session.session1)
                                                   on fa.applicationId equals ap.studentId
                                                   select new { ap.studentId };

                // Merit-based accepted students
                var meritBaseAcceptedStudents = from fa in meritbaseAcceptedApplication
                                                join st in db.Students
                                                on fa.studentId equals st.student_id
                                                select st;

                // Combine need-based and merit-based accepted students
                var acceptedStudents = needBaseAcceptedStudents.Union(meritBaseAcceptedStudents);

                // Left join with graders to find unassigned graders and calculate average rating
                var result = from student in acceptedStudents
                             join grader in db.graders.Where(gr => gr.session != session.session1)
                             on student.student_id equals grader.studentId into gradings
                             from grader in gradings.DefaultIfEmpty()
                             select new
                             {
                                 student.name,
                                 student.arid_no,
                                 student.semester,
                                 student.cgpa,
                                 student.section,
                                 student.degree,
                                 student.father_name,
                                 student.gender,
                                 student.student_id,
                                 student.profile_image,
                                 student.prev_cgpa,
                                 AverageRating = grader == null ? (double?)null : db.graders.Where(r => r.studentId == grader.studentId).Average(r => (double?)r.feedback)
                             };

                var unassignedGradersWithRatings = result.ToList();

                return Request.CreateResponse(HttpStatusCode.OK, unassignedGradersWithRatings);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage Merit()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage pendingApplication()
        {
            try
            {

                var query = from s in db.Students
                            join a in db.Applications
                            on s.student_id equals
                            a.studentId into joinedRecords
                            from a in
                                joinedRecords.DefaultIfEmpty()
                            where a != null
                            select a;
                var pendingApplication = from q in query
                                         join f in db.FinancialAids
                                         on q.applicationID equals f.applicationId into
                                         joinedRecords
                                         from f in joinedRecords.DefaultIfEmpty()
                                         where f != null
                                         select q;
                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GiveRating()
        {
            try
            {

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddSession(String name, String startDate, String endDate, String lastDate)
        {
            try
            {
                var isExist = db.Sessions.Where(s => s.session1 == name).FirstOrDefault();
                if (isExist == null)
                {
                    Session s = new Session();
                    s.session1 = name;
                    s.start_date = startDate;
                    s.end_date = endDate;
                    s.submission_date = lastDate;
                    db.Sessions.Add(s);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Found, "Already Exist");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AcceptApplication(int amount, int applicationid)
        {
            try
            {
                var remainingamount = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();

                if (amount > 0 && remainingamount.remainingAmount >= amount)
                {
                    var application = db.FinancialAids.Where(f => f.applicationId == applicationid).FirstOrDefault();
                    if (application.applicationStatus.ToLower() == "pending")
                    {
                        application.amount = amount.ToString();
                        application.applicationStatus = "Accepted";
                        //  var paisa = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();
                        remainingamount.remainingAmount -= amount;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, remainingamount.remainingAmount + "\n" + application.applicationStatus);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotAcceptable, application.applicationStatus);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "InSufficient Funds");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage RejectApplication(int applicationid)
        {
            try
            {
                var application = db.FinancialAids.Where(f => f.applicationId == applicationid).FirstOrDefault();
                if (application.applicationStatus.ToLower() == "pending")
                {
                    application.applicationStatus = "Rejected";
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, application.applicationStatus);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already : " + application.applicationStatus);
                }

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddPolicies(String description, String val1, String val2, String policyFor, String policy, String strength)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                Criterion c = new Criterion();
                Policy p = new Policy();
                p.policyfor = policyFor;
                p.policy1 = policy;
                p.session = session.session1;
                db.Policies.Add(p);
                db.SaveChanges();
                var pol = db.Policies.OrderByDescending(o => o.id).FirstOrDefault();
                c.val1 = val1;
                c.val2 = val2;
                c.description = description;
                c.policy_id = pol.id;
                c.strength = int.Parse(strength);
                db.Criteria.Add(c);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage MeritBaseShortListing()
        {
            try
            {
                List<Student> toperStudent = new List<Student>();

                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var isAlreadyShortListed = db.MeritBases.Where(merit => merit.session == session.session1);
                if (isAlreadyShortListed == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already Short Listed");
                }
                else
                {
                    var amount = db.Amounts.Where(amt => amt.session == session.session1).FirstOrDefault();
                    if (amount != null)
                    {
                        var first = amount.first_position;
                        var second = amount.second_position;
                        var third = amount.third_position;
                        var degree = db.Students.Select(p => new
                        {
                            p.degree,

                        }).Distinct().ToList();

                        foreach (var d in degree)
                        {
                            var semester = db.Students.Where(s => s.degree == d.degree).Select(p => new
                            {
                                p.semester,
                            }).Distinct();
                            foreach (var s in semester)
                            {
                                var section = db.Students.Where(std => std.degree == d.degree && std.semester == s.semester).Select(p => new
                                {
                                    p.section
                                }).Distinct();

                                foreach (var sec in section)
                                {
                                    var count = db.Students.Where(stu => stu.degree == d.degree &&
                                    stu.semester == s.semester && stu.section ==
                                    sec.section).Distinct().Count();
                                    MeritBase m;
                                    FinancialAid fa;

                                    if (count >= 40)
                                    {
                                        var topers1 = db.Students.Where(st => st.degree == d.degree && st.cgpa >=
                                        3.7 && st.semester == s.semester && st.section == sec.section)
                                            .DistinctBy(st => st.student_id)
                                            .OrderByDescending(od => od.cgpa).Take(3)
                                               .Select(p => new
                                               {
                                                   p.cgpa,
                                                   p.student_id
                                               }).ToList();
                                        foreach (var t in topers1)
                                        {
                                            toperStudent.Add(db.Students.Where(stud => stud.student_id == t.student_id).FirstOrDefault());
                                            for (int index = 0; index < toperStudent.Distinct().ToList().Count; index++)
                                            {
                                                m = new MeritBase();
                                                fa = new FinancialAid();
                                                m.studentId = toperStudent[index].student_id;
                                                m.session = session.session1;
                                                m.position = index + 1;
                                                db.MeritBases.Add(m);
                                                fa.applicationStatus = "Pending";
                                                fa.aidtype = "MeritBase";
                                                fa.applicationId = toperStudent[index].student_id;
                                                if (index == 0)
                                                {
                                                    fa.amount = first.ToString();
                                                }
                                                else if (index == 1)
                                                {
                                                    fa.amount = second.ToString();

                                                }
                                                else
                                                {
                                                    fa.amount = third.ToString();
                                                }
                                                db.FinancialAids.Add(fa);
                                            }
                                        }

                                    }
                                    else if (count >= 30 && count < 40)
                                    {
                                        var topers1 = db.Students.Where(st => st.degree == d.degree && st.cgpa >=
                                        3.7 && st.semester == s.semester && st.section == sec.section)
                                            .DistinctBy(st => st.student_id)
                                            .OrderByDescending(od => od.cgpa).Take(2)
                                               .Select(p => new
                                               {
                                                   p.cgpa,
                                                   p.student_id
                                               }).ToList();
                                        foreach (var t in topers1)
                                        {
                                            toperStudent.Add(db.Students.Where(stud => stud.student_id == t.student_id).FirstOrDefault());
                                            for (int index = 0; index < toperStudent.Distinct().ToList().Count; index++)
                                            {
                                                m = new MeritBase();
                                                fa = new FinancialAid();
                                                m.studentId = toperStudent[index].student_id;
                                                m.session = session.session1;
                                                m.position = index + 1;
                                                db.MeritBases.Add(m);
                                                fa.applicationStatus = "Pending";
                                                fa.aidtype = "MeritBase";
                                                fa.applicationId = toperStudent[index].student_id;
                                                if (index == 0)
                                                {
                                                    fa.amount = first.ToString();
                                                }
                                                else if (index == 1)
                                                {
                                                    fa.amount = second.ToString();

                                                }
                                                else
                                                {
                                                    fa.amount = third.ToString();
                                                }
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                    else if (count < 30)
                                    {
                                        var topers1 = db.Students.Where(st => st.degree == d.degree && st.cgpa >=
                                        3.7 && st.semester == s.semester && st.section == sec.section)
                                            .DistinctBy(st => st.student_id)
                                            .OrderByDescending(od => od.cgpa).Take(1)
                                               .Select(p => new
                                               {
                                                   p.cgpa,
                                                   p.student_id
                                               }).ToList();
                                        foreach (var t in topers1)
                                        {
                                            toperStudent.Add(db.Students.Where(stud => stud.student_id == t.student_id).FirstOrDefault());
                                            for (int index = 0; index < toperStudent.Distinct().ToList().Count; index++)
                                            {
                                                m = new MeritBase();
                                                fa = new FinancialAid();
                                                m.studentId = toperStudent[index].student_id;
                                                m.session = session.session1;
                                                m.position = index + 1;
                                                db.MeritBases.Add(m);
                                                fa.applicationStatus = "Pending";
                                                fa.aidtype = "MeritBase";
                                                fa.applicationId = toperStudent[index].student_id;
                                                if (index == 0)
                                                {
                                                    fa.amount = first.ToString();
                                                }
                                                else if (index == 1)
                                                {
                                                    fa.amount = second.ToString();

                                                }
                                                else
                                                {
                                                    fa.amount = third.ToString();
                                                }
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                }
                            }
                            db.SaveChanges();
                        }
                        /*                    var student = db.Students.Join(
                                                db.MeritBases,
                                                s => s.student_id,
                                                m => m.studentId,
                                                (s, m) => new
                                                {
                                                    s,
                                                    m.position
                                                }
                                                ).Distinct();*/

                        return Request.CreateResponse(HttpStatusCode.OK, toperStudent);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest);
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage MeritBase()
        {
            try
            {
                var toperStudent = new List<Student>();

                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var isAlreadyShortListed = db.MeritBases.Any(merit => merit.session == session.session1);

                if (isAlreadyShortListed)
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already Short Listed");
                }
                else
                {
                    var amount = db.Amounts.FirstOrDefault(amt => amt.session == session.session1);
                    if (amount != null)
                    {
                        var first = amount.first_position;
                        var second = amount.second_position;
                        var third = amount.third_position;

                        var degrees = db.Students.Select(p => p.degree).Distinct().ToList();

                        foreach (var degree in degrees)
                        {
                            var semesters = db.Students.Where(s => s.degree == degree).Select(p => p.semester).Distinct().ToList();

                            foreach (var semester in semesters)
                            {
                                var sections = db.Students.Where(std => std.degree == degree && std.semester == semester).Select(p => p.section).Distinct().ToList();

                                foreach (var section in sections)
                                {
                                    var studentQuery = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section)
                                                                 .OrderByDescending(od => od.cgpa).ToList();

                                    if (studentQuery.Count >= 40)
                                    {
                                        var toperCount = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section && stu.cgpa >= 3.7).Take(3)
                                        .OrderByDescending(od => od.cgpa).ToList();
                                        for (int i = 0; i < toperCount.Count; i++)
                                        {
                                            if (!toperStudent.Any(ts => ts.student_id == toperCount[i].student_id))
                                            {
                                                toperStudent.Add(toperCount[i]);
                                                var m = new MeritBase
                                                {
                                                    studentId = toperCount[i].student_id,
                                                    session = session.session1,
                                                    position = i + 1
                                                };

                                                var fa = new FinancialAid
                                                {
                                                    applicationStatus = "Pending",
                                                    aidtype = "MeritBase",
                                                    applicationId = toperCount[i].student_id,
                                                    amount = GetAmount(i + 1, first, second, third)
                                                };

                                                db.MeritBases.Add(m);
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                    else if (studentQuery.Count >= 30 && studentQuery.Count < 40)
                                    {
                                        var toperCount = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section && stu.cgpa >= 3.7).Take(2)
                                           .OrderByDescending(od => od.cgpa).ToList();
                                        for (int i = 0; i < toperCount.Count; i++)
                                        {
                                            if (!toperStudent.Any(ts => ts.student_id == toperCount[i].student_id))
                                            {
                                                toperStudent.Add(toperCount[i]);
                                                var m = new MeritBase
                                                {
                                                    studentId = toperCount[i].student_id,
                                                    session = session.session1,
                                                    position = i + 1
                                                };

                                                var fa = new FinancialAid
                                                {
                                                    applicationStatus = "Pending",
                                                    aidtype = "MeritBase",
                                                    applicationId = toperCount[i].student_id,
                                                    amount = GetAmount(i + 1, first, second, third)
                                                };

                                                db.MeritBases.Add(m);
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var toperCount = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section && stu.cgpa >= 3.7).Take(1)
                                        .OrderByDescending(od => od.cgpa).ToList();
                                        for (int i = 0; i < toperCount.Count; i++)
                                        {
                                            if (!toperStudent.Any(ts => ts.student_id == toperCount[i].student_id))
                                            {
                                                toperStudent.Add(toperCount[i]);
                                                var m = new MeritBase
                                                {
                                                    studentId = toperCount[i].student_id,
                                                    session = session.session1,
                                                    position = i + 1
                                                };

                                                var fa = new FinancialAid
                                                {
                                                    applicationStatus = "Pending",
                                                    aidtype = "MeritBase",
                                                    applicationId = toperCount[i].student_id,
                                                    amount = GetAmount(i + 1, first, second, third)
                                                };

                                                db.MeritBases.Add(m);
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, toperStudent.Select(s => new
                        {
                            s.arid_no,
                            s.name,
                            s.cgpa,
                            s.degree,
                            s.semester,
                            s.student_id,
                            s.section,
                            s.profile_image,
                            s.gender,
                        }));
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest);
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

    }
} 