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
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;
using BioLink.Client.Utilities;
using BioLink.Data.Model;

namespace BioLink.Data {

    public abstract class MapperBase {

        private static Regex PREFIX_REGEX = new Regex(@"^([a-z]+)[A-Za-z\d]+$");

        public static string KNOWN_TYPE_PREFIXES = "chr,vchr,bit,int,txt,flt,tint,dt,sint,img";

        public static void ReflectMap(object dest, DbDataReader reader, HashSet<string> columnIgnores, ColumnMapping[] columnMappings, params ConvertingMapper[] columnOverrides) {
            PropertyInfo[] props = dest.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Dictionary<string, PropertyInfo> propMap = new Dictionary<string, PropertyInfo>();

            if (columnMappings != null && columnMappings.Length > 0) {
                foreach (ColumnMapping mapping in columnMappings) {
                    // find the property by the name...
                    PropertyInfo pinfo = Array.Find(props, (prop) => {
                        return prop.Name == mapping.PropertyName;
                    });

                    if (pinfo != null && pinfo.CanWrite) {
                        propMap[mapping.ColumnName] = pinfo;
                    }
                }
            }

            foreach (PropertyInfo propInfo in props) {
                var x = Attribute.GetCustomAttribute(propInfo, typeof(MappingInfo));
                if (x != null) {
                    var mapping = x as MappingInfo;
                    if (!mapping.Ignore) {
                        propMap[mapping.Column] = propInfo;
                    } 
                } else if (propInfo.CanWrite) {
                    propMap[propInfo.Name] = propInfo;
                }

            }

            var overrides = new Dictionary<string, ConvertingMapper>();
            if (columnOverrides != null) {
                foreach (ConvertingMapper mapper in columnOverrides) {
                    overrides.Add(mapper.ColumnName, mapper);
                }
            }

            for (int i = 0; i < reader.FieldCount; ++i) {
                string name = reader.GetName(i);

                PropertyInfo target = null;

                if (propMap.ContainsKey(name)) {
                    target = propMap[name];
                } else {
                    // It may be that the column names have been prefixed with their database type.
                    // Look for common prefixes, and if so, strip the prefix off, and try that
                    Match m = PREFIX_REGEX.Match(name);
                    if (m.Success) {
                        string prefix = m.Groups[1].Value;

                        if (KNOWN_TYPE_PREFIXES.IndexOf(prefix) >= 0) {
                            string shortened = name.Substring(prefix.Length);
                            if (propMap.ContainsKey(shortened)) {
                                target = propMap[shortened];
                            }
                        }
                    }
                }

                if (target != null) {
                    object val = reader[i];
                    if (overrides.ContainsKey(name)) {
                        val = overrides[name].Converter(val);
                    } else {
                        if (val is DBNull) {
                            val = null;
                        } else if (val is string) {
                            val = (val as string).TrimEnd();
                        }
                    }

                    target.SetValue(dest, val, null);
                } else {
                    if (columnIgnores == null || !columnIgnores.Contains(name)) {
                        Logger.Debug("Could not map field '{0}' to object of type {1}", name, dest.GetType().Name);
                    }
                }
            }
        }

        public static ConvertingMapper ToNull(string columnName) {
            return new ConvertingMapper(columnName, (@in) => null);
        }

    }

    public class TaxonMapper : MapperBase {

        public static Taxon MapTaxon(DbDataReader reader, params ConvertingMapper[] overrides) {
            Taxon t = new Taxon();
            ReflectMap(t, reader, null, null, overrides);
            return t;
        }

        public static TaxonSearchResult MapTaxonSearchResult(DbDataReader reader) {
            TaxonSearchResult t = new TaxonSearchResult();
            ReflectMap(t, reader, null, null);
            return t;
        }

        private static ConvertingMapper _taxonMapper = new ConvertingMapper("bitAvailableNameAllowed", (@in) => ((byte?) @in).GetValueOrDefault(0) !=0);

        public static TaxonRank MapTaxonRank(DbDataReader reader) {
            TaxonRank tr = new TaxonRank();
            ReflectMap(tr, reader, null, null, _taxonMapper);
            return tr;
        }

    }

    public class GenericMapper<T> : MapperBase where T : new() {

        public GenericMapper() {
        }

        public GenericMapper(params ConvertingMapper[] overrides) {
            Overrides = overrides;
        }

        public T Map(DbDataReader reader) {
            return Map(reader, new T());
        }

        virtual public T Map(DbDataReader reader, T t ) {

            ReflectMap(t, reader, ColumnIgnores, Mappings, Overrides);
            if (PostMapAction != null) {
                PostMapAction(t);
            }
            return t;
        }

        public ConvertingMapper[] Overrides { get; set; }

        public ColumnMapping[] Mappings { get; set; }

        public Action<T> PostMapAction { get; set; }

        public HashSet<string> ColumnIgnores { get; set; }
    }

    public class GenericMapperBuilder<T> where T : new() {

        private List<ConvertingMapper> _overrides = new List<ConvertingMapper>();
        private List<ColumnMapping> _mappings = new List<ColumnMapping>();
        private Action<T> _postMapAction;
        private HashSet<string> _columnIgnores = new HashSet<string>();

        public GenericMapperBuilder() {
        }

        public GenericMapperBuilder<T> @Override(params ConvertingMapper[] overrides) {
            foreach (ConvertingMapper o in overrides) {
                _overrides.Add(o);
            }            
            return this;            
        }

        public GenericMapperBuilder<T> @Override(string column, Converter<object, object> converter) {
            ConvertingMapper @override = new ConvertingMapper(column, converter);
            _overrides.Add(@override);
            return this;
        }

        public GenericMapperBuilder<T> Map(params ColumnMapping[] mappings) {
            foreach (ColumnMapping mapping in mappings) {
                _mappings.Add(mapping);
            }
            return this;
        }

        public GenericMapperBuilder<T> Map(string columnName, string propertyName) {
            ColumnMapping mapping = new ColumnMapping(columnName, propertyName);
            _mappings.Add(mapping);
            return this;
        }

        public GenericMapperBuilder<T> PostMapAction(Action<T> action) {
            _postMapAction = action;
            return this;
        }

        public GenericMapper<T> build() {
            var mapper = new GenericMapper<T>();
            mapper.Overrides = _overrides.ToArray();
            mapper.Mappings = _mappings.ToArray();
            mapper.PostMapAction = _postMapAction;
            mapper.ColumnIgnores = _columnIgnores;
            return mapper;
        }

        internal GenericMapperBuilder<T> Ignore(string column) {
            _columnIgnores.Add(column);
            return this;
        }
    }

    public class ConvertingMapper {

        public ConvertingMapper(string column, Converter<object, object> converter) {
            this.ColumnName = column;
            this.Converter = converter;
        }

        public string ColumnName { get; private set; }
        public Converter<object, object> Converter { get; set; }
    }

    public class ColumnMapping {

        public ColumnMapping(string column, string propertyname) {
            this.ColumnName = column;
            this.PropertyName = propertyname;
        }

        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
    }

    public class TintToBoolConvertingMapper : ConvertingMapper {

        public TintToBoolConvertingMapper(string columnName)
            : base(columnName, (tintval) => {
                return (byte) tintval != 0;
            }) {
        }
    }

    public class ByteToBoolConvertingMapper : ConvertingMapper {
        public ByteToBoolConvertingMapper(string columnName) : base(columnName, (byteval) => { return (byte)byteval != 0; }) { }
    }

    public class IntToBoolConvertingMapper : ConvertingMapper {
        public IntToBoolConvertingMapper(string columnName) : base(columnName, (intval) => { return (int)intval != 0; }) { }
    }

    public class StringToIntConverteringMapping : ConvertingMapper {
        public StringToIntConverteringMapping(string columnName) : base(columnName, (strval) => { return Int32.Parse((string) strval); }) { }
    }

}
