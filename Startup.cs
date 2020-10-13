using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using signup_verification.Helpers;
using signup_verification.Middleware;
using signup_verification.Services;

namespace signup_verification
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddDbContext<DataContext>();
      services.AddCors();
      services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);
      services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
      services.AddSwaggerGen();

      services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

      services.AddScoped<IAccountService, AccountService>();
      services.AddScoped<IEmailService, EmailService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DataContext context)
    {
      context.Database.Migrate();

      app.UseSwagger();
      app.UseSwaggerUI(s => s.SwaggerEndpoint("/swagger/v1/swagger.json", "ASP.NET Core Sign-up and Verification API"));

      app.UseRouting();

      app.UseCors(c => c
        .SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

      app.UseMiddleware<ErrorHandlerMiddleware>();
      app.UseMiddleware<JwtMiddleware>();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
