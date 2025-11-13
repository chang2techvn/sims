using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SIMS.Data;
using SIMS.Models;

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

builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper(typeof(Program));

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
            Age = 30,
            Gender = "Nam",
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
            Age = 35,
            Gender = "Nam",
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
            Age = 20,
            Gender = "Nữ",
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapRazorPages();

app.Run();
