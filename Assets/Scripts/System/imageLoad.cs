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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class imageLoad : MonoBehaviour {

  public static imageLoad instance;
  public GameObject panoSphere;
  public GameObject groundPlane;
  List<panosphereDeviceInterface> panos = new List<panosphereDeviceInterface>();

  void Awake() {
    instance = this;
  }

  public void createPano(string path) {
    if (!File.Exists(path)) return;

    panosphereDeviceInterface p = (Instantiate(panoSphere, Vector3.up + Vector3.right * panos.Count * .2f, Quaternion.identity) as GameObject).GetComponent<panosphereDeviceInterface>();
    panos.Add(p);
    p.loadImage(path);

  }

  public void addPano(panosphereDeviceInterface p) {
    panos.Add(p);
  }

  panosphereDeviceInterface curPano;
  public void togglePano(panosphereDeviceInterface p, bool on) {
    if (on) {

      for (int i = 0; i < panos.Count; i++) {
        if (panos[i] != p) panos[i].toggleActive(false);
        else {
          curPano = panos[i];
          groundPlane.SetActive(false);
        }
      }
    } else {
      if (curPano == p && masterControl.instance.showEnvironment) groundPlane.SetActive(true);
    }
  }

  public void removePano(panosphereDeviceInterface p) {
    panos.Remove(p);
  }
}
