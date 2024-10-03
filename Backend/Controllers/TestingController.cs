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
    public class TestingController : ApiController
    {
        FAAToolEntities db = new FAAToolEntities();

        [HttpGet]
        public HttpResponseMessage GetStudentsEligibleForRejection()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var eligibleStudents = db.MeritBases
                    .Where(mr => mr.session == session.session1 && mr.position != null && mr.position > 1)
                    .Select(mr => new
                    {
                        name = mr.Student.name, 
                        arid_no = mr.Student.arid_no 
                        
                    })
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, eligibleStudents);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [HttpGet]
        public HttpResponseMessage GetStudentEligibleForAcception()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var Accepting = db.MeritBases
                    .Where(ma => ma.session == session.session1 && ma.position == null && ma.position > 1)
                    .Select(ma => new
                    {
                        Name = ma.Student.name,
                        Arid = ma.Student.arid_no,
                    })
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, Accepting);

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [HttpGet]
        public HttpResponseMessage GetEligibleStudents()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                // Get a list of students who meet the eligibility criteria
                var eligibleStudents = db.Students
                    .Where(st => st.cgpa >= 3.5 && // Example condition for eligibility
                                 !db.FinancialAids.Any(fa => fa.applicationId == st.student_id && fa.session == session.session1)) // Students not already having an aid in this session
                    .Select(st => new
                    { 
                        st.name, 
                        st.arid_no

                    }) // Select only Name and arid_no
                    .ToList();

                if (eligibleStudents.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.OK, eligibleStudents);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No eligible students found.");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        // Show All Accepted MeritBase Student 
        [HttpGet]
        public HttpResponseMessage GetMeritBaseAcceptedStudents()
        {
            try
            {
                // Query to find all students with aid type "MeritBase" and application status "Accepted"
                var acceptedMeritBaseStudents = db.FinancialAids
                    .Where(fa => fa.aidtype == "MeritBase" && fa.applicationStatus == "Accepted")
                    .Join(
                        db.Students,
                        fa => fa.applicationId,
                        st => st.student_id,
                        (fa, st) => new
                        {
                            st.name,
                            st.arid_no,
                            AidType = fa.aidtype,
                            ApplicationStatus = fa.applicationStatus
                        })
                    .ToList();

                // Return the list of students if any are found
                if (acceptedMeritBaseStudents.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.OK, acceptedMeritBaseStudents);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No students found with MeritBase aid and Accepted status.");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }


        [HttpGet]
        public HttpResponseMessage AcceptedMeritBaseApplications()
        {
            try
            {
                // Get the most recent session
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                // Join applications with suggestions
                var applications = db.Applications
                    .Where(ap => ap.session == session.session1)
                    .GroupJoin(db.Suggestions,
                        application => application.applicationID,
                        suggestion => suggestion.applicationId,
                        (application, suggestion) => new
                        {
                            application,
                            suggestion
                        });

                // Join the above with students
                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (application, student) => new
                    {
                        student.arid_no,
                        student.name,
                        application.application.applicationID,
                        
                    });

                // Join with FinancialAids and filter by applicationStatus and aidType
                var meritBaseApplications = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus,
                        f.amount,
                        f.aidtype // Include aidType for filtering
                    })
                    .Where(p => p.applicationStatus.ToLower().Equals("accepted") && p.aidtype.ToLower().Equals("meritbase"))
                    .Select(p => new
                    {
                        p.re.arid_no,
                        p.re.name,
               
                        amount = p.amount // Include the accepted amount
                    })
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, meritBaseApplications);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpGet] // those students who have not rated yet
        public HttpResponseMessage GetStudentsNotRated()
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

                // Find students who have not been rated yet
                var studentsNotRated = from student in acceptedStudents
                                       join grader in db.graders.Where(gr => gr.session == session.session1)
                                       on student.student_id equals grader.studentId into gradings
                                       from grader in gradings.DefaultIfEmpty()
                                       where grader == null // Students with no grader entries
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
                                           student.prev_cgpa
                                       };

                var studentsNotRatedList = studentsNotRated.ToList();

                return Request.CreateResponse(HttpStatusCode.OK, studentsNotRatedList);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]   // those student who have not assigned as grader yet
        public HttpResponseMessage GetUnassignedStudents()
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

                // Find students who have not been assigned as a grader
                var unassignedStudents = from student in acceptedStudents
                                         join grader in db.graders.Where(gr => gr.session == session.session1)
                                         on student.student_id equals grader.studentId into gradings
                                         from grader in gradings.DefaultIfEmpty()
                                         where grader == null // Students with no grader entries
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
                                             student.prev_cgpa
                                         };

                var unassignedStudentsList = unassignedStudents.ToList();

                return Request.CreateResponse(HttpStatusCode.OK, unassignedStudentsList);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        // Define a strongly-typed class to store the merit student information
        public class MeritStudent
        {
            public int StudentId { get; set; }
            public int Position { get; set; }
        }

        //[HttpPost]
        //public HttpResponseMessage DecideMeritBaseApplication(int id, String status)
        //{
        //    try
        //    {
        //        var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
        //        var record = db.MeritBases.FirstOrDefault(mr => mr.studentId == id && mr.session == session.session1);
        //        var result = db.FinancialAids.FirstOrDefault(fn => fn.applicationId == record.studentId);

        //        if (record != null && status == "Accepted")
        //        {
        //            result.applicationStatus = status;
        //            db.SaveChanges();
        //            return Request.CreateResponse(HttpStatusCode.OK, "Accepted");
        //        }
        //        else if (status == "Rejected")
        //        {
        //            // Get the top 5 students in merit order using the new MeritStudent class
        //            var topStudents = db.MeritBases
        //                .Where(m => m.session == session.session1)
        //                .OrderBy(m => m.position)
        //                .Take(5) // Only consider top 5 students
        //                .Select(m => new MeritStudent
        //                {
        //                    StudentId = m.studentId,
        //                    Position = m.position
        //                })
        //                .ToList();

        //            // Predefined amounts for the top 3 positions
        //            var amountMap = new Dictionary<int, double>
        //    {
        //        { 1, 25000 }, // 1st position amount
        //        { 2, 14000 }, // 2nd position amount
        //        { 3, 10000 }  // 3rd position amount
        //    };

        //            // Call the method to handle shifting of amounts
        //            ShuffleFinancialAid(topStudents, amountMap, id);

        //            result.applicationStatus = status;
        //            db.SaveChanges();
        //            return Request.CreateResponse(HttpStatusCode.OK, "Rejected and Amount Reassigned");
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "Not Found");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
        //    }
        //}

        //// Updated method with strong typing
        //private void ShuffleFinancialAid(List<MeritStudent> topStudents, Dictionary<int, double> amountMap, int rejectedStudentId)
        //{
        //    // Find the rejected student
        //    var rejectedStudent = topStudents.FirstOrDefault(s => s.StudentId == rejectedStudentId);
        //    if (rejectedStudent == null) return;

        //    // Start the amount shifting process
        //    int startPosition = rejectedStudent.Position;

        //    // Loop through top 5 students, shifting amounts only for the top 3
        //    for (int i = startPosition; i <= 5; i++)
        //    {
        //        var currentStudent = topStudents.FirstOrDefault(s => s.Position == i);
        //        var nextStudent = topStudents.FirstOrDefault(s => s.Position == i + 1);

        //        if (i <= 3 && currentStudent != null && nextStudent != null)
        //        {
        //            // Move current amount to the next student
        //            db.FinancialAids.FirstOrDefault(fa => fa.applicationId == nextStudent.StudentId).amount = amountMap[i].ToString();
        //            db.SaveChanges();
        //        }
        //        else if (i > 3 && currentStudent != null && nextStudent != null)
        //        {
        //            // If the current student (4th or 5th) rejects, shift to the next student
        //            var currentAid = db.FinancialAids.FirstOrDefault(fa => fa.applicationId == currentStudent.StudentId);
        //            var nextAid = db.FinancialAids.FirstOrDefault(fa => fa.applicationId == nextStudent.StudentId);

        //            if (currentAid != null && nextAid != null)
        //            {
        //                nextAid.amount = currentAid.amount; // Shift rejected amount to next student
        //                db.SaveChanges();
        //            }
        //        }

        //        // If we reach the 5th student and they reject, end the process
        //        if (i == 5 && nextStudent == null)
        //        {
        //            break;
        //        }
        //    }
        //}



        //[HttpPost]
        //public HttpResponseMessage DecideMeritBaseApplication(int id, String status)
        //{
        //    try
        //    {
        //        var budget = db.Budgets.OrderByDescending(b => b.budgetId).FirstOrDefault();

        //        var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
        //        var record = db.MeritBases.Where(mr => mr.studentId == id && mr.session == session.session1).FirstOrDefault();
        //        var result = db.FinancialAids.Where(fn => fn.applicationId == record.studentId).FirstOrDefault();
        //        if (record != null && status == "Accepted")
        //        {
        //            result.applicationStatus = status;
        //            db.SaveChanges();
        //            return Request.CreateResponse(HttpStatusCode.OK, "Accepted");
        //        }
        //        else if (status == "Rejected")
        //        {
        //            // Get students who are ranked below the rejected student
        //            var studentsBelow = db.MeritBases
        //                .Where(mr => mr.position > record.position && mr.session == session.session1)
        //                .OrderBy(mr => mr.position)
        //                .ToList();

        //            // Update the positions of the students
        //            foreach (var student in studentsBelow)
        //            {
        //                student.position -= 1; // Move each student up by 1 position
        //            }

        //            db.SaveChanges();

        //            // Now find the alternate top student from the reserves
        //            var reservetoper = db.Students.Join
        //            (
        //                db.Reserves.Where(r => r.session == session.session1),
        //                st => st.student_id,
        //                res => res.student_id,
        //                (st, res) => new
        //                {
        //                    st,
        //                    res
        //                }
        //            );

        //            var alternateToper = reservetoper.OrderByDescending(rt => rt.st.cgpa).Select(s => new
        //            {
        //                s.st,
        //            }).FirstOrDefault();

        //            if (alternateToper != null)
        //            {
        //                // Assign the alternate top student to the vacated position
        //                MeritBase newEntry = new MeritBase();
        //                newEntry.session = session.session1;
        //                newEntry.position = studentsBelow.Count + 1; // Assign last position
        //                newEntry.studentId = alternateToper.st.student_id;
        //                db.MeritBases.Add(newEntry);
        //                db.SaveChanges();

        //                FinancialAid f = new FinancialAid();
        //                f.session = session.session1;
        //                f.amount = result.amount;
        //                f.applicationStatus = "Pending";
        //                f.aidtype = "MeritBase";
        //                f.applicationId = alternateToper.st.student_id;
        //                db.FinancialAids.Add(f);
        //                db.SaveChanges();

        //                result.applicationStatus = status; // Update rejected student's status
        //                db.SaveChanges();

        //                // Remove the alternate top student from the reserves
        //                var del = db.Reserves.Where(rev => rev.student_id == alternateToper.st.student_id).FirstOrDefault();
        //                db.Reserves.Remove(del);
        //                db.SaveChanges();
        //            }
        //            else
        //            {
        //                result.applicationStatus = status;
        //                db.SaveChanges();
        //            }

        //            return Request.CreateResponse(HttpStatusCode.OK, "Rejected and positions reshuffled");
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "Not Found");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage DecideMeritBaseApplication(int id, String status)
        //{
        //    try
        //    {
        //        var budget = db.Budgets.OrderByDescending(b => b.budgetId).FirstOrDefault();
        //        var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

        //        // Fetch the record of the student based on studentId and session
        //        var record = db.MeritBases.Where(mr => mr.studentId == id && mr.session == session.session1).FirstOrDefault();

        //        // Fetch the financial aid record based on applicationId
        //        var result = db.FinancialAids.Where(fn => fn.applicationId == record.studentId).FirstOrDefault();

        //        if (record != null && status == "Accepted")
        //        {
        //            result.applicationStatus = status;
        //            db.SaveChanges();
        //            return Request.CreateResponse(HttpStatusCode.OK, "Accepted");
        //        }
        //        else if (status == "Rejected")
        //        {
        //            // Handle nullable position (int?)
        //            int currentPosition = record.position ?? 0; // Use 0 if position is null

        //            if (currentPosition == 1) // Handle rejection for 1st position
        //            {
        //                // Promote 2nd position to 1st
        //                var secondPosition = db.MeritBases.Where(m => m.position == 2 && m.session == session.session1).FirstOrDefault();
        //                if (secondPosition != null)
        //                {
        //                    secondPosition.position = 1; // Move 2nd to 1st
        //                    db.SaveChanges();
        //                }

        //                // Promote 3rd position to 2nd
        //                var thirdPosition = db.MeritBases.Where(m => m.position == 3 && m.session == session.session1).FirstOrDefault();
        //                if (thirdPosition != null)
        //                {
        //                    thirdPosition.position = 2; // Move 3rd to 2nd
        //                    db.SaveChanges();
        //                }

        //                // Assign new student to 3rd position
        //                var newTopper = db.Students.Join(
        //                    db.Reserves.Where(r => r.session == session.session1),
        //                    st => st.student_id,
        //                    res => res.student_id,
        //                    (st, res) => new { st, res }
        //                )
        //                .OrderByDescending(rt => rt.st.cgpa)
        //                .Select(s => new { s.st })
        //                .FirstOrDefault();

        //                if (newTopper != null)
        //                {
        //                    MeritBase newMeritBase = new MeritBase
        //                    {
        //                        session = session.session1,
        //                        position = 3,
        //                        studentId = newTopper.st.student_id
        //                    };
        //                    db.MeritBases.Add(newMeritBase);
        //                    db.SaveChanges();

        //                    FinancialAid newFinancialAid = new FinancialAid
        //                    {
        //                        session = session.session1,
        //                        amount = result.amount,
        //                        applicationStatus = "Pending",
        //                        aidtype = "MeritBase",
        //                        applicationId = newTopper.st.student_id
        //                    };
        //                    db.FinancialAids.Add(newFinancialAid);
        //                    db.SaveChanges();

        //                    // Remove from reserves
        //                    var reserveRecord = db.Reserves.FirstOrDefault(rev => rev.student_id == newTopper.st.student_id);
        //                    if (reserveRecord != null)
        //                    {
        //                        db.Reserves.Remove(reserveRecord);
        //                        db.SaveChanges();
        //                    }
        //                }

        //                result.applicationStatus = status;
        //                db.SaveChanges();
        //                return Request.CreateResponse(HttpStatusCode.OK, "Rejected and positions updated");
        //            }
        //            else if (currentPosition == 2 || currentPosition == 3) // Handle rejection for 2nd or 3rd position
        //            {
        //                var reservetoper = db.Students.Join(
        //                    db.Reserves.Where(r => r.session == session.session1),
        //                    st => st.student_id,
        //                    res => res.student_id,
        //                    (st, res) => new { st, res }
        //                )
        //                .OrderByDescending(rt => rt.st.cgpa)
        //                .Select(s => new { s.st })
        //                .FirstOrDefault();

        //                if (reservetoper != null)
        //                {
        //                    MeritBase m = new MeritBase
        //                    {
        //                        session = session.session1,
        //                        position = record.position.GetValueOrDefault(), // Handle nullable position
        //                        studentId = reservetoper.st.student_id
        //                    };
        //                    db.MeritBases.Add(m);
        //                    db.SaveChanges();

        //                    FinancialAid f = new FinancialAid
        //                    {
        //                        session = session.session1,
        //                        amount = result.amount,
        //                        applicationStatus = "Pending",
        //                        aidtype = "MeritBase",
        //                        applicationId = reservetoper.st.student_id
        //                    };
        //                    db.FinancialAids.Add(f);
        //                    db.SaveChanges();

        //                    var del = db.Reserves.FirstOrDefault(rev => rev.student_id == reservetoper.st.student_id);
        //                    if (del != null)
        //                    {
        //                        db.Reserves.Remove(del);
        //                        db.SaveChanges();
        //                    }

        //                    result.applicationStatus = status;
        //                    db.SaveChanges();
        //                }

        //                return Request.CreateResponse(HttpStatusCode.OK, "Rejected and alternate student assigned");
        //            }
        //        }

        //        return Request.CreateResponse(HttpStatusCode.NotFound, "Not Found");
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage GetStudentFinancialAidHistory(int studentId)
        //{
        //    try
        //    {
        //        // Assuming you have a database context (db) and a FinancialAidHistories table
        //        var history = db.FinancialAids
        //                        .Where(h => h.applicationId == studentId)
        //                        .ToList();

        //        if (history == null || history.Count == 0)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "No financial aid history found for the student.");
        //        }

        //        return Request.CreateResponse(HttpStatusCode.OK, history);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        //[HttpPost]
        //public HttpResponseMessage DecideMeritBaseApplication(int id, String status)
        //{
        //    try
        //    {
        //        var budget = db.Budgets.OrderByDescending(b => b.budgetId).FirstOrDefault();

        //        var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
        //        var record = db.MeritBases.Where(mr => mr.studentId == id && mr.session == session.session1).FirstOrDefault();
        //        var result = db.FinancialAids.Where(fn => fn.applicationId == record.studentId).FirstOrDefault();
        //        if (record != null && status == "Accepted")
        //        {
        //            result.applicationStatus = status;
        //            db.SaveChanges();
        //            return Request.CreateResponse(HttpStatusCode.OK, "Accepted");
        //        }
        //        else if (status == "Rejected")
        //        {

        //            var reservetoper = db.Students.Join
        //                (
        //                    db.Reserves.Where(r => r.session == session.session1),
        //                    st => st.student_id,
        //                    res => res.student_id,
        //                    (st, res) => new
        //                    {
        //                        st,
        //                        res
        //                    }
        //                );

        //            var alternateToper = reservetoper.OrderByDescending(rt => rt.st.cgpa).Select(s => new
        //            {
        //                s.st,
        //            }).FirstOrDefault();

        //            if (reservetoper != null)
        //            {

        //                MeritBase m = new MeritBase();
        //                m.session = session.session1;
        //                m.position = record.position;
        //                m.studentId = alternateToper.st.student_id;
        //                db.MeritBases.Add(m);
        //                db.SaveChanges();

        //                FinancialAid f = new FinancialAid();
        //                f.session = session.session1;
        //                f.amount = result.amount;
        //                f.applicationStatus = "Pending";
        //                f.aidtype = "MeritBase";
        //                f.applicationId = alternateToper.st.student_id;
        //                db.FinancialAids.Add(f);
        //                db.SaveChanges();
        //                result.applicationStatus = status;
        //                db.SaveChanges();

        //                var del = db.Reserves.Where(rev => rev.student_id == alternateToper.st.student_id).FirstOrDefault();
        //                db.Reserves.Remove(del);
        //                db.SaveChanges();
        //            }
        //            else
        //            {
        //                result.applicationStatus = status;
        //                db.SaveChanges();
        //                var del = db.Reserves.Where(rev => rev.student_id == alternateToper.st.student_id).FirstOrDefault();
        //                db.Reserves.Remove(del);
        //                db.SaveChanges();
        //            }
        //            return Request.CreateResponse(HttpStatusCode.OK, "Rejected");
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "Not Found");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
        //    }
        //}

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
                    result.amount = "10000";
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
