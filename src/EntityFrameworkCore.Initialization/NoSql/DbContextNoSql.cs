using LiteDB;
using System;
using System.IO;

namespace EntityFrameworkCore.Initialization.NoSql
{
    //http://www.litedb.org/
    public abstract class DbContextNoSql : LiteRepository, IDisposable
    {
        public string ConnectionString { get; }
        public DbContextNoSql(string connectionString)
            : base(connectionString)
        {
            ConnectionString = connectionString;
        }

        private readonly MemoryStream _memoryStream;
        public DbContextNoSql(MemoryStream memoryStream)
           : base(memoryStream)
        {
            _memoryStream = memoryStream;
        }

        public abstract void Seed();

        public new void Dispose()
        {
            base.Dispose();

            if (_memoryStream != null)
                _memoryStream.Dispose();
        }
    }
}
