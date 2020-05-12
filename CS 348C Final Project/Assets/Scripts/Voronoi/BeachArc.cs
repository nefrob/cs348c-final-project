/*
* File:        Beach Arc
* Author:      Robert Neff
* Date:        11/08/17
* Description: Implements a beach arc created when the sweep line encounters
*              a new site point.
*/

namespace Fracture {
    public class BeachArc : RBNodeBase<BeachArc> {
        // Public variables
        public Site site;
        public CircleEvent circleEvent; // potential circle event
        public Edge edge;

        /* 
         * Constructor 
         */
        public BeachArc(Site site) {
            this.site = site;
        }

        /* Method: reset
         * -------------
         * Resets values for reuse.
         */
        public void reset() {
            site = null;
            edge = null;
            circleEvent = null;
        }
    }
}