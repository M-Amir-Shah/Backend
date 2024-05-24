using Backend.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
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

        public HttpResponseMessage getStudentInfo(int id) 
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK,db.Students.Where(s=>s.student_id==id).FirstOrDefault());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
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
        public HttpResponseMessage getStudentApplicationStatus(int id)
        {
            try
            {
                var count = db.Applications.Where(c => c.studentId == 1d).FirstOrDefault();
                if (count != null)
                {
                    var result = db.Applications.Where(ap=>ap.studentId==id).Join(
                    db.FinancialAids,
                    a => a.applicationID,
                    f => f.applicationId,
                    (a, f) => new
                    {
                        f.applicationStatus,
                    }
                    ).FirstOrDefault();
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