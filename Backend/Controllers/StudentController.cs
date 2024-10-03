using Backend.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace FinancialAidAllocation.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class StudentController : ApiController
    {
        FAAToolEntities db = new FAAToolEntities();

        [HttpGet]

        public HttpResponseMessage checkCgpaPolicy()
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
                return Request.CreateResponse(HttpStatusCode.OK, pol.Where(po => /*po.p.session == session.session1  &&*/ po.p.policyfor == "NeedBase" && po.p.policy1 == "CGPA").OrderByDescending(od => od.p.id).Select(s => new
                {
                    s.c.val1,
                }).FirstOrDefault());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage getStudentInfo(int id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Students.Where(s => s.student_id == id).FirstOrDefault());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage getSession()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                if (session != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, session);

                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        //[HttpGet]
        //public HttpResponseMessage getStudentApplicationStatus(int id)
        //{
        //    try
        //    {
        //        // Fetch the application for the student based on the studentId
        //        var application = db.Applications.Where(c => c.studentId == id).FirstOrDefault();

        //        // Fetch the latest session
        //        var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

        //        // Fetch MeritBase entry for the student in the latest session
        //        var meritBase = db.MeritBases.Where(mr => mr.studentId == id && mr.session == session.session1).FirstOrDefault();

        //        // If an application exists, fetch the corresponding FinancialAid record
        //        if (application != null)
        //        {
        //            var result = db.Applications.Where(ap => ap.studentId == id).Join(
        //                db.FinancialAids,
        //                a => a.applicationID, // Joining on the applicationID field
        //                f => f.applicationId,
        //                (a, f) => new
        //                {
        //                    f.applicationStatus,
        //                    f.amount,
        //                    f.aidtype
        //                }
        //            ).FirstOrDefault();

        //            return Request.CreateResponse(HttpStatusCode.OK, result);
        //        }
        //        // If a merit-based entry exists, fetch the corresponding FinancialAid record based on studentId
        //        else if (meritBase != null)
        //        {
        //            var result = db.MeritBases.Where(ap => ap.studentId == id && ap.session == session.session1).Join(
        //                db.FinancialAids,
        //                a => a.studentId, // Joining on studentId because MeritBase doesn't have applicationID
        //                f => f.applicationId,
        //                (a, f) => new
        //                {
        //                    f.applicationStatus,
        //                    f.amount,
        //                    f.aidtype
        //                }
        //            ).FirstOrDefault();

        //            return Request.CreateResponse(HttpStatusCode.OK, result);
        //        }
        //        // If no application or merit-based record is found
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "Not Submitted");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle any exceptions
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}


        [HttpGet]
        public HttpResponseMessage getStudentApplicationStatus(int id)
        {
            try
            {
                // Fetch the application for the student based on the studentId
                var application = db.Applications.Where(c => c.studentId == id).FirstOrDefault();

                // Fetch the latest session
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                // Fetch MeritBase entry for the student in the latest session
                var meritBase = db.MeritBases.Where(mr => mr.studentId == id && mr.session == session.session1).FirstOrDefault();

                // If an application exists, fetch the corresponding FinancialAid record
                if (application != null)
                {
                    var result = db.Applications.Where(ap => ap.studentId == id).Join(
                        db.FinancialAids,
                        a => a.applicationID, // Joining on the applicationID field
                        f => f.applicationId,
                        (a, f) => new
                        {
                            f.applicationStatus,
                            f.amount,
                            f.aidtype
                        }
                    ).FirstOrDefault();

                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                // If a merit-based entry exists, fetch the corresponding FinancialAid record based on studentId
                else if (meritBase != null)
                {
                    var result = db.MeritBases.Where(ap => ap.studentId == id && ap.session == session.session1).Join(
                        db.FinancialAids,
                        a => a.studentId, // Joining on studentId because MeritBase doesn't have applicationID
                        f => f.applicationId,
                        (a, f) => new
                        {
                            f.applicationStatus,
                            f.amount,
                            f.aidtype
                        }
                    ).FirstOrDefault();

                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                // If no application or merit-based record is found
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Not Submitted");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage BuildProfile()
        {
            try
            {
                var request = HttpContext.Current.Request;
                int id = int.Parse(request["id"]);
                String name = request["name"];
                String aridno = request["aridno"];
                int Semester = int.Parse(request["semester"]);
                String gender = request["gender"];
                String fname = request["fname"];
                String fstatus = request["fstatus"];
                String jobtitle = request["jobtitle"];
                String contact = request["contact"];
                String salary = request["salary"];
                String cgpa = request["cgpa"];
                String degree = request["degree"];
                var provider = new MultipartMemoryStreamProvider();
                var salaryslip = request.Files["salaryslip"];
                String salaryslippath = DateTime.Now.Millisecond.ToString() + "." + salaryslip.FileName.Split('.')[1];
                salaryslip.SaveAs(HttpContext.Current.Server.MapPath
                    ("~/Content/SalarySlip/" + salaryslippath));
                var certificate = request.Files["certificate"];
                String certificatepath = DateTime.Now.Millisecond.ToString() + "." + certificate.FileName.Split('.')[1];
                certificate.SaveAs(HttpContext.Current.Server.MapPath
                    ("~/Content/DeathCertificates/" + certificatepath));
                Student s = new Student();
             //   s.fatherStatus = fstatus;
                s.name = name;
             //   s.fatherName = fname;
                s.semester = Semester;
                s.degree=degree;
                s.arid_no = aridno;
                s.gender=gender;
             //   s.salarySlip = salaryslippath;
             //   s.jobTitle = jobtitle;
             //   s.deathCertificate = certificatepath;
             //   s.guardianContact = contact;
             //   s.salary = salary;
                s.cgpa = double.Parse(cgpa);
                db.Students.Add(s);
                db.SaveChanges();
                var std = db.Students.Where(st=>st.arid_no==aridno).FirstOrDefault();
                var profile = db.Users.Where(u=>u.id==id).FirstOrDefault();
                profile.profileId = std.student_id;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage updateProfilePicture() 
        {
            try
            {
                var request = HttpContext.Current.Request;
                int id = int.Parse(request["id"]);
                var image = request.Files["image"];
                String imagename = "profilepicure" + DateTime.Now.Millisecond.ToString() + "." + image.FileName.Split('.')[1];
                image.SaveAs(HttpContext.Current.Server.MapPath
                    ("~/Content/ProfileImages/" + imagename));

                var student = db.Students.Where(s => s.student_id == id).FirstOrDefault();
                student.profile_image = imagename;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage sendApplication()
        {
            try
            {
                var request = HttpContext.Current.Request;
                String Status = request["status"];
                String occupation = request["occupation"];
                String RAmount = request["contactNo"];
                String salary = request["salary"];
                String gName = request["gName"];
                String gContact = request["gContact"];
                String gRelation = request["gRelation"];
                String house = request["house"];
                String reason = request["reason"];
                String amount = request["amount"];
                int length = int.Parse(request["length"]);
                bool isPicked = bool.Parse(request["isPicked"]);
                int studentId = int.Parse(request["studentId"]);
                String ss, a;

                var session = db.Sessions.OrderByDescending(se => se.id).FirstOrDefault();

                var ap1 = db.Applications.Where(app => app.studentId == studentId && app.session == session.session1).FirstOrDefault();

                if (ap1 != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized);
                }
                else
                {
                    EvidenceDocument ed = new EvidenceDocument();

                    if (Status == "Alive" && isPicked)
                    {
                        var docs = request.Files["docs"];
                        ss = "salaryslip" + DateTime.Now.Millisecond.ToString() + "." + docs.FileName.Split('.')[1];
                        docs.SaveAs(HttpContext.Current.Server.MapPath
                            ("~/Content/SalarySlip/" + ss));
                        ed.image = ss;
                        ed.document_type = "salaryslip";
                    }
                    else if(Status!="Alive")
                    {
                        var deathCertificate = request.Files["docs"];
                        ss = "Certificate" + DateTime.Now.Millisecond.ToString() + "." + deathCertificate.FileName.Split('.')[1];
                        deathCertificate.SaveAs(HttpContext.Current.Server.MapPath
                            ("~/Content/DeathCertificates/" + ss));
                        ed.image = ss;
                        ed.document_type = "deathcertificate";
                    }
                    List<string> paths =new List<string>();
                    for (int i=0;i<length;i++) 
                    {
                        var agreement = request.Files["agreement"+i];
                        a = "agreement" + DateTime.Now.Millisecond.ToString() + "." + agreement.FileName.Split('.')[1];
                        agreement.SaveAs(HttpContext.Current.Server.MapPath
                            ("~/Content/HouseAgreement/" + a));
                        paths.Add(a);
                    }



                    String date = DateTime.Now.Day.ToString()+"/" + DateTime.Now.Month.ToString() + "/" + DateTime.Now.Year.ToString();


                    Application application = new Application();

                    application.father_status = Status;
                    application.studentId = studentId;
                    application.reason = reason;
                    application.requiredAmount = amount;
                    application.applicationDate = date;
                    application.session = session.session1;

                    if (Status == "Alive")
                    {
                        application.jobtitle = occupation;
                        application.house = house;
                        application.salary = salary;
                    }
                    else
                    {
                        application.guardian_contact = gContact;
                        application.guardian_name = gName;
                    }
                    db.Applications.Add(application);
                    db.SaveChanges();
                    var app1 = db.Applications.Where(app => app.studentId == studentId && app.session == session.session1).FirstOrDefault();

                    ed.applicationId = app1.applicationID;
                    db.EvidenceDocuments.Add(ed);
                    db.SaveChanges();

                    for (int j=0;j<paths.Count;j++) 
                    {
                        EvidenceDocument ed1 = new EvidenceDocument();
                        ed1.applicationId = app1.applicationID;
                        ed1.document_type = "houseAgreement";
                        ed1.image = paths[j];
                        db.EvidenceDocuments.Add(ed1);

                    }
                    db.SaveChanges();
/*                    Suggestion s = new Suggestion();
                    s.applicationId = app1.applicationID;
                    db.Suggestions.Add(s);
                    db.SaveChanges();*/
                    FinancialAid fa = new FinancialAid();
                    fa.applicationId = app1.applicationID;
                    fa.applicationStatus = "Pending";
                    fa.aidtype = "NeedBase";
                    db.FinancialAids.Add(fa);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Submitted successfully");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [HttpGet]
        public HttpResponseMessage NeedBasePolicies()
        {
            try
            {
                // db.Policies.Where(s => s.policyFor.ToLower().Equals("NeedBase"))
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage MeritBasePolicies()
        {
            try
            {
                //, db.Policies.Where(s => s.policyFor.ToLower().Equals("meritbase"))
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage ApplicationHistory(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var totalCommitteeMembers = db.Committees.Where(c => c.status == "1").ToList().Count();

                var applications = db.Applications.Where(app => app.session != session.session1 && app.studentId == id)
                    .GroupJoin(db.Suggestions,
                        application => application.applicationID,
                        suggestion => suggestion.applicationId,
                        (application, suggestions) => new
                        {
                            application,
                            suggestions
                        })
                    .Where(ap => ap.suggestions.ToList().Count == totalCommitteeMembers);

                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
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
                        application.application.applicationDate,
                        application.application.reason,
                        application.application.requiredAmount,
                        application.application.EvidenceDocuments,
                        application.application.applicationID,
                        application.application.session,
                        application.application.father_status,
                        application.application.jobtitle,
                        application.application.salary,
                        application.application.guardian_contact,
                        application.application.house,
                        application.application.guardian_name,
                        application.suggestions
                    });

                var pendingApplications = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus
                    });

                var finalResult = pendingApplications.Select(pa => new
                {
                    pa.applicationStatus,
                    pa.re.arid_no,
                    pa.re.name,
                    pa.re.student_id,
                    pa.re.father_name,
                    pa.re.gender,
                    pa.re.degree,
                    pa.re.cgpa,
                    pa.re.semester,
                    pa.re.section,
                    pa.re.profile_image,
                    pa.re.applicationDate,
                    pa.re.reason,
                    pa.re.requiredAmount,
                    pa.re.EvidenceDocuments,
                    pa.re.applicationID,
                    pa.re.session,
                    pa.re.father_status,
                    pa.re.jobtitle,
                    pa.re.salary,
                    pa.re.guardian_contact,
                    pa.re.house,
                    pa.re.guardian_name,
                    Suggestions = pa.re.suggestions.Select(s => new
                    {
                        s.comment,
                        amount = s.amount,
                        CommitteeMemberName = db.Faculties
                            .Where(fac => fac.facultyId == db.Committees.FirstOrDefault(c => c.committeeId == s.committeeId).facultyId)
                            .Select(fac => fac.name).FirstOrDefault()
                    }).ToList()
                });

                return Request.CreateResponse(HttpStatusCode.OK, finalResult);
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
                    result.amount = "5000";
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


/*
 
        [HttpPost]
        public HttpResponseMessage sendApplication()
        {
            try
            {
                var request = HttpContext.Current.Request;
                String Status = request["status"];
                String occupation = request["occupation"];
                String RAmount = request["contactNo"];
                String salary = request["salary"];
                String gName = request["gName"];
                String gContact = request["gContact"];
                String gRelation = request["gRelation"];
                String house = request["house"];
                String reason = request["reason"];
                String amount = request["amount"];
                bool isPicked = bool.Parse(request["isPicked"]);
                int studentId = int.Parse(request["studentId"]);
                String ss, a;

                var session = db.Sessions.OrderByDescending(se => se.id).FirstOrDefault();

                var ap1 = db.Applications.Where(app => app.studentId == studentId && app.session == session.session1).FirstOrDefault();

                if (ap1 != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized);
                }
                else
                {

                    EvidenceDocument ed = new EvidenceDocument();

                    if (Status == "Alive" && isPicked)
                    {
                        var docs = request.Files["docs"];
                        ss = "salaryslip" + DateTime.Now.Millisecond.ToString() + "." + docs.FileName.Split('.')[1];
                        docs.SaveAs(HttpContext.Current.Server.MapPath
                            ("~/Content/SalarySlip/" + ss));
                        ed.image = ss;
                        ed.document_type = "salaryslip";
                    }
                    else if(Status!="Alive")
                    {
                        var deathCertificate = request.Files["docs"];
                        ss = "Certificate" + DateTime.Now.Millisecond.ToString() + "." + deathCertificate.FileName.Split('.')[1];
                        deathCertificate.SaveAs(HttpContext.Current.Server.MapPath
                            ("~/Content/DeathCertificates/" + ss));
                        ed.image = ss;
                        ed.document_type = "deathcertificate";
                    }

                    var agreement = request.Files["agreement"];

                    a = "agreement" + DateTime.Now.Millisecond.ToString() + "." + agreement.FileName.Split('.')[1]; 
                    agreement.SaveAs(HttpContext.Current.Server.MapPath
                        ("~/Content/HouseAgreement/" + a));


                    String date = DateTime.Now.Day.ToString()+"/" + DateTime.Now.Month.ToString() + "/" + DateTime.Now.Year.ToString();


                    Application application = new Application();

                    application.father_status = Status;
                    application.studentId = studentId;
                    application.reason = reason;
                    application.requiredAmount = amount;
                    application.applicationDate = date;
                    application.session = session.session1;

                    if (Status == "Alive")
                    {
                        application.jobtitle = occupation;
                        application.house = house;
                        application.salary = salary;
                    }
                    else
                    {
                        application.guardian_contact = gContact;
                        application.guardian_name = gName;
                    }
                    db.Applications.Add(application);
                    db.SaveChanges();
                    var app1 = db.Applications.Where(app => app.studentId == studentId && app.session == session.session1).FirstOrDefault();

                    ed.applicationId = app1.applicationID;
                    db.EvidenceDocuments.Add(ed);
                    db.SaveChanges();

                    EvidenceDocument ed1 = new EvidenceDocument();
                    ed1.applicationId = app1.applicationID;
                    ed1.document_type = "houseAgreement";
                    ed1.image = a;

                    db.EvidenceDocuments.Add(ed1);

                    db.SaveChanges();
                    Suggestion s = new Suggestion();
                    s.applicationId = app1.applicationID;
                    db.Suggestions.Add(s);
                    db.SaveChanges();
                    FinancialAid fa = new FinancialAid();
                    fa.applicationId = app1.applicationID;
                    fa.applicationStatus = "Pending";
                    fa.aidtype = "NeedBase";
                    db.FinancialAids.Add(fa);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Submitted successfully");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

 */