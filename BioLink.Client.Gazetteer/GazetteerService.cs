﻿/*******************************************************************************
 * Copyright (C) 2011 Atlas of Living Australia
 * All Rights Reserved.
 * 
 * The contents of this file are subject to the Mozilla Public
 * License Version 1.1 (the "License"); you may not use this file
 * except in compliance with the License. You may obtain a copy of
 * the License at http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS
 * IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
 * implied. See the License for the specific language governing
 * rights and limitations under the License.
 ******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;
using BioLink.Data;
using BioLink.Data.Model;
using BioLink.Client.Utilities;
using BioLink.Client.Extensibility;

namespace BioLink.Client.Gazetteer {

    public class GazetteerService : SQLiteServiceBase, IDisposable {


        public GazetteerService(string file, bool create = false) : base(file, create) {
            
            if (create) {
                CreateNewGazFile(file);
            } else {
                if (File.Exists(file) && !create) {
                    SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0}", file));
                    ValidateGazFile(connection);
                } else {                
                    throw new Exception("Gazetteer file not found!");
                }                
            } 
        }

        private void CreateNewGazFile(string filename) {

            if (!File.Exists(filename)) {
                SQLiteConnection.CreateFile(filename);
            }

            try {

                ExecuteNonQuery("DROP TABLE IF EXISTS tblSettings");
                ExecuteNonQuery("DROP TABLE IF EXISTS tblDivisions");
                ExecuteNonQuery("DROP TABLE IF EXISTS tblGaz");

                ExecuteNonQuery("DROP INDEX IF EXISTS tblDivisions_PrimaryKey");
                ExecuteNonQuery("DROP INDEX IF EXISTS tblGaz_Place");
                ExecuteNonQuery("DROP INDEX IF EXISTS tblGaz_State");
                ExecuteNonQuery("DROP INDEX IF EXISTS tblSettings_PrimaryKey");
                ExecuteNonQuery("DROP INDEX IF EXISTS tblSettings_SettingKey");

                // Create Tables...
                ExecuteNonQuery("CREATE TABLE 'tblSettings' ('SettingKey' TEXT, 'SettingValue' TEXT, 'LongData' TEXT, 'UseLongData' INTEGER)");
                ExecuteNonQuery("CREATE TABLE 'tblDivisions' ('tDatabase' TEXT, 'tAbbreviation' TEXT, 'tFull' TEXT)");
                ExecuteNonQuery("CREATE TABLE 'tblGaz' ('tPlace' TEXT, 'tType' TEXT, 'tDivision' TEXT, 'tLatitude' TEXT, 'tLongitude' TEXT, 'dblLatitude' DOUBLE, 'dblLongitude' DOUBLE)");

                // Create indexes
                ExecuteNonQuery("CREATE UNIQUE INDEX 'tblDivisions_PrimaryKey' ON 'tblDivisions' ('tDatabase' )");
                ExecuteNonQuery("CREATE INDEX 'tblGaz_Place' ON 'tblGaz' ('tPlace' )");
                ExecuteNonQuery("CREATE INDEX 'tblGaz_State' ON 'tblGaz' ('tDivision' , 'tPlace' )");
                ExecuteNonQuery("CREATE UNIQUE INDEX 'tblSettings_PrimaryKey' ON 'tblSettings' ('SettingKey' )");
                ExecuteNonQuery("CREATE INDEX 'tblSettings_SettingKey' ON 'tblSettings' ('SettingKey' )");

            } catch (Exception ex) {
                // Clean up if we fail...
                Disconnect();                
                if (File.Exists(FileName)) {
                    File.Delete(FileName);
                }
                throw ex;
            }

        }

        public void SetSetting(string key, string value, bool useLongData = false) {
            if (!useLongData) {
                ExecuteNonQuery("REPLACE INTO tblSettings (SettingKey, SettingValue, LongData, UseLongData) VALUES (@key, @value, null, 0)", _P("@key", key), _P("@value", value));
            } else {
                ExecuteNonQuery("REPLACE INTO tblSettings (SettingKey, SettingValue, LongData, UseLongData) VALUES (@key, null, @value, 1)", _P("@key", key), _P("@value", value));
            }
        }

        public List<PlaceName> FindPlaceNames(string find, int maxrows = 1000) {
            List<PlaceName> list = new List<PlaceName>();
            try {
                string sql = "SELECT tPlace as Name, tType as PlaceType, tDivision as Division, tLatitude as LatitudeString, tLongitude as LongitudeString, dblLatitude as Latitude, dblLongitude as Longitude FROM tblGaz WHERE tPlace like @find ORDER BY tDivision, tPlace, tType LIMIT @limit";
                SelectReader(sql, (reader) => {
                    PlaceName place = new PlaceName();
                    MapperBase.ReflectMap(place, reader, null, null);
                    list.Add(place);
                }, new SQLiteParameter("@find", find + "%"), new SQLiteParameter("@limit", maxrows));
            } catch (Exception ex) {
                GlobalExceptionHandler.Handle(ex);
            }

            return list;
        }

        public List<PlaceName> FindPlaceNamesLimited(string find, string limitToDivision, int maxrows = 1000) {
            List<PlaceName> list = new List<PlaceName>();
            try {
                string sql = "SELECT tPlace as Name, tType as PlaceType, tDivision as Division, tLatitude as LatitudeString, tLongitude as LongitudeString, dblLatitude as Latitude, dblLongitude as Longitude FROM tblGaz WHERE tPlace like @find AND tDivision = @division ORDER BY tDivision, tPlace, tType LIMIT @limit";
                SelectReader(sql, (reader) => {
                    PlaceName place = new PlaceName();
                    MapperBase.ReflectMap(place, reader, null, null);
                    list.Add(place);
                }, new SQLiteParameter("@find", find + "%"), new SQLiteParameter("@limit", maxrows), new SQLiteParameter("@division", limitToDivision));
            } catch (Exception ex) {
                GlobalExceptionHandler.Handle(ex);
            }


            return list;
        }

        public List<CodeLabelPair> GetDivisions() {
            List<CodeLabelPair> results = new List<CodeLabelPair>();
            try {                
                SelectReader("SELECT tDatabase, tAbbreviation FROM tblDivisions", (reader) => {
                    string code = reader["tDatabase"] as string;
                    string abbrev = reader["tAbbreviation"] as string;
                    if (String.IsNullOrEmpty(abbrev)) {
                        abbrev = code;
                    }
                    results.Add(new CodeLabelPair(code, abbrev));
                });
            } catch (Exception ex) {
                GlobalExceptionHandler.Handle(ex);
            }

            return results;
        }

        private bool ValidateGazFile(SQLiteConnection connection) {
            // TODO: check for the necessary tables and version info...
            return true;
        }


        public void AddDivision(string databaseName, string abbrev, string full) {
            ExecuteNonQuery("INSERT INTO tblDivisions (tDatabase, tAbbreviation, tFull) VALUES (@database, @abbrev, @full)", _P("@database", databaseName), _P("@abbrev", abbrev), _P("@full", full));
        }

        public void AddPlaceName(string place, string type, string division, string latstring, string lonstring, double lat, double lon) {
            ExecuteNonQuery("INSERT INTO tblGaz (tPlace, tType, tDivision, tLatitude, tLongitude, dblLatitude, dblLongitude) VALUES (@place, @type, @division, @latstring, @lonstring, @lat, @lon)", 
                _P("@place", place), _P("@type", type), _P("@division", division), _P("@latstring", latstring), _P("@lonstring", lonstring), _P("@lat", lat), _P("@lon", lon));
        }

        public GazetteerInfo GetGazetteerInfo() {
            var result = new GazetteerInfo { DatabaseName = GetSetting("DatabaseName"), DatabaseVersion = GetSetting("DatabaseVersion"), Description = GetSetting("Description") };

            // Now count the records...            
            SelectReader("SELECT count(*) from tblGaz", (reader) => {
                result.RecordCount = (int) (long) reader[0];
            });

            return result;                
        }

        public String GetSetting(string key, string @default = "") {
            string result = @default;
            this.SelectReader("SELECT SettingValue, LongData, UseLongData FROM tblSettings WHERE SettingKey = @key", (reader) => {                

                var useLong = (long) reader[2];
                if (useLong != 0) {
                    result = reader[1] as string;
                } else {
                    result = reader[0] as string;
                }

            }, _P("@key", key));

            return result;
        }

        public int CountPlacesInBoundedBox(double x1, double y1, double x2, double y2, string placeType) {
            int count = 0;
            try {
                string sql = "SELECT Count(*) FROM tblGaz WHERE (dblLatitude BETWEEN @y1 AND @y2) AND (dblLongitude BETWEEN @x1 AND @x2)";
                if (!string.IsNullOrEmpty(placeType)) {
                    sql += " and tType = @div";
                }
                SelectReader(sql, (reader) => {
                    count = reader.GetInt32(0);
                }, new SQLiteParameter("@y1", y1), new SQLiteParameter("@y2", y2), new SQLiteParameter("@x1", x1), new SQLiteParameter("@x2", x2), new SQLiteParameter("@div", placeType));
            } catch (Exception ex) {
                GlobalExceptionHandler.Handle(ex);
            }

            return count;
        }

        public List<PlaceName> GetPlacesInBoundedBox(double x1, double y1, double x2, double y2, string placeType) {
            var list = new List<PlaceName>();
            try {
                string sql = "SELECT tPlace as Name, tType as PlaceType, tDivision as Division, tLatitude as LatitudeString, tLongitude as LongitudeString, dblLatitude as Latitude, dblLongitude as Longitude FROM tblGaz WHERE (dblLatitude BETWEEN @y1 AND @y2) AND (dblLongitude BETWEEN @x1 AND @x2)";
                if (!string.IsNullOrEmpty(placeType)) {
                    sql += " and tType = @div";
                }
                SelectReader(sql, (reader) => {
                    PlaceName place = new PlaceName();
                    MapperBase.ReflectMap(place, reader, null, null);
                    list.Add(place);                    
                }, new SQLiteParameter("@y1", y1), new SQLiteParameter("@y2", y2), new SQLiteParameter("@x1", x1), new SQLiteParameter("@x2", x2), new SQLiteParameter("@div", placeType));
            } catch (Exception ex) {
                GlobalExceptionHandler.Handle(ex);
            }

            return list;
        }

        public List<string> GetPlaceTypes() {
            
            var list = new List<string>();
            try {
                var sql = "SELECT DISTINCT tType FROM tblGaz";
                SelectReader(sql, (reader) => {
                    list.Add(reader[0] as string);
                });
            } catch (Exception ex) {
                GlobalExceptionHandler.Handle(ex);
            }

            return list;

        }

    }

    public class GazetteerInfo {

        public string DatabaseName { get; set; }
        public string DatabaseVersion { get; set; }
        public int RecordCount { get; set; }
        public string Description { get; set; }

    }

}
