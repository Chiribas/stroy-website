namespace Core.Interfaces;

public interface IHtmlSanitizerService
{
    string Sanitize(string html);
}
