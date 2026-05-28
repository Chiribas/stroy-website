using Core.DTOs;

namespace Core.Interfaces;

public interface IArticleService
{
    Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(int page, int pageSize);
    Task<ArticleDto?> GetBySlugAsync(string slug);
    Task<ArticleDto> CreateAsync(CreateArticleDto dto);
    Task<ArticleDto?> UpdateAsync(int id, UpdateArticleDto dto);
    Task<bool> DeleteAsync(int id);
}
