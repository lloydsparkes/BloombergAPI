namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class represents a single parsed SSE event.
    /// </summary>
    public class SseEvent
    {
        public string Origin { get; }
        public string Type { get; }
        public string Id { get; }
        public string Data { get; }
        public string Comments { get; }
        public int? Retry { get; }
        /// <summary>
        /// Indicates whether or not the event is a BEAP hearbeat.
        /// </summary>
        /// <returns>true for heartbeats, false otherwise.</returns>
        public bool IsHeartbeat { get { return Data == null; } }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="origin">A URI the event came from.</param>
        /// <param name="type">Event type.</param>
        /// <param name="id">Event identifier.</param>
        /// <param name="data">All the event data lines.</param>
        /// <param name="comments">All the event comment lines.</param>
        /// <param name="retry">Reconnection timeout value, if any, or null.</param>
        public SseEvent(string origin, string type, string id, string data, string comments, int? retry)
        {
            Origin = origin;
            Type = type;
            Id = id;
            Data = data;
            Comments = comments;
            Retry = retry;
        }

    }
}