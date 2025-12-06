using SIMS.Models;
using System.Collections.Generic;

namespace SIMS.Models.ViewModels
{
    public class AssignedClassViewModel
    {
        public Course Course { get; set; } = null!;
        public List<Student> Students { get; set; } = new List<Student>();
    }
}