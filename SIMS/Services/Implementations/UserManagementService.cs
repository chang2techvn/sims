using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly ApplicationDbContext _context;

        public UserManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<(bool success, string message)> AssignStudentToCourseAsync(int studentId, int courseId)
        {
            var existing = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (existing == null)
            {
                var studentCourse = new StudentCourse
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    EnrollmentDate = DateTime.Now
                };
                _context.StudentCourses.Add(studentCourse);
                await _context.SaveChangesAsync();
                return (true, "Student assigned to course successfully!");
            }
            else
            {
                return (false, "Student is already assigned to this course.");
            }
        }

        public async Task<(bool success, string message)> RemoveStudentFromCourseAsync(int studentId, int courseId)
        {
            var studentCourse = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (studentCourse != null)
            {
                _context.StudentCourses.Remove(studentCourse);
                await _context.SaveChangesAsync();
                return (true, "Student removed from course successfully!");
            }
            else
            {
                return (false, "Assignment not found.");
            }
        }

        public async Task<(bool success, string message)> UpdateStudentCourseAssignmentAsync(int currentStudentId, int currentCourseId, int newStudentId, int newCourseId)
        {
            // Check if the new assignment already exists
            var existingNew = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == newStudentId && sc.CourseId == newCourseId);

            if (existingNew != null && (currentStudentId != newStudentId || currentCourseId != newCourseId))
            {
                return (false, "Student is already assigned to this course.");
            }

            // If it's the same assignment, no need to update
            if (currentStudentId == newStudentId && currentCourseId == newCourseId)
            {
                return (true, "Assignment updated successfully!");
            }

            // Remove the current assignment
            var currentAssignment = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == currentStudentId && sc.CourseId == currentCourseId);

            if (currentAssignment != null)
            {
                _context.StudentCourses.Remove(currentAssignment);
            }

            // Create new assignment
            var newAssignment = new StudentCourse
            {
                StudentId = newStudentId,
                CourseId = newCourseId,
                EnrollmentDate = DateTime.Now
            };

            _context.StudentCourses.Add(newAssignment);
            await _context.SaveChangesAsync();

            return (true, "Student course assignment updated successfully!");
        }

        public async Task<(bool success, string message, object? data)> GetCourseStudentsAsync(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return (false, "Course not found.", null);
            }

            var students = await _context.StudentCourses
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Select(sc => new
                {
                    studentId = sc.Student.StudentId,
                    name = sc.Student.User.Name,
                    email = sc.Student.User.Email,
                    avatar = sc.Student.User.Avatar,
                    enrollmentDate = sc.EnrollmentDate
                })
                .ToListAsync();

            var data = new
            {
                success = true,
                courseName = course.CourseName,
                subjectName = course.Subject.Name,
                semesterName = $"{course.Semester.Name} ({course.Semester.StartDate:MMM dd, yyyy} - {course.Semester.EndDate:MMM dd, yyyy})",
                students = students
            };

            return (true, "Course students retrieved successfully.", data);
        }
    }
}