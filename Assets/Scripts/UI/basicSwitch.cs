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

public class basicSwitch : manipObject {
  public Transform onLabel, offLabel;
  Material[] labelMats;
  public bool switchVal = false;
  public Transform switchObject;
  float rotationIncrement = 45f;
  public Transform glowTrans;
  Material mat;
  Color customColor;

  public bool redOption = true;

  public override void Awake() {
    base.Awake();
    mat = glowTrans.GetComponent<Renderer>().material;
    customColor = Color.HSVToRGB(.6f, .5f, .1f);
    mat.SetColor("_TintColor", Color.black);

    if (onLabel != null && offLabel != null) {
      labelMats = new Material[2];
      labelMats[0] = onLabel.GetComponent<Renderer>().material;
      labelMats[1] = offLabel.GetComponent<Renderer>().material;

      labelMats[0].SetColor("_TintColor", Color.HSVToRGB(.4f, .7f, .9f));
      labelMats[0].SetFloat("_EmissionGain", .3f);

      labelMats[1].SetColor("_TintColor", Color.HSVToRGB(redOption ? 0 : .4f, .7f, .9f));
      labelMats[1].SetFloat("_EmissionGain", .3f);
    }

    setSwitch(true, true);
  }

  public void setSwitch(bool on, bool forced = false) {
    if (switchVal == on && !forced) return;
    if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
    switchVal = on;
    float rot = rotationIncrement * (switchVal ? 1 : -1);
    switchObject.localRotation = Quaternion.Euler(rot, 0, 0);


    if (onLabel != null && offLabel != null) {
      labelMats[0].SetColor("_TintColor", Color.HSVToRGB(.4f, .7f, on ? .9f : .1f));
      labelMats[0].SetFloat("_EmissionGain", on ? .3f : .05f);

      labelMats[1].SetColor("_TintColor", Color.HSVToRGB(redOption ? 0 : .4f, .7f, !on ? .9f : .1f));
      labelMats[1].SetFloat("_EmissionGain", !on ? .3f : .05f);
    }
  }

  public override void grabUpdate(Transform t) {
    float curY = transform.InverseTransformPoint(manipulatorObj.position).z - offset;
    if (Mathf.Abs(curY) > 0.01f) setSwitch(curY > 0);
  }

  float offset;
  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      mat.SetColor("_TintColor", Color.black);
    } else if (curState == manipState.selected) {
      mat.SetColor("_TintColor", customColor * 0.5f);
    } else if (curState == manipState.grabbed) {
      mat.SetColor("_TintColor", customColor);
      offset = transform.InverseTransformPoint(manipulatorObj.position).z;
    }
  }
}
