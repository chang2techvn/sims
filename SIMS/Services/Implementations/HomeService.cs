using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private readonly ApplicationDbContext _context;
        private readonly DashboardStrategyFactory _strategyFactory;

        public HomeService(ApplicationDbContext context, DashboardStrategyFactory strategyFactory)
        {
            _context = context;
            _strategyFactory = strategyFactory;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, string role)
        {
            var strategy = _strategyFactory.GetStrategy(role);
            return await strategy.GetDashboardDataAsync(userId, _context);
        }
    }
}