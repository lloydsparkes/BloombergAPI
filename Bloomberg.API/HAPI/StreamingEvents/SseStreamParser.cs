using System;
using System.IO;
using System.Threading.Tasks;
using Bloomberg.API.HAPI.Model;

namespace Bloomberg.API.HAPI.StreamingEvents
{
    /// <summary>
    /// This class is a SSE event stream parser.
    /// </summary>
    public class SseStreamParser : IDisposable
    {
        private readonly StreamReader dData;
        private readonly string dOrigin;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="data">The stream to be read.</param>
        /// <param name="origin">A URI the stream came from.</param>
        public SseStreamParser(StreamReader data, string origin)
        {
            dData = data;
            dOrigin = origin;
        }

        /// <summary>
        /// Reads an event from the event stream.
        /// </summary>
        /// <returns>The next event.</returns>
        public async Task<SseEvent> ReadEvent()
        {
            var builder = new SseEventBuilder(dOrigin);
            while (true)
            {
                var line = await dData.ReadLineAsync();
                if (line == null)
                {
                    // Discard the event being constructed if the stream ends before the final new line.
                    throw new EndOfStreamException("event stream is over");
                }

                if (line == string.Empty)
                {
                    // Dispatch the event if a blank line is encountered.
                    return builder.MakeEvent();
                }

                // Keep collecting the event lines.
                builder.AddLine(line);
            }
        }

        /// <summary>
        /// Closes the underlying stream.
        /// </summary>
        public void Dispose()
        {
            dData.Dispose();
        }
    }
}