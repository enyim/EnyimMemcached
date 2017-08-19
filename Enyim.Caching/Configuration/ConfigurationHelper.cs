using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

namespace Enyim.Caching.Configuration
{
    public static class ConfigurationHelper
    {
        internal static bool TryGetAndRemove(Dictionary<string, string> dict, string name, out int value, bool required)
        {
            string tmp;
            if (TryGetAndRemove(dict, name, out tmp, required)
                && Int32.TryParse(tmp, out value))
            {
                return true;
            }

            if (required)
                throw new Exception("Missing or invalid parameter: " + (String.IsNullOrEmpty(name) ? "element content" : name));

            value = 0;

            return false;
        }

        internal static bool TryGetAndRemove(Dictionary<string, string> dict, string name, out TimeSpan value, bool required)
        {
            string tmp;
            if (TryGetAndRemove(dict, name, out tmp, required)
                && TimeSpan.TryParse(tmp, out value))
            {
                return true;
            }

            if (required)
                throw new Exception("Missing or invalid parameter: " + (String.IsNullOrEmpty(name) ? "element content" : name));

            value = TimeSpan.Zero;

            return false;
        }

        internal static bool TryGetAndRemove(Dictionary<string, string> dict, string name, out string value, bool required)
        {
            if (dict.TryGetValue(name, out value))
            {
                dict.Remove(name);

                if (!String.IsNullOrEmpty(value))
                    return true;
            }

            if (required)
                throw new Exception("Missing parameter: " + (String.IsNullOrEmpty(name) ? "element content" : name));

            return false;
        }

        internal static void CheckForUnknownAttributes(Dictionary<string, string> dict)
        {
            if (dict.Count > 0)
                throw new Exception("Unrecognized parameter: " + dict.Keys.First());
        }

        public static void CheckForInterface(Type type, Type interfaceType)
        {
            if (type == null || interfaceType == null) return;

            //if (Array.IndexOf<Type>(type.GetInterfaces(), interfaceType) == -1)
            //	throw new System.Configuration.ConfigurationErrorsException("The type " + type.AssemblyQualifiedName + " must implement " + interfaceType.AssemblyQualifiedName);
        }

        public static EndPoint ResolveToEndPoint(string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentNullException("value");

            var parts = value.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("host:port is expected", "value");

            int port;
            if (!Int32.TryParse(parts[1], out port))
                throw new ArgumentException("Cannot parse port: " + parts[1], "value");

            return ResolveToEndPoint(parts[0], port);
        }

        public static EndPoint ResolveToEndPoint(string host, int port)
        {
            if (String.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");

            IPAddress address;
            // parse as an IP address
            if (!IPAddress.TryParse(host, out address))
            {
                var addresses = Dns.GetHostAddresses(host);
                address = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (address == null)
                    throw new ArgumentException(String.Format("Could not resolve host '{0}'.", host));
            }
            return new IPEndPoint(address, port);
        }
    }
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk? enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
