using System.IO;

namespace RPGGamer_Radio_Desktop.Helpers;

public class StreamFileAbstraction(Stream stream, string name) : TagLib.File.IFileAbstraction
{
    private bool _isDisposed;

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public Stream ReadStream { get; } = stream ?? throw new ArgumentNullException(nameof(stream));

    public Stream WriteStream => throw new NotImplementedException();

    public void CloseStream(Stream stream)
    {
        if (_isDisposed) return;

        if (stream != null)
        {
            try
            {
                // Only dispose if it's a stream that should be closed (optional based on your needs)
                stream.Close(); // This closes the stream for reading, if applicable
            }
            catch (Exception ex)
            {
                // Log or handle the error if needed
                Console.WriteLine($"Error closing stream: {ex.Message}");
            }
        }
        _isDisposed = true;
    }

    // Optionally, implement IDisposable to ensure the stream is closed automatically when the abstraction is disposed
    public void Dispose()
    {
        if (!_isDisposed)
        {
            stream?.Dispose();
            _isDisposed = true;
        }
    }
}