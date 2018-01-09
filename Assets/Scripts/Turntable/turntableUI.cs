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

public class turntableUI : manipObject {
  Material mat;
  Transform masterObj;

  Vector3 startPos;
  Quaternion startRot;

  turntableComponentInterface _interface;

  public override void Awake() {
    base.Awake();
    _interface = GetComponentInParent<turntableComponentInterface>();
    mat = GetComponent<Renderer>().material;
    masterObj = transform.parent;
  }

  float lastAngle = 0;
  public override void grabUpdate(Transform t) {
    Vector3 pos = masterObj.InverseTransformPoint(t.position);
    pos.y = 0;

    transform.localRotation = Quaternion.FromToRotation(startPos, pos) * startRot;

    float newAngle = Mathf.Atan2(pos.x, pos.z) * Mathf.Rad2Deg;
    _interface.addDelta(Mathf.DeltaAngle(lastAngle, newAngle));
    lastAngle = newAngle;
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      _interface.updateTurntableGrab(false);
    }

    curState = state;

    if (curState == manipState.none) {
      mat.SetFloat("_EmissionGain", .4f);
    }
    if (curState == manipState.selected) {
      mat.SetFloat("_EmissionGain", .5f);
    }
    if (curState == manipState.grabbed) {
      _interface.updateTurntableGrab(true);
      mat.SetFloat("_EmissionGain", .6f);
      startRot = transform.localRotation;
      startPos = masterObj.InverseTransformPoint(manipulatorObj.position);
      startPos.y = 0;
      lastAngle = Mathf.Atan2(startPos.x, startPos.z) * Mathf.Rad2Deg;
    }
  }
}
