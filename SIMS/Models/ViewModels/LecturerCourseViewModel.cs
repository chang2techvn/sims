using System.Collections.Generic;

namespace SIMS.Models.ViewModels
{
    public class LecturerCourseViewModel
    {
        public Course Course { get; set; } = null!;
        public int StudentCount { get; set; }
    }
}