using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParserDb;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ParserDbTester
{
    public class PostRepositoryTests : IAsyncLifetime
    {
        private string _testDbPath = string.Empty;
        private DbContextOptions<ForumDbContext> _options = null!;

        public async Task InitializeAsync()
        {
            // Генерируем уникальный путь для каждого теста
            _testDbPath = $"test_forum_{Guid.NewGuid()}.db";

            var optionsBuilder = new DbContextOptionsBuilder<ForumDbContext>();
            optionsBuilder.UseSqlite($"Data Source={_testDbPath}");
            _options = optionsBuilder.Options;

            // Создаём БД перед каждым тестом
            await using var context = new ForumDbContext(_options);
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            // Принудительно очищаем все активные соединения с этой БД
            SqliteConnection.ClearAllPools();

            // Ждём немного, чтобы ОС успела закрыть файловые дескрипторы
            await Task.Delay(100);

            // Удаляем временную БД после каждого теста
            if (!string.IsNullOrEmpty(_testDbPath) && File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                }
                catch (IOException)
                {
                    // Если файл всё ещё заблокирован, игнорируем ошибку
                    // (файл будет удалён при следующем запуске или вручную)
                }
            }

            await Task.CompletedTask;
        }

        private async Task<PostRepository> CreateRepositoryWithTestData()
        {
            await using var context = new ForumDbContext(_options);

            context.Posts.AddRange(
                new ForumPost { Id = 1, Name = "User1", Message = "Message 1" },
                new ForumPost { Id = 2, Name = "User2", Message = "Message 2" },
                new ForumPost { Id = 3, Name = "User1", Message = "Message 3 from User1" }
            );
            await context.SaveChangesAsync();

            return new PostRepository(_options);
        }

        // ========== GET BY ID TESTS ==========

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsPost()
        {
            var repository = await CreateRepositoryWithTestData();
            var post = await repository.GetByIdAsync(1);
            Assert.NotNull(post);
            Assert.Equal(1, post.Id);
            Assert.Equal("User1", post.Name);
            Assert.Equal("Message 1", post.Message);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var repository = await CreateRepositoryWithTestData();
            var post = await repository.GetByIdAsync(999);
            Assert.Null(post);
        }

        // ========== GET BY NAME TESTS ==========

        [Fact]
        public async Task GetByNameAsync_ExistingName_ReturnsAllMatchingPosts()
        {
            var repository = await CreateRepositoryWithTestData();
            var posts = await repository.GetByNameAsync("User1");
            Assert.NotNull(posts);
            Assert.Equal(2, posts.Count);
            Assert.All(posts, p => Assert.Equal("User1", p.Name));
        }

        [Fact]
        public async Task GetByNameAsync_NonExistingName_ReturnsEmptyList()
        {
            var repository = await CreateRepositoryWithTestData();
            var posts = await repository.GetByNameAsync("Nobody");
            Assert.NotNull(posts);
            Assert.Empty(posts);
        }

        // ========== ADD TESTS ==========

        [Fact]
        public async Task AddAsync_NewId_AddsPostToDatabase()
        {
            var repository = new PostRepository(_options);
            await repository.AddAsync(100, "NewUser", "New message content");

            await using var verifyContext = new ForumDbContext(_options);
            var addedPost = await verifyContext.Posts.FindAsync(100);
            Assert.NotNull(addedPost);
            Assert.Equal(100, addedPost.Id);
            Assert.Equal("NewUser", addedPost.Name);
            Assert.Equal("New message content", addedPost.Message);
        }

        [Fact]
        public async Task AddAsync_DuplicateId_UpdatesExistingPostInsteadOfAdding()
        {
            await using var setupContext = new ForumDbContext(_options);
            setupContext.Posts.Add(new ForumPost { Id = 1, Name = "OriginalUser", Message = "Original message" });
            await setupContext.SaveChangesAsync();

            var repository = new PostRepository(_options);
            await repository.AddAsync(1, "UpdatedUser", "Updated message");

            await using var verifyContext = new ForumDbContext(_options);
            var posts = await verifyContext.Posts.ToListAsync();
            Assert.Single(posts);

            var updatedPost = await verifyContext.Posts.FindAsync(1);
            Assert.Equal("UpdatedUser", updatedPost.Name);
            Assert.Equal("Updated message", updatedPost.Message);
        }

        // ========== UPDATE TESTS ==========

        [Fact]
        public async Task UpdateAsync_ExistingId_UpdatesMessageAndReturnsTrue()
        {
            var repository = await CreateRepositoryWithTestData();
            var result = await repository.UpdateAsync(2, "Completely new message");
            Assert.True(result);
            var updatedPost = await repository.GetByIdAsync(2);
            Assert.Equal("Completely new message", updatedPost.Message);
            Assert.Equal("User2", updatedPost.Name);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingId_ReturnsFalse()
        {
            var repository = await CreateRepositoryWithTestData();
            var result = await repository.UpdateAsync(999, "This post doesn't exist");
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_EmptyMessage_AllowsEmptyString()
        {
            var repository = await CreateRepositoryWithTestData();
            var result = await repository.UpdateAsync(3, "");
            Assert.True(result);
            var updatedPost = await repository.GetByIdAsync(3);
            Assert.Equal("", updatedPost.Message);
        }

        // ========== DELETE TESTS ==========

        [Fact]
        public async Task DeleteAsync_ExistingId_RemovesPostAndReturnsTrue()
        {
            var repository = await CreateRepositoryWithTestData();
            var postBefore = await repository.GetByIdAsync(1);
            Assert.NotNull(postBefore);

            var result = await repository.DeleteAsync(1);
            Assert.True(result);
            var postAfter = await repository.GetByIdAsync(1);
            Assert.Null(postAfter);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            var repository = await CreateRepositoryWithTestData();
            var result = await repository.DeleteAsync(999);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_AfterDeletion_OtherPostsRemainIntact()
        {
            var repository = await CreateRepositoryWithTestData();
            await repository.DeleteAsync(2);

            var post1 = await repository.GetByIdAsync(1);
            var post3 = await repository.GetByIdAsync(3);
            Assert.NotNull(post1);
            Assert.NotNull(post3);

            var post2 = await repository.GetByIdAsync(2);
            Assert.Null(post2);
        }
    }
}