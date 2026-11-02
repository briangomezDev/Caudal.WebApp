using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure;

/// <summary>
/// Lo que cada motor de base de datos aporta a la infraestructura:
/// su mapeo de entidades y cómo reconoce una violación de clave única.
/// </summary>
public interface IDatabaseProvider
{
    void ConfigureModel(ModelBuilder modelBuilder);
    bool TryGetUniqueConstraintName(DbUpdateException exception, out string? constraintName);
}
