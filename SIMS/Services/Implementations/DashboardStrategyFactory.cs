using Microsoft.Extensions.DependencyInjection;
using SIMS.Services.Interfaces;

namespace SIMS.Services.Implementations
{
    public class DashboardStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DashboardStrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDashboardStrategy GetStrategy(string role)
        {
            return role.ToLower() switch
            {
                "admin" => _serviceProvider.GetRequiredService<AdminDashboardStrategy>(),
                "lecturer" => _serviceProvider.GetRequiredService<LecturerDashboardStrategy>(),
                "student" => _serviceProvider.GetRequiredService<StudentDashboardStrategy>(),
                _ => throw new ArgumentException("Invalid role")
            };
        }
    }
}