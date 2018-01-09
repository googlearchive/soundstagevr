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

public class timelineHandle : manipObject {
  public Transform timelineTransform;
  timelineEvent _timelineEvent;
  GameObject highlight;
  Material highlightMat;

  Color glowColor = Color.HSVToRGB(0.1f, 0.7f, 0.1f);

  public bool stretchMode = false;

  public override void Awake() {
    base.Awake();
    canBeDeleted = true;
    _timelineEvent = GetComponentInParent<timelineEvent>();
    timelineTransform = _timelineEvent.transform.parent;
    createHandleFeedback();
  }

  public override void selfdelete() {
    _timelineEvent.removeSelf();
    Destroy(_timelineEvent.gameObject);
  }

  public void setHue(float h) {
    h = Mathf.Repeat(h, 6) / 6f;
    GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.HSVToRGB(h, .95f, 1));
    highlightMat.SetColor("_TintColor", Color.HSVToRGB(h, 0.7f, 0.1f));
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

  void Update() {
    if (recordDesired) {
      highlight.SetActive(recording);
      GetComponent<Renderer>().enabled = !recording;
      GetComponent<Collider>().enabled = !recording;
      recordDesired = false;
    }
  }

  bool recordDesired = false;
  bool recording = false;

  public void setRecord(bool on) {
    recordDesired = true;
    recording = on;
  }

  public override void grabUpdate(Transform t) {
    if (!stretchMode) {

      Vector2 a = _timelineEvent._componentInterface.worldPosToGridPos(t.position);
      Vector2 dif = a - manipOffset;
      Vector2 newPos = startPos + dif;

      if (!_timelineEvent._componentInterface.notelock) {
        _timelineEvent.track = Mathf.FloorToInt(_timelineEvent._componentInterface._gridParams.YtoUnit(a.y));
        setHue(_timelineEvent.track);
      }

      float newIn = startIn + dif.x;
      if (_timelineEvent._componentInterface.snapping) {
        newIn = _timelineEvent._componentInterface._gridParams.XtoSnap(newIn, false);
      }
      _timelineEvent.in_out.x = _timelineEvent._componentInterface._gridParams.XtoUnit(newIn);
      _timelineEvent.in_out.y = _timelineEvent.in_out.x + unitLength;

      _timelineEvent.gridUpdate();

    } else {
      Vector3 a = _timelineEvent._componentInterface.worldPosToGridPos(t.position, true);
      float dif = a.x - manipOffset.x;
      if (manipOffset.x - a.x > 0) {
        a.x = dif + manipOffset.x - _timelineEvent._componentInterface._gridParams.unitSize / (2 * _timelineEvent._componentInterface._gridParams.snapFraction);
        _timelineEvent.edgeOutHandle.transform.position = _timelineEvent.edgeOut.position = timelineTransform.TransformPoint(a);

        a.x = manipOffset.x + _timelineEvent._componentInterface._gridParams.unitSize / (2 * _timelineEvent._componentInterface._gridParams.snapFraction);
        _timelineEvent.edgeInHandle.transform.position = _timelineEvent.edgeIn.position = timelineTransform.TransformPoint(a);
      } else {
        a.x = dif + manipOffset.x + _timelineEvent._componentInterface._gridParams.unitSize / (2 * _timelineEvent._componentInterface._gridParams.snapFraction);
        _timelineEvent.edgeInHandle.transform.position = _timelineEvent.edgeIn.position = timelineTransform.TransformPoint(a);

        a.x = manipOffset.x - _timelineEvent._componentInterface._gridParams.unitSize / (2 * _timelineEvent._componentInterface._gridParams.snapFraction);
        _timelineEvent.edgeOutHandle.transform.position = _timelineEvent.edgeOut.position = timelineTransform.TransformPoint(a);
      }

      _timelineEvent.recalcTrackPosition();
    }
  }

  float startIn, unitLength;
  Vector2 startPos;
  Vector2 manipOffset;
  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      _timelineEvent.grabbed = false;
      if (stretchMode) stretchMode = false;

      _timelineEvent.transform.position = highlight.transform.position;
      _timelineEvent.transform.rotation = highlight.transform.rotation;

      highlight.transform.localPosition = Vector3.zero;
      highlight.transform.localRotation = Quaternion.identity;

      _timelineEvent.transform.parent = timelineTransform;
      _timelineEvent.recalcTrackPosition();

      _timelineEvent.toggleEdges(true);

      _timelineEvent.overlapCheck();/// = false;
    }

    curState = state;

    if (curState == manipState.none) {

      if (multiselected) {
        highlight.SetActive(true);
        if (true) highlightMat.SetFloat("_EmissionGain", .9f);
      } else highlight.SetActive(false);

    }
    if (curState == manipState.selected) {
      highlight.SetActive(true);
      highlightMat.SetFloat("_EmissionGain", .55f);
    }
    if (curState == manipState.grabbed) {
      _timelineEvent._componentInterface.killMultiselect();
      _timelineEvent.grabbed = true;
      _timelineEvent.toggleEdges(false);
      highlight.SetActive(true);

      if (stretchMode) manipOffset = _timelineEvent._componentInterface.worldPosToGridPos(manipulatorObj.position, true);
      else {
        manipOffset = _timelineEvent._componentInterface.worldPosToGridPos(manipulatorObj.position);
        startPos = _timelineEvent.transform.localPosition;
        startIn = _timelineEvent._componentInterface._gridParams.UnittoX(_timelineEvent.in_out.x);
        unitLength = _timelineEvent.in_out.y - _timelineEvent.in_out.x;
      }
      highlightMat.SetFloat("_EmissionGain", .75f);
    }
  }

  bool multiselected = false;
  public void OnMultiselect(bool on) {
    multiselected = on;
    highlight.SetActive(on);
    if (on) highlightMat.SetFloat("_EmissionGain", .9f);
  }
}

