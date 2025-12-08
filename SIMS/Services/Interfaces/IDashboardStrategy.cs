using SIMS.Data;
using SIMS.Models.ViewModels;

namespace SIMS.Services.Interfaces
{
    public interface IDashboardStrategy
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string userId, ApplicationDbContext context);
    }
}