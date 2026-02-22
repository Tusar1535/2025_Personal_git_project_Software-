using AIRecommendation.Application.Interfaces;
using AIRecommendation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIRecommendation.Infrastructure.Persistence;

public class EfDocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;
    public EfDocumentRepository(AppDbContext db) => _db = db;

    public Task AddAsync(DocumentRecord doc)
    {
        _db.Documents.Add(doc);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();

    public Task<List<DocumentRecord>> GetByCategoryAsync(string category)
        => _db.Documents.Where(d => d.Category == category).ToListAsync();
}