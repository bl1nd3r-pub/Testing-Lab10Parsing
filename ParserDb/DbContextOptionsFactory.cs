using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserDb
{
    public static class DbContextOptionsFactory
    {
        // Строка подключения хранится в одном месте
        private static readonly string ConnectionString = "Data Source=forum.db";

        public static DbContextOptions<ForumDbContext> Create()
        {
            return new DbContextOptionsBuilder<ForumDbContext>()
                .UseSqlite(ConnectionString)
                .Options;
        }
    }
}
