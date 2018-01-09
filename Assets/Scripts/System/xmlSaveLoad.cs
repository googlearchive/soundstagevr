// Copyright 2017 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

[XmlRoot("SynthSet")]
public class xmlSaveLoad {
  [XmlArray("Instruments"), XmlArrayItem("Instruments")]
  public List<InstrumentData> InstrumentList = new List<InstrumentData>();

  [XmlArray("Plugs"), XmlArrayItem("Plugs")]
  public List<PlugData> PlugList = new List<PlugData>();

  [XmlArray("Systems"), XmlArrayItem("Systems")]
  public List<SystemData> SystemList = new List<SystemData>();

  public static xmlSaveLoad LoadFromFile(string path) {
    XmlSerializer serializer = new XmlSerializer(typeof(xmlSaveLoad));
    using (var stream = new FileStream(path, FileMode.Open)) {
      return serializer.Deserialize(stream) as xmlSaveLoad;
    }
  }

  public void SaveToFile(string path) {
    XmlSerializer serializer = new XmlSerializer(typeof(xmlSaveLoad));
    using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8"))) {
      serializer.Serialize(stream, this);
    }
  }

}