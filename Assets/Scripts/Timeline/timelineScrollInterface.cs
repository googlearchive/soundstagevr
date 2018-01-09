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

public class timelineScrollInterface : componentInterface {

  timelineComponentInterface _deviceInterface;

  public xHandle window, edgeIn, edgeOut, quadEdge;
  public Transform timelineQuad;

  Mesh windowMesh;
  BoxCollider windowCollider, edgeInCollider, edgeOutCollider;

  float startduration = 32;

  float windowHeight = .02f;
  float unitScale = .01f;

  public Vector2 curIO;
  float timelineBound;
  Vector3 windowPos;

  void Awake() {
    _deviceInterface = GetComponentInParent<timelineComponentInterface>();

    windowMesh = new Mesh();
    window.GetComponent<MeshFilter>().mesh = windowMesh;
    window.GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
    windowCollider = window.GetComponent<BoxCollider>();
    edgeInCollider = edgeIn.GetComponent<BoxCollider>();
    edgeOutCollider = edgeOut.GetComponent<BoxCollider>();
  }

  bool initialized = false;
  public void Activate() {
    curIO = _deviceInterface._gridParams.range * unitScale;

    timelineBound = startduration * unitScale;
    quadEdge.xBounds = new Vector2(Mathf.NegativeInfinity, -unitScale * 4);
    quadEdge.transform.localPosition = new Vector3(-timelineBound, 0, 0);

    UpdateQuad();
    UpdateWindow();
    windowPos = window.transform.localPosition;
    initialized = true;
  }

  void UpdateQuad() {
    timelineBound = -quadEdge.transform.localPosition.x;
    timelineQuad.localScale = new Vector3(timelineBound, unitScale / 2f, 1);
    timelineQuad.localPosition = new Vector3(-timelineBound / 2f, 0, 0);

    updateClamps();
  }

  void updateClamps() {
    if (curIO.y > timelineBound) curIO.y = timelineBound;
    if (curIO.x > timelineBound) curIO.x = timelineBound - 4 * unitScale;

    float width = curIO.y - curIO.x;

    window.xBounds.y = 0 - width / 2f;
    window.xBounds.x = -timelineBound + width / 2f;

    float offset = _deviceInterface._gridParams.width / .2f * unitScale;

    edgeIn.xBounds.y = 0;
    edgeIn.xBounds.x = edgeOut.transform.localPosition.x + offset;

    edgeOut.xBounds.y = edgeIn.transform.localPosition.x - offset;
    edgeOut.xBounds.x = -timelineBound;
  }

  public void handleUpdate(float x) {
    _deviceInterface._gridParams.width = -x;

    curIO.y = curIO.x + _deviceInterface._gridParams.width / _deviceInterface._gridParams.unitSize * unitScale;
    UpdateWindow();
  }

  float curWindowX;

  void Update() {
    if (!initialized) return;
    bool updateRequested = false;

    edgeInCollider.enabled = (window.curState != manipObject.manipState.grabbed);
    edgeOutCollider.enabled = (window.curState != manipObject.manipState.grabbed);
    windowCollider.enabled = (edgeIn.curState != manipObject.manipState.grabbed && edgeOut.curState != manipObject.manipState.grabbed);

    if (window.transform.localPosition.x != -curWindowX) {
      float dif = window.transform.localPosition.x + curWindowX;
      curIO.x -= dif;
      curIO.y -= dif;

      Vector3 pos;
      pos = edgeIn.transform.localPosition;
      pos.x = -curIO.x;
      edgeIn.transform.localPosition = pos;

      pos.x = -curIO.y;
      edgeOut.transform.localPosition = pos;

      updateRequested = true;
    }

    if (edgeIn.transform.localPosition.x != -curIO.x) {
      curIO.x = -edgeIn.transform.localPosition.x;
      updateRequested = true;
    }

    if (edgeOut.transform.localPosition.x != -curIO.y) {
      curIO.y = -edgeOut.transform.localPosition.x;
      updateRequested = true;
    }

    if (timelineBound != -quadEdge.transform.localPosition.x) {
      UpdateQuad();
      updateRequested = true;
    }

    if (updateRequested) UpdateWindow();
  }

  void UpdateWindow() {
    windowMesh.Clear();

    float width = curIO.y - curIO.x;
    float pos = curIO.x + width / 2;

    windowCollider.size = new Vector3(width - .01f, windowHeight, .02f);

    Vector3[] points = new Vector3[]
    {
            new Vector3(-width/2f,-windowHeight/2,0),
            new Vector3(-width/2f,windowHeight/2,0),
            new Vector3(width/2f,windowHeight/2,0),
            new Vector3(width/2f,-windowHeight/2,0)
    };

    int[] lines = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 };

    windowMesh.vertices = points;
    windowMesh.SetIndices(lines, MeshTopology.Lines, 0);

    edgeIn.transform.localPosition = new Vector3(-curIO.x, 0, 0);
    edgeOut.transform.localPosition = new Vector3(-curIO.y, 0, 0);
    window.transform.localPosition = new Vector3(-pos, 0, 0);

    updateClamps();

    curWindowX = pos;

    _deviceInterface.updateGrid(curIO / unitScale, _deviceInterface._gridParams.width);
  }
}
