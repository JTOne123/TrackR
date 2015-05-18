﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Omu.ValueInjecter;
using TrackR.Common.DeepCloning;
using TrackR.Common.DeepCloning.SmartConvention;

namespace TrackR.Common
{
    /// <summary>
    /// All extensions in one place.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Performs string.Format() on a string object.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string FormatStatic(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        /// <summary>
        /// Performs string.IsNullOrWhitespace() on a string object.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(this string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }

        /// <summary>
        /// Converts an object into a query parameter compatible format (e.g. datetime yyyy-MM-dd)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string ToUriParameter(this object value, string key)
        {
            const string format = "{0}={1}";
            if (value == null)
            {
                return format.FormatStatic(key, "NULL");
            }

            if (value is DateTime)
            {
                return format.FormatStatic(key, ((DateTime)value).ToString("yyyy-MM-dd"));
            }
            var s = value as string;
            if (s != null)
            {
                return format.FormatStatic(key, Uri.EscapeDataString(s));
            }

            var array = value as Array;
            if (array != null)
            {
                var parameters = array
                    .Cast<object>()
                    .Select(o => o.ToUriParameter(key));
                Debugger.Log(1, "TrackR", string.Join("&", parameters));
                return string.Join("&", parameters);
            }

            return format.FormatStatic(key, value.ToString());
        }

        /// <summary>
        /// Used deep injection to inject data into arbitrary objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T DeepInject<T>(this object value) where T : new()
        {
            return (T)new T().InjectFrom<DeepCloneInjection>(value);
        }

        /// <summary>
        /// Used deep injection to inject data into arbitrary objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static T DeepInject<T>(this object value, T destination)
        {
            return (T)destination.InjectFrom<DeepCloneInjection>(value);
        }

        /// <summary>
        /// Used deep injection to inject data into arbitrary objects.
        /// </summary>
        /// <typeparam name="T">Class to transform the enumerable into. Needs a parameterless constructor.</typeparam>
        /// <param name="source">Source</param>
        /// <returns></returns>
        public static IEnumerable<T> DeepInject<T>(this IEnumerable source)
        {
            return source
                .Cast<object>()
                .Select(o => (T)(CreateNewObject<T>(o).InjectFrom<DeepCloneInjection>(o)))
                .ToList();
        }

        private static object CreateNewObject<T>(object o)
        {
            try
            {
                var targetType = typeof(T);
                var targetAssembly = targetType.Assembly;

                var sourceType = o.GetType();

                var matchingTypes = targetAssembly.GetTypes()
                    .Where(x => x.Name == sourceType.Name)
                    .ToList();

                while (true)
                {
                    // No match
                    if (!matchingTypes.Any())
                    {
                        return typeof (T).GetConstructors().Single(x => !x.GetParameters().Any()).Invoke(null);
                    }

                    // More than 1
                    if (matchingTypes.Count > 1)
                    {
                        matchingTypes = matchingTypes.Where(x => x.Namespace == targetType.Namespace).ToList();
                    }

                    // Exactly 1
                    var ctor = matchingTypes.Single().GetConstructors().SingleOrDefault(x => !x.GetParameters().Any());
                    if (ctor != null)
                    {
                        return ctor.Invoke(null);
                    }
                    return typeof (T).GetConstructors().Single(x => !x.GetParameters().Any()).Invoke(null);;
                }
            }
            catch (Exception err)
            {
                throw new TypeInitializationException(string.Format("Could not create type {0}.", typeof(T).Name), err);
            }
        }

        /// <summary>
        /// Replaces the First() and FirstOrDefault() for context queries.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IQueryable<T> FirstQuery<T>(this IQueryable<T> source)
        {
            return source.Take(1);
        }

    }
}
