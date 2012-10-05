using System.Collections.Generic;

namespace Regalo.Core.EventSourcing
{
    public interface IConcurrencyMonitor
    {
        IEnumerable<ConcurrencyConflict> CheckForConflicts(IEnumerable<object> unseenEvents, IEnumerable<object> uncommittedEvents);
    }
}