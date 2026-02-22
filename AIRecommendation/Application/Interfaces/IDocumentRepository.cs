using AIRecommendation.Domain.Entities;

namespace AIRecommendation.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(DocumentRecord doc);
    Task<int> SaveChangesAsync();
    Task<List<DocumentRecord>> GetByCategoryAsync(string category);
}