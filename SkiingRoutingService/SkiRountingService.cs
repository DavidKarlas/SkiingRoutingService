using Itinero;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Profiles;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Db;
using OsmSharp.Db.Impl;
using OsmSharp.Streams;
using Route = Itinero.Route;

internal class SkiRountingService
{
    Router router;

    public SkiRountingService(string pbfPath, string routerDbCache)
    {
        RouterDb routerDb;
        if (File.Exists(routerDbCache))
        {
            using var fs = new FileStream(routerDbCache, FileMode.Open);
            routerDb = RouterDb.Deserialize(fs);
        }
        else
        {
            using var fs = new FileStream(pbfPath, FileMode.Open);
            var osmSource = new PBFOsmStreamSource(fs);
            routerDb = new RouterDb();
            using var stream = new FileStream("pedestrian.lua", FileMode.Open);
            routerDb.LoadOsmData(osmSource, DynamicVehicle.LoadFromStream(stream));
            using var cacheWriter = new FileStream(routerDbCache, FileMode.Create);
            routerDb.Serialize(cacheWriter);
        }
        router = new Router(routerDb);
    }


    internal Result<Route> GetGpsRoute(Coordinate from, Coordinate to)
    {
        return router.TryCalculate(router.Db.GetSupportedProfiles().First(), from, to);
    }
}
