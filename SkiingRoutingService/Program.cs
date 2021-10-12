using System.Globalization;
using System.Text;
using Itinero;
using Itinero.Exceptions;
using Itinero.LocalGeo;
using Itinero.LocalGeo.IO;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Coordinate = Itinero.LocalGeo.Coordinate;
using Route = Itinero.Route;

var app = WebApplication.CreateBuilder(args).Build();
var skiRountingService = new SkiRountingService(args[0], args[1]);

app.MapGet("/nav", (
    [FromQuery(Name = "from")] string from,
    [FromQuery(Name = "to")] string to
    ) =>
{
 if (!TryParseCoordinate(from, out var fromCord))
 {
  return Results.BadRequest("'from' parameter wrong format, should be '12.124,46.678'");
 }
 if (!TryParseCoordinate(to, out var toCord))
 {
  return Results.BadRequest("'to' parameter wrong format, should be '12.124,46.678'");
 }
 return GetResponse(skiRountingService.GetGpsRoute(fromCord, toCord));
});

app.MapGet("/map", () =>
 {
  return Results.Content(File.ReadAllText("map.html"), "text/html", Encoding.UTF8);
 });

app.Run();


static object GetResponse(Result<Route> result)
{
 if (result.IsError)
 {
  try
  {
   result.Value.ToString();//Dummy to trigger exception
  }
  catch (RouteNotFoundException ex)
  {
   if (string.IsNullOrEmpty(result.ErrorMessage))
    return Results.BadRequest("Route not found.");
   return Results.BadRequest("Route not found: " + result.ErrorMessage);
  }
  catch (Exception)
  {
  }
  return Results.BadRequest(result.ErrorMessage);
 }
 return Results.Text(RouteToJson(result.Value));
}


static bool TryParseCoordinate(string text, out Coordinate cord)
{
 var index = text.IndexOf(",");
 cord = default;
 if (index == -1)
  return false;
 if (!float.TryParse(text.Remove(index), NumberStyles.Number, CultureInfo.InvariantCulture, out var lat))
  return false;
 if (!float.TryParse(text.Substring(index + 1), NumberStyles.Number, CultureInfo.InvariantCulture, out var lon))
  return false;
 cord = new Coordinate(lat, lon);
 return true;
}

static string RouteToJson(Route route)
{
 var featureCollection = new FeatureCollection();
 featureCollection.Add(new Feature(
  new LineString(
   route.Shape.Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude)).ToArray()), new AttributesTable()
 {
    {"time",route.TotalTime.ToString(CultureInfo.InvariantCulture) },
    {"distance",route.TotalDistance.ToString(CultureInfo.InvariantCulture) }
 }));
 var serializer = GeoJsonSerializer.Create();
 using (var stringWriter = new StringWriter())
 using (var jsonWriter = new JsonTextWriter(stringWriter))
 {
  serializer.Serialize(jsonWriter, featureCollection);
  return stringWriter.ToString();
 }
}