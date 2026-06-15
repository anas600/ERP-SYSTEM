// ERP-SYSTEM Backend Entry Point
// Phase 0: Foundation + Identity Module

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// TODO Phase 0: Add Identity Module services
// TODO Phase 0: Add Multi-tenancy middleware
// TODO Phase 1: Add Finance Module
// TODO Phase 2: Add Projects Module
// TODO Phase 2: Add Inventory Module

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
