using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace SIMS.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMemoryCache _cache;
        private const string USER_STATS_CACHE_KEY = "UserStatistics";

        public UserManagementService(ApplicationDbContext context, UserManager<User> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
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

        public async Task<StudentCourse?> GetStudentCourseAsync(int studentId, int courseId)
        {
            return await _context.StudentCourses
                .Include(sc => sc.Student)
                .ThenInclude(s => s.User)
                .Include(sc => sc.Course)
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
        }

        public async Task<(bool success, string message)> AddUserAsync(string name, string email, string password, string role, string? studentCode, DateTime? dateOfBirth, string? phone, string? gender, string? address)
        {
            try
            {
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    Name = name,
                    Role = role,
                    StudentCode = studentCode,
                    DateOfBirth = dateOfBirth,
                    Phone = phone,
                    Gender = gender,
                    Address = address,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role);

                    // Create role-specific records
                    switch (role.ToLower())
                    {
                        case "student":
                            var student = new Student { UserId = user.Id };
                            _context.Students.Add(student);
                            break;
                        case "lecturer":
                            var lecturer = new Lecturer { UserId = user.Id };
                            _context.Lecturers.Add(lecturer);
                            break;
                        case "admin":
                            var admin = new Admin { UserId = user.Id };
                            _context.Admins.Add(admin);
                            break;
                    }

                    await _context.SaveChangesAsync();
                    await InvalidateUserStatsCache();
                    return (true, "User added successfully!");
                }
                else
                {
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        public async Task<(bool success, string message)> UpdateUserAsync(string id, string name, string role, string? studentCode, DateTime? dateOfBirth, string? phone, string? gender, string? address, string? password, string? avatar)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return (false, "User not found");

                user.Name = name;
                user.Role = role;
                user.StudentCode = studentCode;
                user.DateOfBirth = dateOfBirth;
                user.Phone = phone;
                user.Gender = gender;
                user.Address = address;

                if (!string.IsNullOrEmpty(avatar))
                {
                    user.Avatar = avatar;
                }

                var result = await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(password))
                {
                    var passwordResult = await _userManager.RemovePasswordAsync(user);
                    if (passwordResult.Succeeded)
                    {
                        passwordResult = await _userManager.AddPasswordAsync(user, password);
                        if (!passwordResult.Succeeded)
                        {
                            return (false, "Failed to update password: " + string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        return (false, "Failed to remove old password: " + string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    }
                }

                if (result.Succeeded)
                {
                    await InvalidateUserStatsCache();
                    return (true, "User updated successfully!");
                }
                else
                {
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        public async Task<(bool success, string message)> DeleteUserAsync(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return (false, "User not found");

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    // Delete role-specific records
                    switch (user.Role.ToLower())
                    {
                        case "student":
                            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                            if (student != null)
                            {
                                _context.Students.Remove(student);
                            }
                            break;
                        case "lecturer":
                            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                            if (lecturer != null)
                            {
                                _context.Lecturers.Remove(lecturer);
                            }
                            break;
                        case "admin":
                            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == user.Id);
                            if (admin != null)
                            {
                                _context.Admins.Remove(admin);
                            }
                            break;
                    }

                    await _context.SaveChangesAsync();
                    await InvalidateUserStatsCache();
                    return (true, "User deleted successfully!");
                }
                else
                {
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        private async Task InvalidateUserStatsCache()
        {
            _cache.Remove(USER_STATS_CACHE_KEY);
            await Task.CompletedTask;
        }
    }
}