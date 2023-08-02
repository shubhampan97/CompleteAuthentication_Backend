using CompleteAuthentication.Data;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddAuthentication(o =>
{
    o.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
    o.DefaultForbidScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie().AddGoogleOpenIdConnect(options =>
{
    options.ClientId = "3387507828-jc2uq9tk0bbiq02iru95dppjieh7n2b3.apps.googleusercontent.com";
    options.ClientSecret = "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";
});

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
_ = new JwtHeader();
_ = new JwtPayload();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
 
app.UseCors(options => options.WithOrigins(new[] { "http://localhost:4200","http://localhost:3000"}).
                                AllowAnyHeader().
                                AllowAnyMethod().
                                AllowCredentials());

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
