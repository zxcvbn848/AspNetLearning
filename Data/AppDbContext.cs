using AspNetLearning.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetLearning.Data;

/// <summary>
/// 代表應用程式與 PostgreSQL 資料庫之間的一次工作階段。
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// 建立 AppDbContext，並接收 Dependency Injection 提供的資料庫設定。
    /// </summary>
    /// <param name="options">包含 PostgreSQL Provider 與連線設定的選項。</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>代表 PostgreSQL 中的 todo_items 資料表。</summary>
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    /// <summary>代表 PostgreSQL 中的 users 資料表。</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// 設定 TodoItem 與 PostgreSQL 資料表的對應規則及初始資料。
    /// </summary>
    /// <param name="modelBuilder">用來建立 EF Core 資料模型的物件。</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity<TodoItem> 表示以下規則都套用到 TodoItem。
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.ToTable("todo_items");
            entity.HasKey(todoItem => todoItem.Id);

            // Title 在資料庫中不可為 null，且最多儲存 100 個字元。
            entity.Property(todoItem => todoItem.Title)
                .IsRequired()
                .HasMaxLength(100);

            // HasData 讓第一個 Migration 建立資料表時一併加入學習資料。
            entity.HasData(
                new TodoItem
                {
                    Id = 1,
                    Title = "閱讀第一階段文件",
                    IsCompleted = true
                },
                new TodoItem
                {
                    Id = 2,
                    Title = "認識 Class 與 List",
                    IsCompleted = false
                });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(254);

            // Unique Index 讓 PostgreSQL 從資料庫層阻止重複 Email。
            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .IsRequired();

            entity.Property(user => user.CreatedAtUtc)
                .IsRequired();
        });
    }
}
