using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserDb
{
    public class PostRepository
    {
        private readonly DbContextOptions<ForumDbContext> _options;

        public PostRepository(DbContextOptions<ForumDbContext> options)
        {
            _options = options;
        }

        // 1. GetById - получение записи по ID
        public async Task<ForumPost?> GetByIdAsync(int id)
        {
            await using var context = new ForumDbContext(_options);
            return await context.Posts.FindAsync(id);
        }

        // 2. GetByName - получение записей по имени
        public async Task<List<ForumPost>> GetByNameAsync(string name)
        {
            await using var context = new ForumDbContext(_options);
            return await context.Posts
                .Where(p => p.Name == name)
                .ToListAsync();
        }

        // 3. Add - добавление записи
        public async Task AddAsync(int id, string name, string message)
        {
            await using var context = new ForumDbContext(_options);

            var exists = await context.Posts.FindAsync(id);

            if (exists != null) { 
                exists.Name = name;
                exists.Message = message;
            }
            else
            {
                var post = new ForumPost
                {
                    Id = id,
                    Name = name,
                    Message = message
                };

                await context.Posts.AddAsync(post);
            }

            
            await context.SaveChangesAsync();
        }

        // 4. Update - изменение message по ID
        public async Task<bool> UpdateAsync(int id, string newMessage)
        {
            await using var context = new ForumDbContext(_options);

            var post = await context.Posts.FindAsync(id);
            if (post == null)
                return false;

            post.Message = newMessage;
            await context.SaveChangesAsync();
            return true;
        }

        // 5. Delete - удаление записи по ID
        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = new ForumDbContext(_options);

            var post = await context.Posts.FindAsync(id);
            if (post == null)
                return false;

            context.Posts.Remove(post);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
