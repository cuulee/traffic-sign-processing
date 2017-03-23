using CsvHelper;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Itinero;
using Itinero.Algorithms;
using Itinero.Graphs.Geometric;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using traffic_sign_processing.Domain;

namespace traffic_sign_processing
{
    class Program
    {
        static void Main(string[] args)
        {
            Itinero.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}-{3}] {1} - {2}", origin, level, message, DateTime.Now.ToString()));
            };

            var trafficSigns = new List<MaxSpeedSign>();
            using (var stream = File.OpenText(@"../data/verkeersbordpt.csv"))
            {
                var csv = new CsvReader(stream, new CsvHelper.Configuration.CsvConfiguration()
                {
                    Delimiter = ";",
                    CultureInfo = System.Globalization.CultureInfo.InvariantCulture
                });
                while (csv.Read())
                {
                    var type = csv.GetField<string>("type");
                    switch(type)
                    {
                        case "C43":
                            var opschrift = int.Parse(csv.GetField<string>("opschrift").Replace("|", string.Empty));
                            var lat = (float)csv.GetField<double>("point_lat");
                            var lon = (float)csv.GetField<double>("point_lng");

                            trafficSigns.Add(new MaxSpeedSign()
                            {
                                Id = csv.GetField<string>("id"),
                                Latitude = lat,
                                Longitude = lon,
                                Speed = opschrift
                            });
                            break;
                    }
                }
            }

            RouterDb routerDb = null;
            using (var stream = File.OpenRead(@"belgium.c.routerdb"))
            {
                routerDb = RouterDb.Deserialize(stream);
            }
            var router = new Router(routerDb);
            var profile = routerDb.GetSupportedProfile("car");

            var coordinates = new Itinero.LocalGeo.Coordinate[trafficSigns.Count];
            for (var i = 0; i < coordinates.Length; i++)
            {
                coordinates[i] = new Itinero.LocalGeo.Coordinate()
                {
                    Latitude = trafficSigns[i].Latitude,
                    Longitude = trafficSigns[i].Longitude
                };
            }
            var massResolver = new Itinero.Algorithms.Search.MassResolvingAlgorithm(router,
                new Itinero.Profiles.IProfileInstance[] { profile }, coordinates);
            massResolver.Run();

            var features = new FeatureCollection();
            for (var i = 0; i < massResolver.RouterPoints.Count; i++)
            {
                var point = massResolver.RouterPoints[i];
                var sign = trafficSigns[i];
                var oi = massResolver.LocationIndexOf(i);
                var coordinate = coordinates[oi];
                var coordinateOnNetwork = point.LocationOnNetwork(routerDb);

                //var shape = new List<GeographicPosition>
                //{
                //    new GeographicPosition(coordinate.Latitude, coordinate.Longitude),
                //    new GeographicPosition(coordinateOnNetwork.Latitude, coordinateOnNetwork.Longitude)
                //}.ToList<IPosition>();

                var signAttributes = new Dictionary<string, object>();
                signAttributes["speed"] = sign.Speed;
                signAttributes["id"] = sign.Id;
                //features.Features.Add(new Feature(new LineString(shape)));
                //features.Features.Add(new Feature(new Point(new GeographicPosition(coordinate.Latitude, coordinate.Longitude)),
                //    signAttributes));

                var edge = routerDb.Network.GeometricGraph.GetEdge(point.EdgeId);
                var dir = point.Direction(routerDb);
                var to = edge.To;
                if (dir == Itinero.Navigation.Directions.RelativeDirectionEnum.Left)
                {
                    to = edge.From;
                }
                var toCoordinate = routerDb.Network.GetVertex(to);
                
                var speedFeatures = new FeatureCollection();
                var speedShape = point.ShapePointsTo(routerDb, to);
                var shape = new List<IPosition>();
                shape.Add(new GeographicPosition(coordinateOnNetwork.Latitude, coordinateOnNetwork.Longitude));
                for (var s = 0; s < speedShape.Count; s++)
                {
                    shape.Add(new GeographicPosition(speedShape[s].Latitude,
                        speedShape[s].Longitude));
                }
                shape.Add(new GeographicPosition(toCoordinate.Latitude, toCoordinate.Longitude));

                speedFeatures.Features.Add(new Feature(new LineString(shape), signAttributes));
                speedFeatures.Features.Add(new Feature(new Point(new GeographicPosition(coordinate.Latitude, coordinate.Longitude)),
                    signAttributes));

                features.Features.AddRange(speedFeatures.Features);
                File.WriteAllText("decoded_" + i + ".geojson", JsonConvert.SerializeObject(speedFeatures, Formatting.Indented));
            }
            File.WriteAllText("decoded_all.geojson", JsonConvert.SerializeObject(features, Formatting.Indented));


        }
    }
}
