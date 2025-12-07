using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services;
using SIMS.Services.Interfaces;
using SIMS.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<User>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "SIMS.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Cookie expires after 30 days
    options.SlidingExpiration = true; // Reset expiration on each request
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "SIMS.Session";
});

// Add memory cache
builder.Services.AddMemoryCache();

builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper(typeof(Program));

// Register custom services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IMajorService, MajorService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IImportExportService, ImportExportService>();
builder.Services.AddScoped<IAdminViewService, AdminViewService>();
builder.Services.AddScoped<IAdminDataService, AdminDataService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ILecturerService, LecturerService>();
builder.Services.AddScoped<IHomeService, HomeService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    
    // Create roles if they don't exist
    var roles = new[] { "Admin", "Lecturer", "Student" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    
    // Create sample users for testing
    await CreateSampleUsers(userManager, context);
}

static async Task CreateSampleUsers(UserManager<User> userManager, ApplicationDbContext context)
{
    // Admin User
    if (await userManager.FindByEmailAsync("admin@sims.com") == null)
    {
        var admin = new User
        {
            UserName = "admin@sims.com",
            Email = "admin@sims.com",
            Name = "System Administrator",
            Phone = "0123456789",
            DateOfBirth = new DateTime(1994, 1, 1),
            Gender = "Male",
            Address = "123 Admin Street",
            Role = "Admin",
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(admin, "Admin123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            
            var adminRecord = new Admin { UserId = admin.Id };
            context.Admins.Add(adminRecord);
        }
    }

    // Lecturer User  
    if (await userManager.FindByEmailAsync("lecturer@sims.com") == null)
    {
        var lecturer = new User
        {
            UserName = "lecturer@sims.com",
            Email = "lecturer@sims.com",
            Name = "Dr. Nguyễn Văn A",
            Phone = "0987654321",
            DateOfBirth = new DateTime(1989, 5, 15),
            Gender = "Male",
            Address = "456 Lecturer Avenue",
            Role = "Lecturer",
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(lecturer, "Lecturer123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(lecturer, "Lecturer");
            
            var lecturerRecord = new Lecturer 
            { 
                UserId = lecturer.Id, 
                DepartmentId = 1 // Computer Science
            };
            context.Lecturers.Add(lecturerRecord);
        }
    }

    // Student User
    if (await userManager.FindByEmailAsync("student@sims.com") == null)
    {
        var student = new User
        {
            UserName = "student@sims.com",
            Email = "student@sims.com",
            Name = "Trần Thị B",
            Phone = "0369258147",
            StudentCode = "BC00132",
            DateOfBirth = new DateTime(2004, 3, 20),
            Gender = "Female",
            Address = "789 Student Road",
            Role = "Student",
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(student, "Student123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(student, "Student");
            
            var studentRecord = new Student 
            { 
                UserId = student.Id, 
                MajorId = 1 // Software Engineering
            };
            context.Students.Add(studentRecord);
        }
    }

    await context.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Enable session before authentication

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapRazorPages();

app.Run();
