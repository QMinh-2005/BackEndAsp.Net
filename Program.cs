using Mapster;
using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Data;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;
using MyOwnLearning.Repositories;
using MyOwnLearning.Service;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MyOwnLearning.DTO.Response.Admin;

var config = TypeAdapterConfig<User, UserResponse>.NewConfig()
    .Map(dest => dest.Roles, src => src.Roles.Select(r => r.RoleName))
    .Config;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<WebBadmintonContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategotyService, CategoryService>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IBrandService, BrandService>();

builder.Services.AddAuthentication(options =>
{
    // Thiết lập Scheme mặc định là JwtBearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // Đặt là true nếu bạn có quy định Issuer
        ValidateAudience = false, // Đặt là true nếu bạn có quy định Audience
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        // KEY này phải trùng với Key bạn dùng để tạo Token trong AuthService
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyForAuthenticationShouldBeLongEnough"))
    };
});

// Thêm vào trước builder.Build();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
