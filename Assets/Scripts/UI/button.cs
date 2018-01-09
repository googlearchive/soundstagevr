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

public class button : manipObject {
  public bool isToggle = false;
  public bool toggleKey = false;
  public int buttonID;
  public int[] button2DID = new int[] { 0, 0 };
  public bool glowMatOnToggle = true;
  public Material onMat;
  Renderer rend;
  Material offMat;
  Material glowMat;
  public componentInterface _componentInterface;
  public GameObject selectOverlay;
  public float glowHue = 0;
  Color glowColor = Color.HSVToRGB(0, .5f, .25f);
  Color offColor;

  public bool onlyOn = false;

  bool singleID = true;

  Renderer labelRend;
  public Color labelColor = new Color(0.75f, .75f, 1f);
  public float labelEmission = .4f;
  public float glowEmission = .5f;

  Queue<bool> hits = new Queue<bool>();
  public bool startToggled = false;
  public bool disregardStartToggled = false;

  public bool changeOverlayGlow = false;

  public override void Awake() {
    base.Awake();
    toggleKey = false;
    glowColor = Color.HSVToRGB(glowHue, .5f, .25f);

    if (_componentInterface == null) {
      if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();
    }

    rend = GetComponent<Renderer>();
    offMat = rend.material;
    offColor = offMat.GetColor("_Color");
    glowMat = new Material(onMat);
    glowMat.SetFloat("_EmissionGain", glowEmission);
    glowMat.SetColor("_TintColor", glowColor);
    selectOverlay.SetActive(false);

    if (changeOverlayGlow) {
      selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", glowColor);
      selectOverlay.GetComponent<Renderer>().material.SetFloat("_EmissionGain", glowEmission);
    }

    if (GetComponentInChildren<TextMesh>() != null) labelRend = GetComponentInChildren<TextMesh>().transform.GetComponent<Renderer>();
    if (labelRend != null) {
      labelRend.material.SetFloat("_EmissionGain", .1f);
      labelRend.material.SetColor("_TintColor", labelColor);
    }
  }

  void Start() {
    if (disregardStartToggled) return;
    keyHit(startToggled);
  }

  public void Highlight(bool on) {
    if (on) {
      glowMat.SetFloat("_EmissionGain", .9f);
      offMat.SetColor("_Color", glowColor);
    } else {
      glowMat.SetFloat("_EmissionGain", .7f);
      offMat.SetColor("_Color", offColor);
    }
  }

  public void Setup(int IDx, int IDy, bool on, Color c) {
    singleID = false;

    if (_componentInterface == null) {
      if (transform.parent.GetComponent<componentInterface>() != null) _componentInterface = transform.parent.GetComponent<componentInterface>();
      else _componentInterface = transform.parent.parent.GetComponent<componentInterface>();
    }
    button2DID[0] = IDx;
    button2DID[1] = IDy;
    glowColor = c;
    keyHit(on);
    startToggled = on;
    glowMat.SetColor("_TintColor", glowColor);
  }

  public void setOnAtStart(bool on) {
    keyHit(on);
    startToggled = on;
  }

  public void phantomHit(bool on) {
    hits.Enqueue(on);
  }

  void Update() {
    for (int i = 0; i < hits.Count; i++) {
      bool on = hits.Dequeue();
      isHit = on;
      toggled = on;
      if (on) {
        if (glowMatOnToggle) rend.material = glowMat;
        if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", labelEmission);
      } else {
        if (glowMatOnToggle) rend.material = offMat;
        if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", .1f);

      }
    }
  }

  public bool isHit = false;
  public void keyHit(bool on) {
    isHit = on;
    toggled = on;

    if (on) {
      if (singleID) _componentInterface.hit(on, buttonID);
      else _componentInterface.hit(on, button2DID[0], button2DID[1]);

      if (glowMatOnToggle) rend.material = glowMat;
      if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", labelEmission);
    } else {
      if (singleID) _componentInterface.hit(on, buttonID);
      else _componentInterface.hit(on, button2DID[0], button2DID[1]);

      if (glowMatOnToggle) rend.material = offMat;
      if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", .1f);
    }

  }

  bool toggled = false;

  public override void setState(manipState state) {
    if (curState == manipState.grabbed && state != curState) {
      if (!isToggle) keyHit(false);
      if (!glowMatOnToggle) {
        rend.material = offMat;
      }
    }
    curState = state;
    if (curState == manipState.none) {
      if (!singleID) _componentInterface.onSelect(false, button2DID[0], button2DID[1]);
      selectOverlay.SetActive(false);
    } else if (curState == manipState.selected) {
      if (!singleID) _componentInterface.onSelect(true, button2DID[0], button2DID[1]);
      selectOverlay.SetActive(true);
    } else if (curState == manipState.grabbed) {
      if (!singleID) _componentInterface.onSelect(true, button2DID[0], button2DID[1]);
      if (isToggle) {
        toggled = !toggled;
        if (toggled) keyHit(true);
        else if (!onlyOn) keyHit(false);
      } else keyHit(true);

      if (!glowMatOnToggle) {
        rend.material = glowMat;
      }
    }
  }

  public override void onTouch(bool on, manipulator m) {
    if (m != null) {
      if (m.emptyGrab) {
        if (!on) {
          if (!isToggle) keyHit(false);
          if (!glowMatOnToggle) {
            rend.material = offMat;
          }
        } else {
          if (isToggle) {
            toggled = !toggled;
            if (toggled) keyHit(true);
            else if (!onlyOn) keyHit(false);
          } else keyHit(true);

          if (!glowMatOnToggle) {
            rend.material = glowMat;
          }
        }
      }
    }
  }
}
