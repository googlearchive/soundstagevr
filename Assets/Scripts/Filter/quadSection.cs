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

public class quadSection : manipObject {
  public int ID = 0;

  int texSize = 64;
  Texture2D tex, texB;
  Renderer texrend;
  Color32[] texpixels;
  filterDeviceInterface _deviceInterface;

  float width = .1f;
  float height = .1f;
  float depth = .2f;

  float edgeMin = -.04f;
  float edgeMax = .26f;

  public bool toggled = true;
  float hue = .0875f;

  public override void Awake() {
    base.Awake();
    _deviceInterface = GetComponentInParent<filterDeviceInterface>();
    texrend = GetComponent<Renderer>();

    tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
    texB = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
    texpixels = new Color32[texSize * texSize];

    GenerateOutline();
    tex.SetPixels32(texpixels);
    GenerateFlat();
    texB.SetPixels32(texpixels);
    tex.Apply(false);
    texB.Apply(false);

    texrend.material.mainTexture = texB;

    Color c = Color.HSVToRGB(hue, 182 / 255f, 229 / 255f);
    texrend.material.SetColor("_TintColor", c);
    texrend.material.SetFloat("_EmissionGain", .1f);

    setToggle(toggled);
    transform.localScale = new Vector3(width, height, depth);
  }

  bool outlined = false;
  void toggleOutline() {
    outlined = !outlined;
    if (outlined) texrend.material.mainTexture = tex;
    else texrend.material.mainTexture = texB;
  }

  public void getPercentages(out float percentA, out float percentB) {
    float amount = edgeMax - edgeMin;
    float edgeA = transform.localPosition.x - width / 2 - gap;
    float edgeB = transform.localPosition.x + width / 2 + gap;

    percentA = (edgeA - edgeMin) / amount;
    percentB = (edgeB - edgeMin) / amount;
  }

  float getX(float per) {
    return per * (edgeMax - edgeMin) + edgeMin;
  }

  void setOutline(bool on) {
    if (on) texrend.material.mainTexture = tex;
    else texrend.material.mainTexture = texB;
  }

  void GenerateOutline() {
    for (int i = 0; i < texSize; i++) {
      for (int i2 = 0; i2 < texSize; i2++) {
        byte s = 50;
        if (i == 0 || i == texSize - 1 || i2 == 0 || i2 == texSize - 1) s = 255;
        texpixels[i2 * texSize + i] = new Color32(s, s, s, s);
      }
    }
  }

  void GenerateFlat() {
    for (int i = 0; i < texSize; i++) {
      for (int i2 = 0; i2 < texSize; i2++) {
        byte s = 50;
        texpixels[i2 * texSize + i] = new Color32(s, s, s, s);
      }

    }
  }

  enum vizstate {
    off,
    on,
    selected_off,
    selected_on,
    grab_on,
    grab_off
  };

  vizstate curvizstate = vizstate.off;

  void setVizState(vizstate s) {
    switch (s) {
      case vizstate.off:
        setOutline(false);
        texrend.material.SetFloat("_EmissionGain", .1f);
        break;
      case vizstate.on:
        setOutline(false);
        texrend.material.SetFloat("_EmissionGain", .3f);
        break;
      case vizstate.selected_off:
        setOutline(true);
        texrend.material.SetFloat("_EmissionGain", .1f);
        break;
      case vizstate.selected_on:
        setOutline(true);
        texrend.material.SetFloat("_EmissionGain", .3f);
        break;
      case vizstate.grab_off:
        setOutline(true);
        texrend.material.SetFloat("_EmissionGain", .15f);
        break;
      case vizstate.grab_on:
        setOutline(true);
        texrend.material.SetFloat("_EmissionGain", .5f);
        break;
      default:
        break;
    }
  }

  public void setToggle(bool on) {
    toggled = on;
    if (curState == manipState.none) setVizState(toggled ? vizstate.on : vizstate.off);
    else if (curState == manipState.selected) setVizState(toggled ? vizstate.selected_on : vizstate.selected_off);
    else setVizState(toggled ? vizstate.grab_on : vizstate.grab_off);
  }

  bool updateWidth(float w, int dir) {
    Vector3 p = transform.localPosition;

    float delta = width;

    width = Mathf.Clamp(w, .01f, .28f);
    delta = delta - width;
    p.x += dir * delta / 2;

    transform.localScale = new Vector3(width, height, depth);
    transform.localPosition = p;

    if (width == .01f) return false;
    else return true;
  }

  float offset = 0;
  public override void grabUpdate(Transform t) {
    float modW = transform.parent.InverseTransformPoint(manipulatorObj.position).x - offset;

    if (ID == 0) {
      updateWidth(startWidth + modW, -1);
      Vector3 p = transform.localPosition;
      _deviceInterface.quads[1].updateEdge(-1, p.x + width / 2 + gap);
    } else if (ID == 2) {
      updateWidth(startWidth - modW, 1);
      Vector3 p = transform.localPosition;
      _deviceInterface.quads[1].updateEdge(1, p.x - width / 2 - gap);
    } else {
      updatePosition(startX + modW);
    }
  }

  float gap = .01f;
  public void updateEdge(int edge, float xPos) {
    Vector3 pos = transform.localPosition;
    float amount = width - ((pos.x + edge * width / 2) - xPos) * edge;
    bool check = updateWidth(amount, -edge);

    if (!check && ID == 1) //the middle one is as small as it's going to be so start shifting over
    {
      updatePosition(transform.localPosition.x + (width - amount) * -edge);
    }
  }

  public void setupPercents(float p0, float p1) //just for center quad
  {
    updateEdge(-1, getX(p0));
    updateEdge(1, getX(p1));
    updatePosition(getX((p1 - p0) / 2 + p0));
  }

  public void updatePercentage(float p) {
    if (ID == 1) {
      p = Mathf.Lerp(edgeMin + gap + width / 2, edgeMax - gap - width / 2, p);
      updatePosition(p);
    } else if (ID == 0) {
      float w = Mathf.Lerp(.01f, .28f - _deviceInterface.quads[2].width, p);
      updateWidth(w, -1);
      Vector3 pos = transform.localPosition;
      _deviceInterface.quads[1].updateEdge(-1, pos.x + width / 2 + gap);

    } else if (ID == 2) {
      float w = Mathf.Lerp(.01f, .28f - _deviceInterface.quads[0].width, p);
      updateWidth(w, 1);
      Vector3 pos = transform.localPosition;
      _deviceInterface.quads[1].updateEdge(1, pos.x - width / 2 - gap);

    }
  }

  public void updatePosition(float x) {
    Vector3 p = transform.localPosition;
    p.x = Mathf.Clamp(x, edgeMin + gap + width / 2, edgeMax - gap - width / 2);

    _deviceInterface.quads[0].updateEdge(1, p.x - width / 2 - gap);
    _deviceInterface.quads[2].updateEdge(-1, p.x + width / 2 + gap);
    transform.localPosition = p;
  }

  float startWidth = 0;
  float startX = 0;
  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      setVizState(toggled ? vizstate.on : vizstate.off);
    } else if (curState == manipState.selected) {
      setVizState(toggled ? vizstate.selected_on : vizstate.selected_off);
    } else if (curState == manipState.grabbed) {

      setVizState(toggled ? vizstate.grab_on : vizstate.grab_off);

      startWidth = width;
      startX = transform.localPosition.x;
      offset = transform.parent.InverseTransformPoint(manipulatorObj.position).x;
    }
  }
}
