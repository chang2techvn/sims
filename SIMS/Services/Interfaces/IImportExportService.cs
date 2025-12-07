using Microsoft.AspNetCore.Http;
using SIMS.Models;

namespace SIMS.Services.Interfaces
{
    public interface IImportExportService
    {
        Task<ImportResult> ImportUsersAsync(IFormFile file, bool skipDuplicates = true);
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}