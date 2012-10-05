using System;
using System.Collections.Generic;
using System.Linq;

namespace Regalo.Core.EventSourcing
{
    public class StrictConcurrencyMonitor : IConcurrencyMonitor
    {
        public IEnumerable<ConcurrencyConflict> CheckForConflicts(IEnumerable<object> unseenEvents, IEnumerable<object> uncommittedEvents)
        {
            if (unseenEvents == null) throw new ArgumentNullException("unseenEvents");
            if (uncommittedEvents == null) throw new ArgumentNullException("uncommittedEvents");

            if (unseenEvents.Any() && uncommittedEvents.Any())
            {
                return new[] { new ConcurrencyConflict("Changes conflict with one or more committed events.", unseenEvents, uncommittedEvents) };
            }

            return Enumerable.Empty<ConcurrencyConflict>();
        }
    }
}