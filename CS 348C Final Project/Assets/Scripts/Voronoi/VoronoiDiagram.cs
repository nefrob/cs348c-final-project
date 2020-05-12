/*
* File:        Voronoi Diagram
* Author:      Robert Neff
* Date:        11/06/17
* Description: Implements the Voronoi diagram that tracks all half-edges, sites and cells.
*              Each half-edge has a reference to site entry, start/end vertices of half-edge.
*              Each site has references to half-edges.
*              Each cell has references to half-edges and site.
*/

using System.Collections.Generic;

namespace Fracture {
    public class VoronoiDiagram {
        // Public variables
        public List<Edge> edges;
        public List<Site> sites;
        public List<VoronoiCell> cells;

        /* 
         * Constructor 
         */
        public VoronoiDiagram() {
            edges = new List<Edge>();
            sites = new List<Site>();
            cells = new List<VoronoiCell>();
        }

        /* Basic overload. */
        public static implicit operator bool(VoronoiDiagram g) {
            return g != null;
        }
    }
}