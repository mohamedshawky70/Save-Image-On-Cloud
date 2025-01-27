using BookHaven.Web.Core.Mapping;
using BookHaven.Web.Helper;
using BookHaven.Web.Repository.BaseRepository;
using BookHaven.Web.Seeds;
using BookHaven.Web.Services;
using BookHaven.Web.Settings;
using BookHaven.Web.Tasks;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Reflection;
using UoN.ExpressiveAnnotations.NetCore.DependencyInjection;
using WhatsAppCloudApi.Extensions;
//15/12/2024 The beginning 
//23/1/2025 The End
//38 Day
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SecurityStampValidatorOptions>(option => option.ValidationInterval = TimeSpan.Zero);
// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
/*
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();*/

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultUI() //identity page download with default identity by default
	.AddDefaultTokenProviders(); // To forget passowrd
builder.Services.AddControllersWithViews();

builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(MappingProfile)));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection(nameof(CloudinarySettings)));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(nameof(EmailSettings)));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUnitOfWord, UnitOfWord>();
builder.Services.AddExpressiveAnnotations();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomClaim>();
builder.Services.AddDataProtection().SetApplicationName(nameof(BookHaven));
builder.Services.AddWhatsAppApiClient(builder.Configuration);

builder.Services.AddHangfire(x => x.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var scopFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using var scop = scopFactory.CreateScope();
var rolManager = scop.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var userManager = scop.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
await DefaultRoles.SeedRolesAsync(rolManager);
await DefaultUsers.SeedAdminUserAsync(userManager);
app.UseRouting();




app.UseAuthorization();
//Hangfire
app.UseHangfireDashboard("/hangfire");
var UnitOfWord = scop.ServiceProvider.GetRequiredService<IUnitOfWord>();
var WebHostEnvironment = scop.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
var EmailSender = scop.ServiceProvider.GetRequiredService<IEmailSender>();
//Cron is built in Expression(12 ????)
SubscriptionTask subscriptionTask = new SubscriptionTask(UnitOfWord, WebHostEnvironment, EmailSender);
RecurringJob.AddOrUpdate(() => subscriptionTask.SubscribtionExpirationAlerts(), Cron.Daily);
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
