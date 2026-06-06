namespace Tibr.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception thrown when seed data operations fail
    /// </summary>
    public class SeedDataException : Exception
    {
        public SeedDataException(string message) : base(message)
        {
        }

        public SeedDataException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when database connection fails during seeding
    /// </summary>
    public class SeedDatabaseConnectionException : SeedDataException
    {
        public SeedDatabaseConnectionException(string message) : base(message)
        {
        }

        public SeedDatabaseConnectionException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when admin data creation fails
    /// </summary>
    public class SeedAdminCreationException : SeedDataException
    {
        public SeedAdminCreationException(string message) : base(message)
        {
        }

        public SeedAdminCreationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
