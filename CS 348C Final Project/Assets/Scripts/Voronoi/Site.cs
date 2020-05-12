/*
* File:        Site
* Author:      Robert Neff
* Date:        11/06/17
* Description: Implements a Voronoi site as derived class of Vertex.
*/

using System.Collections.Generic;

namespace Fracture {
    public class Site : Vertex {
        // Public variables
        // Inherited = x, y, z
        public List<HalfEdge> halfEdges;
		public int id;

        /* 
         * Constructor 
         */
        public Site(float x, float y, float z = 0) : base(x, y, z) {
            halfEdges = new List<HalfEdge>();
        }

        /* Method: setId
         * -------------
         * Sets the id of the current Site and returns the site.
         */
        public void setId(int id) {
            this.id = id;
        }

        /* Basic overload. */
        public static implicit operator bool(Site s) {
            return s != null;
        }
    }
}