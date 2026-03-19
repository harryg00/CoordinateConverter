using CoordinateConverter.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<GeometryFactory>(
    NtsGeometryServices.Instance.CreateGeometryFactory(srid: 27700));

builder.Services.AddSingleton<IShapeFileService, ShapeFileService>();
builder.Services.AddSingleton<IGpkgHelper, GpkgHelper>();
builder.Services.AddSingleton<ICoordinateTransformer, CoordinateTransformer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
