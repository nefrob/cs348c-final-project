/*
* File:        Vertex
* Author:      Robert Neff
* Date:        11/06/17
* Description: Implements a vertex, i.e. a point on an edge. 
*/

namespace Fracture {
    public class Vertex {
        // Public variables
        public float x; // position
        public float y;
        public float z;

        /* 
         * Constructor 
         */
        public Vertex(float x, float y, float z = 0) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /* Method: ToString
         * ----------------
         * Overloads the Tostring method so the Vertex can be printed.
         */
        public override string ToString() {
            return string.Concat("(", x, ", ", y, ", ", z, ")");
        }

        /* Method: ToString
         * ----------------
         * Returns a vector version of the vertex.
         */
        public UnityEngine.Vector3 ToVector3() {
            return new UnityEngine.Vector3(x, y, z);
        }

        /* Basic overload. */
        public static implicit operator bool(Vertex v) {
            return v != null;
        }
    }
}