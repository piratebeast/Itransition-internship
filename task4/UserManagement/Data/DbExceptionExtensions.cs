using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace UserManagement.Data
{
    public static class DbExceptionExtensions
    {
        private const string UniqueViolation = "23505";

        public static bool IsUniqueViolation(this DbUpdateException ex, string? indexName = null)
        {
            if (ex.InnerException is not PostgresException pg) return false;
            if (pg.SqlState != UniqueViolation) return false;
            return indexName is null || pg.ConstraintName == indexName;
        }
    }
}
