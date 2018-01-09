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

public class panosphereDeviceInterface : deviceInterface {
  public Transform texSphere;
  public TextMesh label;
  Texture2D tex;
  button showButton;
  public Renderer flat;
  string filename;

  public bool sphereActive = false;

  public override void Awake() {
    base.Awake();
    showButton = GetComponentInChildren<button>();
    tex = new Texture2D(4, 4, TextureFormat.DXT1, false);

    texSphere.parent = null;
    texSphere.position = Vector3.zero;
    texSphere.rotation = Quaternion.identity;
  }

  public void toggleActive(bool on) {
    if (on == sphereActive) return;
    showButton.phantomHit(on);

    sphereActive = on;
    texSphere.gameObject.SetActive(on);
  }

  public override void hit(bool on, int ID = -1) {
    toggleActive(on);
    imageLoad.instance.togglePano(this, on);
  }

  void OnDestroy() {
    if (sphereActive) imageLoad.instance.togglePano(this, false);
    imageLoad.instance.removePano(this);
    Destroy(texSphere.gameObject);
  }

  public void loadImage(string path) {
    if (!File.Exists(path)) {
      Debug.Log("PATH FAILED: " + path);
      Destroy(gameObject);
      return;
    }

    filename = path;
    label.text = Path.GetFileNameWithoutExtension(path);
    StartCoroutine(loadImageRoutine(path));
  }

  IEnumerator loadImageRoutine(string path) {
    path = "file:///" + path;
    WWW www = new WWW(path);
    yield return www;
    www.LoadImageIntoTexture(tex);
    flat.material.mainTexture = tex;
  }

  public override InstrumentData GetData() {
    PanoData data = new PanoData();
    data.deviceType = menuItem.deviceType.Pano;
    GetTransformData(data);
    data.filename = filename;
    data.active = sphereActive;
    return data;
  }

  public override void Load(InstrumentData d) {
    PanoData data = d as PanoData;
    base.Load(data);
    imageLoad.instance.addPano(this);
    loadImage(data.filename);
    showButton.startToggled = data.active;
  }
}

public class PanoData : InstrumentData {
  public string filename;
  public bool active;
}