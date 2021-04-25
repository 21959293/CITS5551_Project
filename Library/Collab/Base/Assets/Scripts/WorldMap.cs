﻿using System.Collections;
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

    public class WorldMap : AbstractMap
    {
        private float centerLat;
        private float centerLon;
        //public float OFFSET = 0.792f; // Map == New York
        public float OFFSET = 0.855f; // Map == Suburbs
        private string mapDir = "Maps/SignalTest/";  // "Maps/New York/";
        ConfigClass configObject;

        public Vector2d toXY(double lat, double lon){
            if(centerLat == 0 || centerLon == 0)
            {
                centerLat = (float)configObject.Latitude;
                centerLon = (float)configObject.Longitude;
                UnityEngine.Debug.Log("centerLat: " + centerLat);
                UnityEngine.Debug.Log("centerLon: " + centerLon);
            }
            Vector2d center = Mapbox.Unity.Utilities.Conversions.LatLonToMeters(centerLat, centerLon);
            Vector2d point = Mapbox.Unity.Utilities.Conversions.LatLonToMeters(lat, lon);
            //UnityEngine.Debug.Log("center: " + center);
            //UnityEngine.Debug.Log("point: " + point);
            //UnityEngine.Debug.Log("Math.Cos(degToRad(Math.Abs(lat)): " + Math.Cos(degToRad(Math.Abs(lat))));
            return (point - center) * Math.Cos(degToRad(Math.Abs(lat)));
        }

        private double degToRad(double deg)
        {
            return deg * Math.PI / 180;
        }

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
            }
            StreamReader reader = new StreamReader(filepath);
            configObject = ConfigClass.CreateFromJSON(reader.ReadToEnd());

            // Update options according to the config file
            Options.locationOptions.zoom = (float)configObject.Zoom;
            centerLat = (float)configObject.Latitude;
            centerLon = (float)configObject.Longitude;
            UnityEngine.Debug.Log("START centerLat: " + centerLat);
            UnityEngine.Debug.Log("START centerLon: " + centerLon);
            Options.locationOptions.latitudeLongitude = configObject.Latitude.ToString() + "," + configObject.Longitude.ToString();
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.west = (int)configObject.Extent;
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.east = (int)configObject.Extent;
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.north = (int)configObject.Extent;
            Options.extentOptions.defaultExtents.rangeAroundCenterOptions.south = (int)configObject.Extent;
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
            // Initialize the map
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

    }

    class ConfigClass
    {
        public string Name;
        public string OutputDest;
        public double Latitude;
        public double Longitude;
        public double Extent;
        public double Zoom;
        public double DefaultSpeed;
        public bool AllowOneWay;

        public static ConfigClass CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<ConfigClass>(jsonString);
        }
    }
}