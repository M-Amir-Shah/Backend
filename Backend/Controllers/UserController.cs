using Backend.Models;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Backend.Controllers
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
                        return Request.CreateResponse(HttpStatusCode.OK, user);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, "you are not a committee member");
                    }
                }
                else if (user.role == 2)
                {
                    var committee = db.Committees.Where(f => f.committeeId == user.profileId).FirstOrDefault();
                    if (committee != null)
                    {
                        user.role = 3;
                        user.profileId = committee.facultyId;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, user);
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
        //private void AddStudentToDatabaseAndFinancialAid(Student student, string session, int position, double amount)
        //{
        //    int semes = int.Parse(student.semester.ToString());
        //    String aridno = student.arid_no;

        //    if (!db.Students.Any(a => a.arid_no == aridno))
        //    {
        //        Student s = new Student
        //        {
        //            arid_no = student.arid_no,
        //            name = student.name,
        //            semester = semes,
        //            cgpa = student.cgpa,
        //            prev_cgpa = student.prev_cgpa,
        //            section = student.section,
        //            degree = student.degree,
        //            father_name = student.father_name,
        //            gender = student.gender
        //        };
        //        db.Students.Add(s);
        //        db.SaveChanges();
        //    }

        //    var studentinfo = db.Students.FirstOrDefault(st => st.arid_no == aridno);


        //    String aridNo = aridno.Split('-')[2];

        //    if (!db.Users.Any(du => du.userName == aridNo))
        //    {
        //        User u = new User();
        //        u.userName = aridno;
        //        u.password = aridno;
        //        u.role = 4;
        //        u.profileId = studentinfo.student_id;

        //        db.Users.Add(u);
        //        db.SaveChanges();
        //    }

        //    var meritBase = new MeritBase
        //    {
        //        session = session,
        //        position = position,
        //        studentId = studentinfo.student_id
        //    };
        //    db.MeritBases.Add(meritBase);
        //    db.SaveChanges();

        //    var financialAid = new FinancialAid
        //    {
        //        applicationStatus = "Pending",
        //        session = session,
        //        aidtype = "MeritBase",
        //        applicationId = studentinfo.student_id,
        //        amount = amount.ToString()
        //    };
        //    db.FinancialAids.Add(financialAid);
        //    db.SaveChanges();
        //}
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
                    result.amount = "20000";
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
