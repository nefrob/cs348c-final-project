/*
* File:        Event
* Author:      Robert Neff
* Date:        11/08/17
* Description: Implements a basic site event and comparison function 
*              for sorting priority.
*/

using System;
using System.Collections.Generic;

namespace Fracture {
    public class Event : IComparable<Event> {
        // Public variables
        public Site site;
        public bool isSiteEvent;

        /* 
         * Constructor 
         */
        public Event(Site site = null, bool isSiteEvent = true) {
            this.site = site;
            this.isSiteEvent = isSiteEvent;
        }

        /* Method: sortEventPriority
         * -------------------------
         * IComparer compare method.
         */
        internal static int sortEventPriority(Event e1, Event e2) {
            if (e1 && !e2) return -1;
            if (!e1 && e2) return 1;

            int ret = Comparer<float>.Default.Compare(getYPos(e1), getYPos(e2)); // cmp y
            if (ret == 0) ret = Comparer<float>.Default.Compare(getXPos(e1), getXPos(e2)); // cmp x
            if (ret == 0) { // cmp event type
                if (e1.isSiteEvent && !e2.isSiteEvent) return 1;
                if (!e1.isSiteEvent && e2.isSiteEvent) return -1;
            }
            return ret;
        }

        /* Method: CompareTo
         * -----------------
         * IComparable compare to method.
         */
        public int CompareTo(Event other) {
            return sortEventPriority(this, other);
        }

        /* Function: getYPos
         * -----------------
         * Gets the y position of the event for comparison.
         */
        private static float getYPos(Event e) {
            return (e.isSiteEvent) ? e.site.y : ((CircleEvent) e).y;
        }

        /* Function: getXPos
         * -----------------
         * Gets the x position of the event for comparison.
         */
        private static float getXPos(Event e) {
            return (e.isSiteEvent) ? e.site.x : ((CircleEvent) e).x;
        }

        /* Basic overload. */
        public static implicit operator bool(Event e) {
            return e != null;
        }
    }
}
