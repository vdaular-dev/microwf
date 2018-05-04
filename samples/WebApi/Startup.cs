using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using tomware.Microwf.Core;
using tomware.Microwf.Engine;
using WebApi.Common;
using WebApi.Domain;
using WebApi.Identity;
using WebApi.Workflows.Holiday;

namespace WebApi
{
  public class Startup
  {
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
      this.Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddOptions();

      services.AddCors(options =>
        {
          options.AddPolicy("AllowAllOrigins", builder =>
            {
              builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
        })
        .AddMvc();

      services.AddRouting(options => options.LowercaseUrls = true);

      services.AddAuthorization();

      var connection = this.Configuration["ConnectionString"];
      services
        .AddEntityFrameworkSqlite()
        .AddDbContext<DomainContext>(options => options.UseSqlite(connection));

      // Identity
      services.AddIdentityServer()
          .AddDeveloperSigningCredential()
          .AddInMemoryApiResources(Config.GetApiResources())
          .AddInMemoryClients(Config.GetClients())
          .AddTestUsers(Config.GetUsers());

      services.AddAuthentication("Bearer")
        .AddIdentityServerAuthentication(options =>
        {
          options.Authority = "http://localhost:5000";
          options.RequireHttpsMetadata = false;
          options.ApiName = "api1";
        });

      // Swagger
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new Info
        {
          Version = "v1",
          Title = "WebAPI Documentation",
          Description = "WebAPI Documentation"
        });
      });

      services.ConfigureSwaggerGen(options =>
      {
        options.AddSecurityDefinition("Bearer", new ApiKeyScheme
        {
          In = "header",
          Description = "Please insert JWT with Bearer into field",
          Name = "Authorization",
          Type = "apiKey"
        });
      });

      // Custom services
      services.AddScoped<IEnsureDatabaseService, EnsureDatabaseService>();

      services.AddTransient<UserContextService>();
      
      // TODO: add typed configuration options
      var enableWorker = this.Configuration["Worker:Enable"].ToLower() == "true";
      services.AddWorkflowEngineServices<DomainContext>(enableWorker);

      services.AddTransient<IWorkflowDefinition, HolidayApprovalWorkflow>();
      services.AddTransient<IHolidayService, HolidayService>();
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseCors("AllowAllOrigins");

      app.UseIdentityServer();
      app.UseAuthentication();

      app.UseFileServer();

      app.UseMvc();

      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAPI V1");
      });
    }
  }
}
