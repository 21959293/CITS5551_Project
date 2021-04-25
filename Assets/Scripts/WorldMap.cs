using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;
using Mapbox;
using IniParser;
using IniParser.Model;

namespace Mapbox.Unity.Map
{
    using System;
    using Utils;

    /// <summary>
    /// An extended version of the AbstractMap provided in the MapBox package
    /// </summary>
    public class WorldMap : AbstractMap
    {
        private float centerLat; // Holds the centre-point of the map's Latitude
        private float centerLon; // Holds the centre-point of the map's Longitude
        ConfigClass configObject; // Object holding config information about the game

        /// <summary>
        /// Converts latitude and longitude to in-game (X, Y) coordinates
        /// </summary>
        /// <param name="lat">Double representing latitude of a point</param>
        /// <param name="lon">Double representing longitude of a point</param>
        /// <returns>Vector2d representing the X, Y coordinates of a point</returns>
        public Vector2d toXY(double lat, double lon)
        {
            Vector2d center = Mapbox.Unity.Utilities.Conversions.LatLonToMeters(centerLat, centerLon);
            Vector2d point = Mapbox.Unity.Utilities.Conversions.LatLonToMeters(lat, lon);
            return (point - center) * Math.Cos(degToRad(Math.Abs(lat)));
        }

        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        /// <param name="deg">Double representing degrees</param>
        /// <returns>Double representing radians</returns>
        private double degToRad(double deg)
        {
            return deg * Math.PI / 180;
        }

        /// <summary>
        /// Called before the application starts up
        /// </summary>
        protected override void Awake()
        {
            // Grab config data
            string configFilepath;
            if (UnityEngine.Debug.isDebugBuild)
            {
                configFilepath = "Assets/Resources/config.ini";
            }
            else
            {
                configFilepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/config.ini";
                if(!System.IO.File.Exists(configFilepath)) // Note: This is because unity complains when you try to build and it can't find the file in program files (cry more unity)
                {
                    configFilepath = "Assets/Resources/config.ini";
                }
            }
            var parser = new FileIniDataParser();
            IniData configData = parser.ReadFile(configFilepath);

            // Load in the config file
            string filepath;
            if (UnityEngine.Debug.isDebugBuild)
            {
                filepath = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/output.json";
            }
            else
            {
                filepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/output.json";
                if (!System.IO.File.Exists(filepath))// Note: This is also because unity complains
                {
                    filepath = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/output.json";
                }
            }
            StreamReader reader = new StreamReader(filepath);
            configObject = ConfigClass.CreateFromJSON(reader.ReadToEnd());

            // Update options according to the config file
            Options.locationOptions.zoom = (float)configObject.zoom;
            centerLat = (float)configObject.latitude;
            centerLon = (float)configObject.longitude;
            Options.locationOptions.latitudeLongitude = configObject.latitude.ToString() + "," + configObject.longitude.ToString();
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.west = (int)configObject.extent;
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.east = (int)configObject.extent;
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.north = (int)configObject.extent;
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.south = (int)configObject.extent;
            Options.scalingOptions.scalingType = MapScalingType.WorldScale; // @todo: Should add to config file? Or just calculate based off Zoom/Extent?

            if (_previewOptions.isPreviewEnabled == true)
            {
                DisableEditorPreview();
                _previewOptions.isPreviewEnabled = false;
            }

            MapOnAwakeRoutine();
        }

        protected override void Start()
        {
            // Initialize the map (This used to do more but it's bee mostly automated)
            MapOnStartRoutine();

            // TEST
            //Vector2d MidPoint = new Vector2d(-31.9577836f, 115.8600712f);

            // TEST LATLONTOTILEID (only works for a single tile)
            //Vector2d loc = new Vector2d(-31.9577836f, 115.8600712f);
            //Mapbox.Map.UnwrappedTileId id = Mapbox.Unity.Utilities.Conversions.LatitudeLongitudeToTileId(-31.9527, 115.8605, (int)configObject.Zoom);
            //UnityEngine.Debug.Log("id : " + id);

            //Vector2 xy = Mapbox.Unity.Utilities.Conversions.LatitudeLongitudeToUnityTilePosition(loc,16,0.83f, 4096*3);

            // UNCOMMENT THE FOLLOWING FOR REALIGNING OFFSET
            //Vector2d center = Mapbox.Unity.Utilities.Conversions.LatLonToMeters(40.69061f, -73.945242f); //40.69061, -73.945242
            //Vector2d point = Mapbox.Unity.Utilities.Conversions.LatLonToMeters(40.6937717f, -73.9349599f);
            //UnityEngine.Debug.Log("diff: " + (point-center));

            //Vector2d check = toXY(-31.9590154f, 115.8566012f, MidPoint);
            //UnityEngine.Debug.Log("diff: " + check);



            //Vector2d xy = Mapbox.Unity.Utilities.Conversions.GeoToWorldPosition(-31.9527, 115.8605, loc, 1); //use centre point of the map
            //UnityEngine.Debug.Log("xy: " + xy);

        }

        // TODO: implement IDisposable, instead?
        protected override void OnDestroy()
        {
            
        }

    }

    class ConfigClass
    {
        public string name;
        public string output_dest;
        public double latitude;
        public double longitude;
        public double extent;
        public double zoom;
        public double default_speed;
        public bool allow_one_way;

        public static ConfigClass CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<ConfigClass>(jsonString);
        }
    }
}