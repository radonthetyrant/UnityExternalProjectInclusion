using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

public class ExternalProjectConfiguration
{

    public static ExternalProjectConfiguration Instance
    {
        get
        {
            if (_instance == null)
                _instance = new ExternalProjectConfiguration();
            return _instance;
        }
    }

    static ExternalProjectConfiguration _instance;

    Dictionary<string, string> config;

    ExternalProjectConfiguration()
    {
        config = new Dictionary<string, string>();
        var doc = XDocument.Load("./Assets/Editor/ExternalProject.xml");
        if (doc != null)
        {
            XElement[] elements = doc.Root.Elements().ToArray();

            foreach (XElement element in elements)
            {
                config.Add(element.Name.LocalName, element.Value);
                //Debug.Log("Add: " + element.Name.LocalName + " = " + element.Value);
            }
        } else
        {
            Debug.LogError("File ExternalProject.xml not found!");
        }

        string[] files = Directory.GetFiles("./", "*.CSharp.csproj");

        if (files.Length > 0)
        {
            doc = XDocument.Load(files[0]);
            string ownGuid = doc.Descendants(XName.Get("ProjectGuid", @"http://schemas.microsoft.com/developer/msbuild/2003")).First().Value;
            if (ownGuid != null)
            {
                if (config.ContainsKey("ownProjectGuid"))
                    config["ownProjectGuid"] = ownGuid;
                else
                    config.Add("ownProjectGuid", ownGuid);
            }
            
        }
    }

    public string this[string key]
    {
        get
        {
            return config.ContainsKey(key) ? config[key] : "";
        }
    }

}
