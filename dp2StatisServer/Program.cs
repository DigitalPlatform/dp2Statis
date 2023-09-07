using System;
using dp2StatisServer.Data;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
// using static SnCenter.Controllers.ApiController;


namespace dp2StatisServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var services = builder.Services;

            // Add services to the container.
            services.AddControllersWithViews()
                // https://stackoverflow.com/questions/36452468/swagger-ui-web-api-documentation-present-enums-as-strings
                .AddJsonOptions(x =>
                {
                    // 禁止返回的属性名使用 camel 形态
                    // https://stackoverflow.com/questions/58476681/asp-net-core-3-0-system-text-json-camel-case-serialization
                    x.JsonSerializerOptions.PropertyNamingPolicy = null;
                    x.JsonSerializerOptions.WriteIndented = true;
                    // x.JsonSerializerOptions.Converters.Add(new Controllers.v3Controller.ByteArrayConverter());
                });

            // session support
            {
                services.AddDistributedMemoryCache();

                services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(10);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });
            }

            {
                services.AddSwaggerGen(c =>
                {
                    // https://stackoverflow.com/questions/58834430/c-sharp-net-core-swagger-trying-to-use-multiple-api-versions-but-all-end-point
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "dp2Statis API V1",
                        Description = "dp2 统计 API V1",
                        Version = "v1",
                        License = new OpenApiLicense
                        {
                            Name = "Apache-2.0",
                            Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
                        },
                        Contact = new OpenApiContact
                        {
                            Name = "xietao",
                            Email = "xietao@dp2003.com",
                            Url = new Uri("https://github.com/DigitalPlatform/dp2")
                        }
                    });

                    // c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                    /*
                    // version < 3.0 like this: c.OperationFilter<ExamplesOperationFilter>(); 
                    // version 3.0 like this: c.AddSwaggerExamples(services.BuildServiceProvider());
                    // version > 4.0 like this:
                    c.ExampleFilters();
                    // c.SchemaFilter<EnumSchemaFilter>();
                    */

                });

                /*
                services.AddSwaggerExamplesFromAssemblyOf<VerifyResponseExamples>();
                */
            }

            /*
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IJournalRepository, JournalRepository>();
            */
            services.AddScoped<IInstanceRepository, InstanceRepository>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                // https://github.com/domaindrivendev/Swashbuckle.WebApi/issues/971#issuecomment-335653350
                c.SwaggerEndpoint($"v1/swagger.json", "License API V1");
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // session support
            app.UseSession();

            app.MapControllerRoute("instance",
                             "instance/{name}/{action}",
                             defaults: new
                             {
                                 controller = "instance",
                                 name = "[default]",
                                 action = "index"
                             });
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            /*
            app.MapControllerRoute(
                name: "instance",
                pattern: "{controller=Instance}/{instance=}/{action=Index}");
            */
            app.Run();
        }

#if REMOVED
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            var services = builder.Services;



            // session support
            {
                services.AddDistributedMemoryCache();

                services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(10);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // session support
            app.UseSession();

            app.MapRazorPages();

            app.Run();
        }

#endif
    }
}