using Microsoft.EntityFrameworkCore;
using SyntheticGrassClientFinder.Application.Services;
using SyntheticGrassClientFinder.Application.UseCases;
using SyntheticGrassClientFinder.Domain.Repositories;
using SyntheticGrassClientFinder.Domain.Services;
using SyntheticGrassClientFinder.Infrastructure.DataAccess;
using SyntheticGrassClientFinder.Infrastructure.Repositories;
using SyntheticGrassClientFinder.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SyntheticGrassDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<FixedOpenStreetMapGeocodingService>();
builder.Services.AddHttpClient<OptimizedRealClientService>();

builder.Services.AddScoped<IGeocodingService, FixedOpenStreetMapGeocodingService>();
builder.Services.AddScoped<IPlacesSearchService, OptimizedRealClientService>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ISearchClientsUseCase, SearchClientsUseCase>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "🎯 REAL CLIENT FINDER - Grama Sintética", 
        Version = "v1.3",
        Description = @"
🎯 Encontra CLIENTES REAIS: Escolas, Academias, Condomínios, Creches, Campos Society, Salões, Hotéis

✅ HERE API: Busca estabelecimentos reais
✅ OpenStreetMap: Complementa com dados públicos

🎯 CLIENTES ALVO:
🏫 Escolas (quadras esportivas)
👶 Creches (playgrounds)  
💪 Academias (áreas externas)
🏠 Condomínios (áreas de lazer)
⚽ Campos Society
🎉 Salões de Festa
🏨 Hotéis (áreas de lazer)
🏢 Empresas (áreas de descanso)

🚀 AGORA SIM: Clientes REAIS para seu tráfego orgânico!"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SyntheticGrassDbContext>();
    await context.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🎯 [REAL CLIENT FINDER] - Buscador de Clientes REAIS para Grama Sintética!");
Console.WriteLine("✅ HERE API: Busca estabelecimentos reais");
Console.WriteLine("✅ OpenStreetMap: Complementa com dados públicos");
Console.WriteLine();
Console.WriteLine("🎯 CLIENTES ALVO:");
Console.WriteLine("  🏫 Escolas (quadras esportivas)");
Console.WriteLine("  👶 Creches (playgrounds)");
Console.WriteLine("  💪 Academias (áreas externas)");
Console.WriteLine("  🏠 Condomínios (áreas de lazer)");
Console.WriteLine("  ⚽ Campos Society");
Console.WriteLine("  🎉 Salões de Festa");
Console.WriteLine("  🏨 Hotéis (áreas de lazer)");
Console.WriteLine("  🏢 Empresas (áreas de descanso)");
Console.WriteLine();
Console.WriteLine($"🔗 Swagger: http://localhost:5115/swagger");
Console.WriteLine("🚀 AGORA SIM: Clientes REAIS para seu tráfego orgânico!");

app.Run();
