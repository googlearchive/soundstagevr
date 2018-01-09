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

public class generalGrip : manipObject {
  public Transform masterObj;

  GameObject highlight;
  Material highlightMat;
  Color glowColor = Color.HSVToRGB(0.1f, 0.7f, 0.1f);

  public override void Awake() {
    base.Awake();
    if (masterObj == null) masterObj = transform.parent.parent;
    createHandleFeedback();
  }

  void createHandleFeedback() {
    highlight = new GameObject("highlight");
    MeshFilter m = highlight.AddComponent<MeshFilter>();

    m.mesh = GetComponent<MeshFilter>().mesh;
    MeshRenderer r = highlight.AddComponent<MeshRenderer>();
    r.material = Resources.Load("Materials/Highlight") as Material;
    highlightMat = r.material;

    highlight.transform.SetParent(transform, false);

    highlight.transform.localScale = new Vector3(1.15f, 1.05f, 1.1f);
    highlight.transform.localPosition = new Vector3(0, -.0025f, 0);
    highlightMat.SetColor("_TintColor", glowColor);
    highlightMat.SetFloat("_EmissionGain", .75f);

    highlight.SetActive(false);
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      transform.parent.parent = masterObj;
    }

    curState = state;

    if (curState == manipState.none) {
      highlight.SetActive(false);
    }
    if (curState == manipState.selected) {
      highlight.SetActive(true);
      highlightMat.SetFloat("_EmissionGain", .55f);
    }
    if (curState == manipState.grabbed) {
      highlight.SetActive(true);
      transform.parent.parent = manipulatorObj.parent;
      highlightMat.SetFloat("_EmissionGain", .75f);
    }
  }
}
