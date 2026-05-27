using ParserDb;

namespace Parser
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using (var initContext = new ForumDbContext())
            {
                initContext.Database.EnsureCreated();
                Console.WriteLine("     Схема БД проверена/создана.");
            }

            var options = DbContextOptionsFactory.Create();
            var repository = new PostRepository(options);
            var parser = new ForumParser(repository);

            string htmlPath = Path.Combine(AppContext.BaseDirectory, "forum_sample.html");

            Console.WriteLine(" Начат парсинг...");
            await parser.ParseAndSaveAsync(htmlPath);
            Console.WriteLine("Готово.");


        }
    }
}
