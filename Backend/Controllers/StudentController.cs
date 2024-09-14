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

        [HttpGet]
        public HttpResponseMessage getStudentApplicationStatus(int id)
        {
            try
            {

                var count = db.Applications.Where(c => c.studentId == 1d).FirstOrDefault();
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var count1 = db.MeritBases.Where(mr =>mr.studentId == id && mr.session ==session.session1).FirstOrDefault();
                if (count != null)
                {
                    var result = db.Applications.Where(ap => ap.studentId == id).Join(
                    db.FinancialAids,
                    a => a.applicationID,
                    f => f.applicationId,
                    (a, f) => new
                    {
                        f.applicationStatus,
                        f.amount,
                        f.aidtype,
                    }
                    ).FirstOrDefault();
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                else if(count1 != null)
                {
                    var result = db.MeritBases.Where(ap => ap.studentId == id && ap.session == session.session1).Join(
                        db.FinancialAids,
                        a => a.studentId,
                        f => f.applicationId,
                        (a, f) => new
                        {
                            f.applicationStatus,
                            f.amount,
                            f.aidtype,
                        }
                        ).FirstOrDefault();
                        return Request.CreateResponse(HttpStatusCode.OK,result);
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
        public HttpResponseMessage UploadFile1()
        {
            string pathStr = "";
            var request = HttpContext.Current.Request;
            var enrollmentFile = request.Files["enrollment"];
            var path = HttpContext.Current.Server.MapPath("~/Content/Student_excel_sheet/" + enrollmentFile.FileName.Trim());
            pathStr = path;
            enrollmentFile.SaveAs(path);

            OleDbConnection oleDbConnection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0 Xml;HDR=NO'");

            try
            {
                oleDbConnection.Open();
                OleDbCommand command = new OleDbCommand("select * from [Sheet1$]", oleDbConnection);
                OleDbDataReader reader = command.ExecuteReader();
                List<Student> studentList = new List<Student>();
                Student student;

                while (reader.Read())
                {
                    student = new Student
                    {
                        arid_no = reader[0].ToString(),
                        name = reader[1].ToString(),
                        semester = Convert.ToInt32(reader[2].ToString().Trim()),
                        cgpa = Convert.ToDouble(reader[3].ToString()),
                        section = reader[4].ToString(),
                        degree = reader[5].ToString(),
                        father_name = reader[6].ToString(),
                        gender = reader[7].ToString(),
                        prev_cgpa = Convert.ToDouble(reader[8].ToString().Trim())
                    };
                    studentList.Add(student);
                }
                oleDbConnection.Close();

                var topStudents = new List<Student>();
                var budget = db.Budgets.OrderByDescending(b => b.budgetId).FirstOrDefault();
                var session = db.Sessions.OrderByDescending(s => s.id).FirstOrDefault();

                if (db.MeritBases.Any(m => m.session == session.session1))
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already Short Listed");
                }

                var cgpaPolicy = db.Policies
                                    .Where(p => p.policyfor == "MeritBase" && p.policy1 == "CGPA")
                                    .Join(db.Criteria, p => p.id, c => c.policy_id, (p, c) => c.val1)
                                    .FirstOrDefault();

                var strengthPolicies = db.Policies
                                        .Where(p => p.policyfor == "MeritBase" && p.policy1 == "STRENGTH")
                                        .Join(db.Criteria, p => p.id, c => c.policy_id, (p, c) => new { c.val1, c.val2, c.strength })
                                        .ToList();

                var amount = db.Amounts.OrderByDescending(a => a.Id).FirstOrDefault();
                if (amount == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                int totalAmount = 0;
                var degrees = studentList.Select(s => s.degree).Distinct().ToList();

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var degree in degrees)
                        {
                            var semesters = studentList.Where(s => s.degree == degree).Select(s => s.semester).Distinct().ToList();
                            foreach (var semester in semesters)
                            {
                                var sections = studentList.Where(s => s.degree == degree && s.semester == semester).Select(s => s.section).Distinct().ToList();
                                foreach (var section in sections)
                                {
                                    var studentsInSection = studentList
                                                            .Where(s => s.degree == degree && s.semester == semester && s.section == section)
                                                            .OrderByDescending(s => s.cgpa)
                                                            .ToList();
                                    foreach (var policy in strengthPolicies)
                                    {
                                        int minStrength = int.Parse(policy.val1);
                                        int maxStrength = int.Parse(policy.val2);
                                        double minCgpa = double.Parse(cgpaPolicy);
                                        int topN = int.Parse(policy.strength.ToString());

                                        if (studentsInSection.Count >= minStrength)
                                        {
                                            var topCandidates = studentsInSection.Where(s => s.cgpa >= minCgpa).ToList();
                                            int currentPosition = 1;
                                            double? previousCgpa = null;
                                            int studentCount = 0;

                                            for (int i = 0; i < topCandidates.Count; i++)
                                            {
                                                if (!topStudents.Any(ts => ts.arid_no == topCandidates[i].arid_no))
                                                {
                                                    var sameCgpaStudents = studentsInSection.Where(s => s.cgpa == topCandidates[i].cgpa).ToList();
                                                    int sameCgpaCount = sameCgpaStudents.Count;

                                                    if (sameCgpaCount > 1)
                                                    {
                                                        var previousSession = db.Sessions
                                                        .OrderByDescending(s => s.id)
                                                        .Skip(1)
                                                        .FirstOrDefault();
                                                        currentPosition = i + 1;
                                                        var studentsWithFinancialAid = sameCgpaStudents
                                                            .Where(studnt => db.FinancialAids
                                                                .Any(fa => fa.applicationId == studnt.student_id && fa.session == previousSession.session1))
                                                            .ToList();

                                                        double sharedAmount;
                                                        if (studentsWithFinancialAid.Any())
                                                        {
                                                            sharedAmount = CalculateSharedAmount(studentsWithFinancialAid.Count, currentPosition, amount);
                                                            foreach (var studentWithSameCgpa in studentsWithFinancialAid)
                                                            {
                                                                if (!topStudents.Any(ts => ts.arid_no == studentWithSameCgpa.arid_no))
                                                                {
                                                                    AddStudentToDatabaseAndFinancialAid(studentWithSameCgpa, session.session1, currentPosition, sharedAmount);
                                                                    topStudents.Add(studentWithSameCgpa);
                                                                    studentCount++;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            sharedAmount = CalculateSharedAmount(sameCgpaCount, currentPosition, amount);
                                                            foreach (var studentWithSameCgpa in sameCgpaStudents)
                                                            {
                                                                if (!topStudents.Any(ts => ts.arid_no == studentWithSameCgpa.arid_no))
                                                                {
                                                                    AddStudentToDatabaseAndFinancialAid(studentWithSameCgpa, session.session1, currentPosition, sharedAmount);
                                                                    topStudents.Add(studentWithSameCgpa);
                                                                    studentCount++;
                                                                }
                                                            }
                                                        }
                                                        i += sameCgpaCount - 1; // Skip processed students
                                                    }
                                                    else
                                                    {
                                                        currentPosition = i + 1;
                                                        double singleAmount = GetAmount(currentPosition, amount.first_position.ToString(), amount.second_position.ToString(), amount.third_position.ToString());
                                                        AddStudentToDatabaseAndFinancialAid(topCandidates[i], session.session1, currentPosition, singleAmount);
                                                        topStudents.Add(topCandidates[i]);
                                                    }

                                                    previousCgpa = topCandidates[i].cgpa;
                                                    studentCount++;
                                                    if (currentPosition > 3)
                                                    {
                                                        for (int j = i; j < topCandidates.Count; j++)
                                                        {
                                                            AddStudentToDatabaseAndReserve(topCandidates[i], session.session1, currentPosition);
                                                            if (j == topCandidates.Count) break;
                                                        }
                                                    };
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        budget.remainingAmount -= totalAmount;
                        db.SaveChanges();

                        var merdel = db.MeritBases.Where(mr1 => mr1.position > 3);
                        db.MeritBases.RemoveRange(merdel);
                        var fandel = db.FinancialAids.Where(fan1 => fan1.amount == "0".Trim());
                        db.FinancialAids.RemoveRange(fandel);
                        db.SaveChanges();

                        transaction.Commit();

                        var response = topStudents.Select(s => new
                        {
                            s.arid_no,
                            s.name,
                            s.cgpa,
                            s.degree,
                            s.semester,
                            s.section,
                            s.gender,
                        }).ToList();

                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
                    }
                }
            }
            catch (Exception e)
            {
                oleDbConnection.Close();
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e);
            }
        }
        private void AddStudentToDatabaseAndReserve(Student student, string session, int position)
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
            Reserve r = new Reserve();
            r.student_id = studentinfo.student_id;
            r.session = session;
            db.Reserves.Add(r);
            db.SaveChanges();
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
        private double GetAmount(int position, string firstPositionAmount, string secondPositionAmount, string thirdPositionAmount)
        {
            if (position == 3) return double.Parse(firstPositionAmount);
            if (position == 2) return double.Parse(secondPositionAmount);
            if (position == 1) return double.Parse(thirdPositionAmount);
            return 0;
        }
        private double CalculateSharedAmount(int count, int position, Amount amount)
        {
            double totalAmount = 0;
            if (position == 1)
            {
                if (count >= 3)
                {
                    totalAmount = double.Parse(amount.first_position.ToString()) + double.Parse(amount.second_position.ToString()) + double.Parse(amount.third_position.ToString());
                }
                else if (count == 2)
                {
                    totalAmount = double.Parse(amount.second_position.ToString()) + double.Parse(amount.third_position.ToString());
                }
                else if (count == 1)
                {
                    totalAmount = double.Parse(amount.third_position.ToString());
                }
            }
            else if (position == 2)
            {
                if (count >= 2)
                {
                    totalAmount = double.Parse(amount.second_position.ToString()) + double.Parse(amount.first_position.ToString());
                }
                else if (count == 1)
                {
                    totalAmount = double.Parse(amount.second_position.ToString());
                }
            }
            else if (position == 3)
            {
                totalAmount = double.Parse(amount.first_position.ToString());
            }

            return totalAmount / count;
        }

        [HttpPost]
        public HttpResponseMessage decideMeritBaseApplication(int id, String status)
        {
            try
            {
                var budget = db.Budgets.OrderByDescending(b => b.budgetId).FirstOrDefault();

                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var record = db.MeritBases.Where(mr => mr.studentId == id && mr.session == session.session1).FirstOrDefault();
                var result = db.FinancialAids.Where(fn => fn.applicationId == record.studentId).FirstOrDefault();
                if (record != null && status == "Accepted")
                {
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

                    var alternateToper = reservetoper.OrderByDescending(rt => rt.st.cgpa).Take(1).FirstOrDefault();

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