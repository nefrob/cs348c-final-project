/*
* File:        Edge
* Author:      Robert Neff
* Date:        11/06/17
* Description: Implements a Voronoi edge between two sites. 
*/

namespace Fracture {
    public class Edge {
        // Public variables
        public Site s1;
        public Site s2;
        public Vertex v1;
        public Vertex v2;

        /* 
        * Constructor 
        */
        public Edge(Site s1 = null, Site s2 = null) {
            this.s1 = s1;
            this.s2 = s2;
            v1 = v2 = null;
        }

        /* Method: setStartVertex
         * ----------------------
         * Sets the sites if nothing initialized and starting vertex.
         */
        public void setStartVertex(Site s1, Site s2, Vertex v) {
            if (!v1 && !v2) {
                v1 = v;
                this.s1 = s1;
                this.s2 = s2;
            } else if (this.s1 == s2) v2 = v;
            else v1 = v;
        }

        /* Method: setEndVertex
         * --------------------
         * Sets opposite of start vertex (flip).
         */
        public void setEndVertex(Site s1, Site s2, Vertex v) {
            setStartVertex(s2, s1, v);
        }

        /* Basic overload. */
        public static implicit operator bool(Edge e) {
            return e != null;
        }
    }
}