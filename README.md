# traffic-sign-processing

A repository with some code to try and use traffic signs to improve OSM.

In short, this piece of code does:

1. Load the traffic signs.
2. Matches them to the closest road.
3. Checks if the sign is on the left or right.
4. Mark the segment until the next intersection.

The project uses [Itinero](https://github.com/itinero/routing) and needs a routing database from Belgium, this can be downloaded here:

http://files.itinero.tech/data/itinero/routerdbs/planet/europe/belgium.c.cf.routerdb

The result can be seen in the 'result' folder, for now this is a collection of GeoJSON files.

**DISLCAIMER: This is not even remotely finished!**