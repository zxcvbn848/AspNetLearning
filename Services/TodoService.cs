using AspNetLearning.Data;
using AspNetLearning.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetLearning.Services;

/// <summary>
/// 透過 Entity Framework Core 管理 PostgreSQL 中的待辦事項。
/// </summary>
public sealed class TodoService
{
    // readonly 表示建構完成後，_dbContext 不可改指向其他 AppDbContext。
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// 建立 TodoService，並接收 Dependency Injection 提供的 AppDbContext。
    /// </summary>
    /// <param name="dbContext">目前 HTTP Request 使用的資料庫工作階段。</param>
    public TodoService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 從資料庫取得所有待辦事項。
    /// </summary>
    /// <param name="userId">目前登入會員的 Id。</param>
    /// <returns>依照 Id 排序的待辦事項清單。</returns>
    public async Task<List<TodoItem>> GetAllAsync(int userId)
    {
        // AsNoTracking 表示這次只讀取資料，不需要 EF Core 追蹤物件變更。
        return await _dbContext.TodoItems
            .AsNoTracking()
            .Where(todoItem => todoItem.UserId == userId)
            .OrderBy(todoItem => todoItem.Id)
            .ToListAsync();
    }

    /// <summary>
    /// 建立並保存一筆待辦事項。
    /// </summary>
    /// <param name="userId">擁有這筆待辦事項的會員 Id。</param>
    /// <param name="title">待辦事項的文字內容。</param>
    /// <returns>包含資料庫產生 Id 的待辦事項。</returns>
    public async Task<TodoItem> CreateAsync(int userId, string title)
    {
        // Id 不需手動設定，PostgreSQL 會在新增資料時自動產生。
        TodoItem todoItem = new()
        {
            Title = title.Trim(),
            IsCompleted = false,
            UserId = userId
        };

        // Add 將物件標記成待新增狀態，尚未寫入資料庫。
        _dbContext.TodoItems.Add(todoItem);

        // SaveChangesAsync 將新增操作送到 PostgreSQL。
        await _dbContext.SaveChangesAsync();

        return todoItem;
    }

    /// <summary>
    /// 更新指定待辦事項。
    /// </summary>
    /// <param name="userId">目前登入會員的 Id。</param>
    /// <param name="id">要更新的待辦事項 Id。</param>
    /// <param name="title">更新後的文字內容。</param>
    /// <param name="isCompleted">更新後的完成狀態。</param>
    /// <returns>更新後的待辦事項；找不到時回傳 null。</returns>
    public async Task<TodoItem?> UpdateAsync(int userId, int id, string title, bool isCompleted)
    {
        // 同時比對 Id 與 UserId，避免會員修改其他人的 Todo。
        TodoItem? todoItem = await _dbContext.TodoItems
            .SingleOrDefaultAsync(todoItem => todoItem.Id == id && todoItem.UserId == userId);

        if (todoItem is null)
        {
            return null;
        }

        todoItem.Title = title.Trim();
        todoItem.IsCompleted = isCompleted;

        // EF Core 已追蹤 todoItem，SaveChangesAsync 會產生並執行 UPDATE SQL。
        await _dbContext.SaveChangesAsync();

        return todoItem;
    }

    /// <summary>
    /// 刪除指定待辦事項。
    /// </summary>
    /// <param name="userId">目前登入會員的 Id。</param>
    /// <param name="id">要刪除的待辦事項 Id。</param>
    /// <returns>刪除成功回傳 true；找不到資料回傳 false。</returns>
    public async Task<bool> DeleteAsync(int userId, int id)
    {
        // 同時比對 Id 與 UserId，避免會員刪除其他人的 Todo。
        TodoItem? todoItem = await _dbContext.TodoItems
            .SingleOrDefaultAsync(todoItem => todoItem.Id == id && todoItem.UserId == userId);

        if (todoItem is null)
        {
            return false;
        }

        // Remove 將物件標記成待刪除狀態。
        _dbContext.TodoItems.Remove(todoItem);

        // SaveChangesAsync 會產生並執行 DELETE SQL。
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
