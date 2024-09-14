using Backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Backend.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class TestingController : ApiController
    {
        FAAToolEntities db = new FAAToolEntities();

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

    }
}
