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

namespace FinancialAidAllocation.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class FacultyController : ApiController
    {
        FAAToolEntities db = new FAAToolEntities();

        [HttpGet]
        public HttpResponseMessage FacultyInfo(int id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK,db.Faculties.Where(f=>f.facultyId==id).FirstOrDefault());
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
                                                    if (currentPosition > 3) break;
                                                }
                                            }
                                        }
                                    }


                                    /* foreach (var policy in strengthPolicies)
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
                                                         //
                                                         var studentsWithFinancialAid = sameCgpaStudents
                                                                                     .Where(studnt => db.FinancialAids
                                                                                         .Any(fa => fa.applicationId == studnt.student_id && fa.session==previousSession.session1))
                                                                                     .ToList();
                                                         //double sharedAmount = CalculateSharedAmount(sameCgpaCount, currentPosition, amount);
                                                         if (studentsWithFinancialAid != null)
                                                         {

                                                             double sharedAmount = CalculateSharedAmount(studentsWithFinancialAid.Count, currentPosition, amount);

                                                             foreach (var studentWithSameCgpa in studentsWithFinancialAid)
                                                             {
                                                                 if (!topStudents.Any(ts => ts.arid_no == studentWithSameCgpa.arid_no))
                                                                 {
                                                                     AddStudentToDatabaseAndFinancialAid(studentWithSameCgpa, session.session1, currentPosition, sharedAmount);
                                                                     topStudents.Add(studentWithSameCgpa);
                                                                     studentCount++;
                                                                 }
                                                             }
                                                             i += sameCgpaCount - 1; // Skip processed student
                                                             currentPosition = i + 1;

                                                         }
                                                         else 
                                                         {

                                                             double sharedAmount = CalculateSharedAmount(sameCgpaCount, currentPosition, amount);

                                                             foreach (var studentWithSameCgpa in sameCgpaStudents)
                                                             {
                                                                 if (!topStudents.Any(ts => ts.arid_no == studentWithSameCgpa.arid_no))
                                                                 {
                                                                     AddStudentToDatabaseAndFinancialAid(studentWithSameCgpa, session.session1, currentPosition, sharedAmount);
                                                                     topStudents.Add(studentWithSameCgpa);
                                                                     studentCount++;
                                                                 }
                                                             }
                                                             i += sameCgpaCount - 1; // Skip processed student
                                                             currentPosition = i + 1;

                                                         }

                                                     }
                                                     else
                                                     {
                                                         currentPosition = i + 1;

                                                         double singleAmount = GetAmount(currentPosition, amount.first_position.ToString(), amount.second_position.ToString(), amount.third_position.ToString());
                                                         AddStudentToDatabaseAndFinancialAid(topCandidates[i], session.session1, currentPosition, singleAmount);
                                                         topStudents.Add(topCandidates[i]);
                                                     }

                                                     *//*if (previousCgpa != topCandidates[i].cgpa)
                                                     {
                                                         currentPosition = i + 1;
                                                     }*//*


                                                     previousCgpa = topCandidates[i].cgpa;
                                                     studentCount++;
                                                     if (currentPosition > 3) break;
                                                 }
                                             }
                                         }
                                     }*/
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
                applicationStatus = "Accepted",
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

        [HttpGet]
        public HttpResponseMessage TeachersGraders(int id)
        {
            try
            {   
                var result = db.graders.Where(gr=>gr.facultyId==id).Join
                    (
                    db.Students,
                    gr=>gr.studentId,
                    s=>s.student_id,
                    (gr,s) =>new 
                    {
                        s
                    }
                    );
                return Request.CreateResponse(HttpStatusCode.OK, result);

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage RateGraderPerformance(int facultyId, int graderId, string rate, string comment)
        {
            try
            {
                // Find the grader based on facultyId, graderId, and session, ensuring they haven't been rated already
                var result = db.graders.FirstOrDefault(f => f.facultyId == facultyId && f.studentId == graderId && f.session == comment && f.feedback == null);

                if (result != null)
                {
                    result.feedback = double.Parse(rate); // Set the feedback rating
                    db.SaveChanges(); // Save the changes to the database

                    return Request.CreateResponse(HttpStatusCode.OK, "Rating added successfully.");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Already Rated");
                }
            }
            catch (Exception ex)
            {
                // Log the exception details here if necessary
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetLowFeedbackRatings()
        {
            try
            {
                // Query to get all graders with feedback rating less than 3
                var lowRatings = db.graders.Where(g => g.feedback <= 3).ToList();

                if (lowRatings.Count > 0)
                {
                    var record = 
                    lowRatings.Join(
                       db.Students,
                       lr => lr.studentId,
                       s => s.student_id,
                       (lr, s) => new
                       {
                           lr.session,
                           lr.feedback,
                           lr.comment,
                           s.arid_no,
                           s.name,
                           s.semester,
                       }
                        );
                    return Request.CreateResponse(HttpStatusCode.OK, record);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No graders found.");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}
