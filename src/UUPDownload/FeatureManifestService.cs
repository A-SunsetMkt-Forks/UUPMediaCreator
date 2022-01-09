﻿// Copyright (c) Gustave Monce and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using Microsoft.Cabinet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace UUPDownload
{
    public static class FeatureManifestService
    {
        const int MAXIMUM_CANDIDATE_CAB_SIZE = 1024 * 48; // Feature manifest cabinets have not been observed to be larger than 48KiB

        public static IDictionary<string, string> GetAppxPackageLicenseFileMapFromCabs(IList<string> cabPaths)
        {
            var licenseMap = new Dictionary<string, string>();
            foreach(var cabPath in cabPaths)
            {
                if (new FileInfo(cabPath).Length > MAXIMUM_CANDIDATE_CAB_SIZE)
                    continue;

                using CabinetHandler cab = new CabinetHandler(File.OpenRead(cabPath));
                foreach (var file in cab.Files)
                {
                    if (!Path.GetExtension(file).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var xdoc = XDocument.Load(cab.OpenFile(file), LoadOptions.None);
                        var ns = xdoc.Root.GetDefaultNamespace();
                        var packages = xdoc.Descendants(ns + "AppXPackages");
                        if (packages != null)
                        {
                            foreach (var package in packages.Elements())
                            {
                                var name = package.Attribute("Name")?.Value;
                                var license = package.Attribute("LicenseFile")?.Value;
                                if (name != null && license != null)
                                {
                                    if (licenseMap.ContainsKey(name) && licenseMap[name] != license)
                                    {
                                        Logging.Log($"Package {name} has multiple licenses. Ignoring: {license}.", Logging.LoggingLevel.Warning);
                                    }
                                    else
                                    {
                                        licenseMap[name] = license;
                                    }
                                }
                            }
                        }

                        xdoc = null;
                    }
                    catch(XmlException)
                    {
                        // Skip all unreadable xml
                    }
                }
            }
            return licenseMap;
        }
    }
}