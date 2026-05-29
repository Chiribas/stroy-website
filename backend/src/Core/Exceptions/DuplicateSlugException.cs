namespace Core.Exceptions;

public class DuplicateSlugException : Exception
{
    public string Slug { get; }
    public DuplicateSlugException(string slug)
        : base($"An article with slug '{slug}' already exists.") => Slug = slug;
}
