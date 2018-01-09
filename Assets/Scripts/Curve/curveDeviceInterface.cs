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

public class curveDeviceInterface : deviceInterface {

  public Transform backgroundQuad;
  LineRenderer lr;
  Vector2 quadDimensions = new Vector2(.5f, .25f);

  public override void Awake() {
    base.Awake();
    lr = GetComponentInChildren<LineRenderer>();
    updateDimensions();
    setupLine();
  }

  void setupLine() {
    int nodes = 65;
    lr.numPositions = nodes;
    Vector3[] points = new Vector3[nodes];
    for (int i = 0; i < nodes; i++) {
      float per = (float)i / (nodes - 1);
      float x = Mathf.Lerp(-.5f, .5f, per);
      float y = .5f * Mathf.Sin(Mathf.PI * 2 * per);
      points[i] = new Vector3(x, y, 0);
    }
    lr.SetPositions(points);
  }

  void updateDimensions() {
    backgroundQuad.localScale = new Vector3(quadDimensions.x, quadDimensions.y, 1);
    backgroundQuad.localPosition = new Vector3(-quadDimensions.x / 2f, 0, 0);
  }
}
