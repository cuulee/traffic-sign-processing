using Itinero;
using Itinero.Graphs.Geometric;
using Itinero.LocalGeo;
using Itinero.Navigation.Directions;

namespace traffic_sign_processing
{
    public static class RouterPointExtensions
    {
        /// <summary>
        /// Returns the location on the network.
        /// </summary>
        public static Coordinate LocationOnNetwork(this RouterPoint point, RouterDb db, out Line segment)
        {
            var geometricEdge = db.Network.GeometricGraph.GetEdge(point.EdgeId);
            var shape = db.Network.GeometricGraph.GetShape(geometricEdge);
            var length = db.Network.GeometricGraph.Length(geometricEdge);
            var currentLength = 0.0;
            var targetLength = length * (point.Offset / (double)ushort.MaxValue);
            for (var i = 1; i < shape.Count; i++)
            {
                var segmentLength = Coordinate.DistanceEstimateInMeter(shape[i - 1], shape[i]);
                if (segmentLength + currentLength > targetLength)
                {
                    var segmentOffsetLength = segmentLength + currentLength - targetLength;
                    var segmentOffset = 1 - (segmentOffsetLength / segmentLength);
                    segment = new Line(shape[i - 1], shape[i]);
                    return new Coordinate()
                    {
                        Latitude = (float)(shape[i - 1].Latitude + (segmentOffset * (shape[i].Latitude - shape[i - 1].Latitude))),
                        Longitude = (float)(shape[i - 1].Longitude + (segmentOffset * (shape[i].Longitude - shape[i - 1].Longitude)))
                    };
                }
                currentLength += segmentLength;
            }
            segment = new Line(shape[shape.Count - 2], shape[shape.Count - 1]);
            return shape[shape.Count - 1];
        }

        /// <summary>
        /// Returns the position of the lat/lon in the routerpoint relative to the edge.
        /// </summary>
        /// <returns>Returns left or right.</returns>
        public static RelativeDirectionEnum Direction(this RouterPoint routerPoint, RouterDb routerDb)
        {
            Line segment;
            var pointOnNetwork = routerPoint.LocationOnNetwork(routerDb, out segment);

            var direction = Itinero.Navigation.Directions.DirectionCalculator.Calculate(
                segment.Coordinate1, pointOnNetwork, routerPoint.Location());
            switch (direction.Direction)
            {
                case RelativeDirectionEnum.Left:
                case RelativeDirectionEnum.SharpLeft:
                case RelativeDirectionEnum.SlightlyLeft:
                    return RelativeDirectionEnum.Left;
                case RelativeDirectionEnum.Right:
                case RelativeDirectionEnum.SharpRight:
                case RelativeDirectionEnum.SlightlyRight:
                    return RelativeDirectionEnum.Right;
            }

            Itinero.Logging.Logger.Log("Extensions", Itinero.Logging.TraceEventType.Warning,
                "Cannot determine left or right for routerpoint {1}: {0} was found, taking Left as default.",
                    direction.Direction, routerPoint.ToInvariantString());
            return RelativeDirectionEnum.Left;
        }
    }
}