﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioLink.Client.Tools {

    public class ModelPoint {

        public double[] LayerValues;

        public ModelPoint(int layerCount) {
            UsePoint = true;
            LayerValues = new double[layerCount];
        }

        public double X { get; set; }
        public double Y { get; set; }
        public int CellX { get; set; }
        public int CellY { get; set; }
        public bool UsePoint { get; set; }

        public void SetValueForLayer(int index, double value) {
            LayerValues[index] = value;
        }

        public double GetValueForLayer(int index) {
            return LayerValues[index];
        }
    }

    public class ModelPointSet {

        private List<ModelPoint> _points = new List<ModelPoint>();

        public void AddPoint(ModelPoint p) {
            _points.Add(p);
        }

        public IEnumerable<ModelPoint> Points {
            get { return _points; }
        }

        public bool ContainsCell(int x, int y) {
            return _points.FirstOrDefault((p) => { return (p.CellX == x) && (p.CellY == y); }) != null;
        }

    }
}
