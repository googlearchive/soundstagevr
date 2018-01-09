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

public class midiPanel : manipObject {
  public TextMesh label;
  public Renderer outline;

  Material mat;
  Material textMat;
  public componentInterface _componentInterface;
  public int buttonID = -1;

  Color normalColor;

  Vector2 gains = new Vector2(.3f, .45f);

  public override void Awake() {
    base.Awake();
    mat = outline.material;

    normalColor = Color.HSVToRGB(.25f, .7f, .9f);

    textMat = label.GetComponent<Renderer>().material;
    textMat.SetColor("_TintColor", normalColor);
    textMat.SetFloat("_EmissionGain", gains.x);

    mat.SetColor("_TintColor", normalColor);
    mat.SetFloat("_EmissionGain", gains.x);
  }

  void Start() {
    if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();
  }

  public override void setState(manipState state) {
    if (curState == manipState.grabbed && state != manipState.grabbed) {
      if (_componentInterface != null) _componentInterface.hit(false, buttonID);
    }
    curState = state;
    if (curState == manipState.none) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", gains.x);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", gains.x);
      }
    } else if (curState == manipState.selected) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", gains.y);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", gains.x);
      }
    } else if (curState == manipState.grabbed) {
      mat.SetColor("_TintColor", Color.white);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", gains.y);
      }
      if (_componentInterface != null) _componentInterface.hit(true, buttonID);
    }
  }
}
