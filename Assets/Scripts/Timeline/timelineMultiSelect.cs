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

public class timelineMultiSelect : manipObject {

  Material[] mats;
  public timelineComponentInterface _interface;
  BoxCollider coll;

  Vector2 cornerA, cornerB;
  Vector2 xRange, yRange;

  public override void Awake() {
    base.Awake();
    createFrame();

    mats = GetComponent<Renderer>().materials;
    mats[0].SetColor("_TintColor", Color.white);
  }

  public void Activate() {
    recalcRange();

    if (selectedEvents.Count == 0) Close();


    mats[0].SetColor("_TintColor", Color.blue);
    coll = GetComponent<BoxCollider>();
    coll.enabled = true;
  }

  public void Close() {

    for (int i = 0; i < selectedEvents.Count; i++) {
      if (selectedEvents[i] != null) {
        selectedEvents[i].OnMultiselect(false);
      }
    }

    Destroy(gameObject);
  }

  void recalcRange() {
    cornerA = _interface.worldPosToGridPos(transform.TransformPoint(-.5f, -.5f, 0));
    cornerB = _interface.worldPosToGridPos(transform.TransformPoint(.5f, .5f, 0));


    xRange = new Vector2(_interface._gridParams.XtoUnit(cornerA.x), _interface._gridParams.XtoUnit(cornerB.x));
    yRange = new Vector2(_interface._gridParams.YtoUnit(cornerA.y), _interface._gridParams.YtoUnit(cornerB.y));

    if (xRange.x > xRange.y) xRange = new Vector2(xRange.y, xRange.x);
    if (yRange.x > yRange.y) yRange = new Vector2(yRange.y, yRange.x);
  }

  List<timelineEvent> selectedEvents = new List<timelineEvent>();
  public void SelectCheck() {
    recalcRange();
    selectedEvents.Clear();
    for (int i = 0; i < _interface._tlEvents.Count; i++) {
      if (_interface._tlEvents[i] != null) {
        if (_interface._tlEvents[i].inMultiSelectRange(xRange, yRange)) {
          selectedEvents.Add(_interface._tlEvents[i]);
          _interface._tlEvents[i].OnMultiselect(true);
        } else {
          _interface._tlEvents[i].OnMultiselect(false);
        }
      }
    }
  }

  Mesh mesh;
  Vector3[] framepoints;
  int[] framelines;
  int[] frameQuad;
  Renderer rend;

  void createFrame() {
    rend = GetComponent<Renderer>();

    float w = 1;
    float h = 1;
    mesh = new Mesh();
    mesh.subMeshCount = 2;
    framepoints = new Vector3[]
    {
            new Vector3(-w/2f,-h/2,0),
            new Vector3(-w/2f,h/2,0),
            new Vector3(w/2f,h/2,0),
            new Vector3(w/2f,-h/2,0)
    };

    framelines = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 };

    mesh.vertices = framepoints;
    mesh.SetIndices(framelines, MeshTopology.Lines, 0);

    frameQuad = new int[] { 0, 1, 2, 3 };
    mesh.SetIndices(frameQuad, MeshTopology.Quads, 1);
    GetComponent<MeshFilter>().mesh = mesh;
  }


  public void updateViz() {
    float w = 1;
    float h = 1;

    float x1 = -w / 2f;
    float x2 = w / 2f;

    float y1 = -h / 2f;
    float y2 = h / 2f;

    Vector3 A = _interface.transform.InverseTransformPoint(transform.TransformPoint(-.5f, -.5f, 0));
    Vector3 B = _interface.transform.InverseTransformPoint(transform.TransformPoint(.5f, .5f, 0));

    bool a1 = false;
    bool b1 = false;

    a1 = A.y < 0;
    b1 = B.y < 0;
    if (a1 & b1) {
      rend.enabled = false;
      coll.enabled = false;
      return;
    } else if (a1) y1 = transform.InverseTransformPoint(_interface.transform.TransformPoint(Vector3.zero)).y;
    else if (b1) y2 = transform.InverseTransformPoint(_interface.transform.TransformPoint(Vector3.zero)).y;

    a1 = A.y > _interface._gridParams.getGridHeight();
    b1 = B.y > _interface._gridParams.getGridHeight();
    if (a1 & b1) {
      rend.enabled = false;
      coll.enabled = false;
      return;
    } else if (a1) y1 = transform.InverseTransformPoint(_interface.transform.TransformPoint(new Vector3(0, _interface._gridParams.getGridHeight(), 0))).y;
    else if (b1) y2 = transform.InverseTransformPoint(_interface.transform.TransformPoint(new Vector3(0, _interface._gridParams.getGridHeight(), 0))).y;

    a1 = A.x > 0;
    b1 = B.x > 0;
    if (a1 & b1) {
      rend.enabled = false;
      coll.enabled = false;
      return;
    } else if (a1) x1 = transform.InverseTransformPoint(_interface.transform.TransformPoint(Vector3.zero)).x;
    else if (b1) x2 = transform.InverseTransformPoint(_interface.transform.TransformPoint(Vector3.zero)).x;

    a1 = A.x < -_interface._gridParams.width;
    b1 = B.x < -_interface._gridParams.width;
    if (a1 & b1) {
      rend.enabled = false;
      coll.enabled = false;
      return;
    } else if (a1) x1 = transform.InverseTransformPoint(_interface.transform.TransformPoint(new Vector3(-_interface._gridParams.width, 0, 0))).x;
    else if (b1) x2 = transform.InverseTransformPoint(_interface.transform.TransformPoint(new Vector3(-_interface._gridParams.width, 0, 0))).x;

    framepoints[0] = new Vector3(x1, y1, 0);
    framepoints[1] = new Vector3(x1, y2, 0);
    framepoints[2] = new Vector3(x2, y2, 0);
    framepoints[3] = new Vector3(x2, y1, 0);

    rend.enabled = true;
    coll.enabled = true;
    mesh.vertices = framepoints;
  }

  public override void grabUpdate(Transform t) {
    Vector2 a = _interface.worldPosToGridPos(t.position, false, false);
    Vector2 dif = a - manipOffset;
    dif.x *= -1;

    if (_interface.notelock) dif.y = 0;
    transform.localPosition = startPosition + dif;

    Vector2 candidate;
    dif.x /= _interface._gridParams.unitSize;
    dif.y /= _interface._gridParams.trackHeight;

    for (int i = 0; i < selectedEvents.Count; i++) {
      if (selectedEvents[i] != null) {
        // in_out
        candidate = new Vector2(selectedEvents[i].multiselect_io.x + dif.x, selectedEvents[i].multiselect_io.y + dif.x);

        if (_interface.snapping) {
          float dist = candidate.y - candidate.x;
          selectedEvents[i].in_out.x = _interface._gridParams.UnittoSnap(candidate.x, false);
          selectedEvents[i].in_out.y = selectedEvents[i].in_out.x + dist;
        } else selectedEvents[i].in_out = candidate;

        // track
        if (!_interface.notelock) {
          int _t = Mathf.RoundToInt(selectedEvents[i].multiselect_track + dif.y);
          if (_t < 0) _t = 0;
          if (_t >= _interface._gridParams.tracks) _t = (int)_interface._gridParams.tracks - 1;
          selectedEvents[i].track = _t;
          selectedEvents[i].body.setHue(selectedEvents[i].track);
        }

        selectedEvents[i].gridUpdate();
      }
    }

    updateViz();
  }

  Vector2 manipOffset;
  Vector2 startPosition;
  public override void setState(manipState state) {
    if (curState == manipState.grabbed && state != manipState.grabbed) {
      for (int i = 0; i < selectedEvents.Count; i++) {
        if (selectedEvents[i] != null) {
          selectedEvents[i].overlapCheck();
        }
      }

      updateViz();
      if (!rend.enabled) Close();
    }

    curState = state;

    if (curState == manipState.none) {
      mats[0].SetColor("_TintColor", Color.blue);
    } else if (curState == manipState.selected) {
      mats[0].SetColor("_TintColor", Color.white);
    } else if (curState == manipState.grabbed) {
      mats[0].SetColor("_TintColor", Color.red);
      manipOffset = _interface.worldPosToGridPos(manipulatorObj.position, false, false);
      startPosition = transform.localPosition;

      for (int i = 0; i < selectedEvents.Count; i++) {
        if (selectedEvents[i] != null) {
          selectedEvents[i].multiselect_io = selectedEvents[i].in_out;
          selectedEvents[i].multiselect_track = selectedEvents[i].track;
        }
      }
    }
  }
}
