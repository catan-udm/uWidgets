using System.Threading;
using System.Threading.Tasks;

namespace Media.Services;

public interface ILyricsService
{
    Task<string?> GetLyricsAsync(string title, string artist, double durationSeconds, CancellationToken cancellationToken = default);
}
