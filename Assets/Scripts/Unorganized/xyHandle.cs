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

public class xyHandle : manipObject {
  public int ID = 0;

  public Vector2 xBounds = new Vector2(-Mathf.Infinity, -.05f);
  public Vector2 yBounds = new Vector2(-Mathf.Infinity, 0);
  public Vector2 percent = Vector2.zero;
  public Material onMat;
  Renderer rend;
  Material offMat;
  Material glowMat;

  public bool usingGlowMat = true;
  public bool usePercent = true;

  public componentInterface _interface;

  Color glowColor = Color.HSVToRGB(0, .5f, .1f);

  public override void Awake() {
    base.Awake();
    rend = GetComponent<Renderer>();

    if (usingGlowMat) {
      offMat = rend.material;
      glowMat = new Material(onMat);
      glowMat.SetColor("_TintColor", glowColor);
    } else rend.material.SetColor("_TintColor", glowColor * .25f);
  }

  void Start() {
    if (usePercent) setPercent(percent);
  }

  public void pulse() {
    if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(750);
  }

  public void tryPos(Vector3 pos) {
    if (curState != manipState.grabbed) transform.localPosition = pos;
  }

  public Vector2 getDif() {
    if (curState != manipState.grabbed) return Vector2.zero;
    return (Vector2)transform.localPosition - offset;
  }

  public void resetOffset() {
    if (curState != manipState.grabbed) return;
    offset.x = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
    offset.y = transform.localPosition.y - transform.parent.InverseTransformPoint(manipulatorObj.position).y;
  }

  public override void grabUpdate(Transform t) {
    Vector3 p = transform.localPosition;
    p.x = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).x + offset.x, xBounds.x, xBounds.y);
    p.y = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).y + offset.y, yBounds.x, yBounds.y);
    transform.localPosition = p;
    updatePercent();
  }

  void updatePercent() {
    if (!usePercent) return;
    percent.x = Mathf.InverseLerp(xBounds.x, xBounds.y, transform.localPosition.x);
    percent.y = Mathf.InverseLerp(yBounds.x, yBounds.y, transform.localPosition.y);
  }

  public void forceChange(float value, bool Xaxis) {
    setPercent(new Vector2(value, value), Xaxis, !Xaxis);
    if (curState == manipState.grabbed) {
      offset.x = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
      offset.y = transform.localPosition.y - transform.parent.InverseTransformPoint(manipulatorObj.position).y;
    }
    updatePercent();
  }

  public void setPercent(Vector2 p, bool doX = true, bool doY = true) {
    Vector3 pos = transform.localPosition;
    if (doX) pos.x = Mathf.Lerp(xBounds[0], xBounds[1], p.x);
    if (doY) pos.y = Mathf.Lerp(yBounds[0], yBounds[1], p.y);
    transform.localPosition = pos;
    updatePercent();
  }

  Vector3 posstart = Vector2.zero;
  Vector2 offset = Vector2.zero;
  public override void setState(manipState state) {
    if (curState == manipState.grabbed) {
      if (_interface != null) _interface.onGrab(false, 0);
    }
    curState = state;
    if (curState == manipState.none) {
      if (_interface != null) _interface.onSelect(false, ID);
      if (usingGlowMat) rend.material = offMat;
      else rend.material.SetColor("_TintColor", glowColor * 0.25f);
    } else if (curState == manipState.selected) {
      if (_interface != null) _interface.onSelect(true, ID);
      if (usingGlowMat) {
        rend.material = glowMat;
        glowMat.SetColor("_TintColor", glowColor * .25f);
      } else rend.material.SetColor("_TintColor", glowColor * 0.5f);
    } else if (curState == manipState.grabbed) {
      if (_interface != null) _interface.onSelect(true, ID);
      if (usingGlowMat) rend.material = glowMat;
      else rend.material.SetColor("_TintColor", glowColor * 0.5f);

      posstart = (Vector2)transform.localPosition;
      offset.x = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
      offset.y = transform.localPosition.y - transform.parent.InverseTransformPoint(manipulatorObj.position).y;

      if (_interface != null) _interface.onGrab(true, 0);
    }
  }
}

