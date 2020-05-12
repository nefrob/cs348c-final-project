/*
* File:        Circle Event
* Author:      Robert Neff
* Date:        11/10/17
* Description: Implements a circle event. A circle event occurs whem an empty circle, 
*              i.e. one that contains no sites, touches 3+ sites with its border and 
*              causing the removal of an arc (lowest site). Can be a false alarm.
*/

namespace Fracture {
    public class CircleEvent : Event {
        // Public variables
        // Inherited = site, isSiteEvent
        public BeachArc arc;
        public float x; // site position on sweep line, i.e. bottom of circle
        public float y; 
        public float yCircleCenter; // y-coord of circle, can become cell vertex

        /* 
         * Constructor 
         */
        public CircleEvent() : base(null, false)  {
            arc = null;
            x = y = yCircleCenter = 0;
        }

        /* Method: setPosition
         * -------------------
         * Sets position of circle event.
         */
        public void setPosition(float x, float y, float yCenter) {
            this.x = x;
            this.y = y;
            yCircleCenter = yCenter;
        }
    }
}