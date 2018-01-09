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
using System.Linq;

public class omniPlug : manipObject {
  public GameObject mouseoverFeedback;
  public int ID = -1;
  public bool outputPlug = false;
  public omniJack connected;

  Color cordColor;
  LineRenderer lr;
  Material mat;

  Transform plugTrans;
  List<Vector3> plugPath = new List<Vector3>();
  public omniPlug otherPlug;

  Vector3 lastPos = Vector3.zero;
  Vector3 lastOtherPlugPos = Vector3.zero;
  float calmTime = 0;

  public signalGenerator signal;

  List<omniJack> targetJackList = new List<omniJack>();
  List<Transform> collCandidates = new List<Transform>();
  masterControl.WireMode wireType = masterControl.WireMode.Curved;

  public override void Awake() {
    base.Awake();
    gameObject.layer = 12; //jacks
    mat = transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material;
    lr = GetComponent<LineRenderer>();
    cordColor = new Color(Random.value, Random.value, Random.value);
    lr.material.SetColor("_TintColor", cordColor);
    mat.SetColor("_TintColor", cordColor);
    mouseoverFeedback.GetComponent<Renderer>().material.SetColor("_TintColor", cordColor);
    mouseoverFeedback.SetActive(false);
    plugTrans = transform.GetChild(0);

    if (masterControl.instance != null) {
      if (!masterControl.instance.jacksEnabled) GetComponent<Collider>().enabled = false;
    }
  }

  public void Setup(float c, bool outputting, omniPlug other) {
    Color jackColor = Color.HSVToRGB(c, .8f, .5f);
    cordColor = Color.HSVToRGB(c, .8f, .2f);

    mat.SetColor("_TintColor", jackColor);
    mouseoverFeedback.GetComponent<Renderer>().material.SetColor("_TintColor", jackColor);
    wireType = masterControl.instance.WireSetting;
    outputPlug = outputting;
    otherPlug = other;

    if (outputPlug) {
      lr.material.SetColor("_TintColor", cordColor);
      plugPath.Add(otherPlug.transform.position);

      updateLineVerts();
      lastOtherPlugPos = otherPlug.transform.position;
    }
  }

  public void setLineColor(Color c) {
    cordColor = c;
    lr.material.SetColor("_TintColor", c);
  }

  public void Activate(omniPlug siblingPlug, omniJack jackIn, Vector3[] tempPath, Color tempColor) {
    float h, s, v;
    Color.RGBToHSV(tempColor, out h, out s, out v);

    Color c1 = Color.HSVToRGB(h, .8f, .5f);
    Color c2 = Color.HSVToRGB(h, .8f, .2f);

    cordColor = tempColor;
    lr.material.SetColor("_TintColor", c2);
    mat.SetColor("_TintColor", c1);
    mouseoverFeedback.GetComponent<Renderer>().material.SetColor("_TintColor", c1);

    if (outputPlug) {

      plugPath = tempPath.ToList<Vector3>();
      updateLineVerts();
      calmTime = 1;
    }

    otherPlug = siblingPlug;
    connected = jackIn;
    connected.beginConnection(this);
    signal = connected.homesignal;

    plugTrans.position = connected.transform.position;
    plugTrans.rotation = connected.transform.rotation;
    plugTrans.parent = connected.transform;
    plugTrans.Rotate(-90, 0, 0);
    plugTrans.Translate(0, 0, -.02f);

    transform.parent = plugTrans.parent;
    transform.position = plugTrans.position;
    transform.rotation = plugTrans.rotation;
    plugTrans.parent = transform;

    lastOtherPlugPos = otherPlug.plugTrans.transform.position;
    lastPos = transform.position;
  }

  public PlugData GetData() {
    PlugData data = new PlugData();
    data.ID = transform.GetInstanceID();
    data.position = transform.position;
    data.rotation = transform.rotation;
    data.scale = transform.localScale;
    data.outputPlug = outputPlug;
    data.connected = connected.transform.GetInstanceID();
    data.otherPlug = otherPlug.transform.GetInstanceID();
    data.plugPath = plugPath.ToArray();
    data.cordColor = cordColor;

    return data;
  }

  void Update() {
    if (otherPlug == null) {
      if (lr) lr.numPositions = 0;
      Destroy(gameObject);
      return;
    }

    bool noChange = true;

    if (curState == manipState.grabbed) {
      if (collCandidates.Contains(closestJack)) {
        if (connected == null) updateConnection(closestJack.GetComponent<omniJack>());
        else if (closestJack != connected.transform) {
          updateConnection(closestJack.GetComponent<omniJack>());
        }
      }
      if (connected != null) {
        if (!collCandidates.Contains(connected.transform)) {
          endConnection();
        }
      }
    }

    bool updateLineNeeded = false;
    if (lastPos != transform.position) {
      findClosestJack();
      if (connected != null) transform.LookAt(connected.transform.position);
      else if (closestJack != null) transform.LookAt(closestJack.position);
      updateLineNeeded = true;
      lastPos = transform.position;
    }

    if (outputPlug) {
      if ((curState != manipState.grabbed && otherPlug.curState != manipState.grabbed)
           && (Vector3.Distance(plugPath.Last(), transform.position) > .002f)
           && (Vector3.Distance(plugPath[0], transform.position) > .002f)) {
        Vector3 a = plugTrans.position - plugPath.Last();
        Vector3 b = otherPlug.plugTrans.transform.position - plugPath[0];
        for (int i = 0; i < plugPath.Count; i++) plugPath[i] += Vector3.Lerp(b, a, (float)i / (plugPath.Count - 1));
        noChange = false;
      }

      if (updateLineNeeded) {
        if (Vector3.Distance(plugPath.Last(), transform.position) > .005f) {
          plugPath.Add(plugTrans.position);
          calmTime = 0;
          noChange = false;
        }
      }

      if (plugPath[0] != otherPlug.plugTrans.transform.position) {
        if (Vector3.Distance(plugPath[0], transform.position) > .005f) {
          plugPath.Insert(0, otherPlug.plugTrans.transform.position);
          calmTime = 0;
          noChange = false;
        }
      }

      lrFlowEffect();

      if (!noChange) {
        calming();
        updateLineVerts();
      }

      updateLineVerts();
      if (noChange) calmLine();
    }
  }

  float flowVal = 0;
  void lrFlowEffect() {
    flowVal = Mathf.Repeat(flowVal - Time.deltaTime, 1);
    lr.material.mainTextureOffset = new Vector2(flowVal, 0);
    lr.material.SetFloat("_EmissionGain", .6f);
  }

  Transform closestJack;
  float jackDist = 0;
  void findClosestJack() {
    Transform t = null;
    float closest = Mathf.Infinity;
    bool shouldUpdateList = false;
    foreach (omniJack j in targetJackList) {
      if (j == null)
        shouldUpdateList = true;
      else if (j.near == null || j.near == this) {
        float z = Vector3.Distance(transform.position, j.transform.position);
        if (z < closest) {
          closest = z;
          t = j.transform;
        }
      }
    }

    if (shouldUpdateList) updateJackList();

    jackDist = closest;
    closestJack = t;
  }

  float calmingConstant = .5f;

  void calming() {
    for (int i = 0; i < plugPath.Count; i++) {
      if (i != 0 && i != plugPath.Count - 1) {
        Vector3 dest = (plugPath[i - 1] + plugPath[i] + plugPath[i + 1]) / 3;
        plugPath[i] = Vector3.Lerp(plugPath[i], dest, calmingConstant);
      }
    }

    for (int i = 0; i < plugPath.Count; i++) {
      if (i != 0 && i != plugPath.Count - 1) {
        if (Vector3.Distance(plugPath[i - 1], plugPath[i]) < .01f) plugPath.RemoveAt(i);
      }
    }

    updateLineVerts();
  }

  public void OnDestroy() { }

  void calmLine() {
    if (calmTime == 1) {
      return;
    }

    Vector3 beginPoint = plugPath[0];
    Vector3 endPoint = plugPath.Last();

    calmTime = Mathf.Clamp01(calmTime + Time.deltaTime / 1.5f);

    for (int i = 0; i < plugPath.Count; i++) {
      if (i != 0 && i != plugPath.Count - 1) {
        Vector3 dest = (plugPath[i - 1] + plugPath[i] + plugPath[i + 1]) / 3;
        plugPath[i] = Vector3.Lerp(plugPath[i], dest, Mathf.Lerp(calmingConstant, 0, calmTime));
      }
    }

    for (int i = 0; i < plugPath.Count; i++) {
      if (i != 0 && i != plugPath.Count - 1) {
        if (Vector3.Distance(plugPath[i - 1], plugPath[i]) < .01f) plugPath.RemoveAt(i);
      }
    }
    plugPath[0] = beginPoint;
    plugPath[plugPath.Count - 1] = endPoint;
    updateLineVerts();
  }


  public void updateLineType(masterControl.WireMode num) {
    wireType = num;
    updateLineVerts();
  }


  bool forcedWireShow = false;
  void updateLineVerts(bool justLast = false) {
    if (wireType == masterControl.WireMode.Curved) {
      lr.numPositions = plugPath.Count;
      if (justLast) lr.SetPosition(plugPath.Count - 1, plugPath.Last());
      else lr.SetPositions(plugPath.ToArray());
    } else if (wireType == masterControl.WireMode.Straight && plugPath.Count > 2) {
      lr.numPositions = 2;
      lr.SetPosition(0, plugPath[0]);
      lr.SetPosition(1, plugPath.Last());
    } else if (forcedWireShow) {
      lr.numPositions = 2;
      lr.SetPosition(0, plugPath[0]);
      lr.SetPosition(1, plugPath.Last());
    } else {
      lr.numPositions = 0;
    }
  }

  void updateJackList() {
    targetJackList.Clear();

    omniJack[] possibleJacks = FindObjectsOfType<omniJack>();
    for (int i = 0; i < possibleJacks.Length; i++) {
      if (possibleJacks[i].outgoing != outputPlug) {
        if (otherPlug.connected == null) {
          targetJackList.Add(possibleJacks[i]);
        } else if (otherPlug.connected.transform.parent != possibleJacks[i].transform.parent) {
          targetJackList.Add(possibleJacks[i]);
        }
      }
    }

  }

  public void Destruct() {
    Destroy(gameObject);
  }

  public void Release() {
    foreach (omniJack j in targetJackList) j.flash(Color.black);
    if (connected == null) {
      if (lr) lr.numPositions = 0;
      otherPlug.Destruct();
      Destroy(gameObject);
    } else {
      if (plugTrans.parent != connected.transform) {
        plugTrans.position = connected.transform.position;
        plugTrans.rotation = connected.transform.rotation;
        plugTrans.parent = connected.transform;
        plugTrans.Rotate(-90, 0, 0);
        plugTrans.Translate(0, 0, -.02f);
      }

      transform.parent = plugTrans.parent;
      transform.position = plugTrans.position;
      transform.rotation = plugTrans.rotation;
      plugTrans.parent = transform;
      calmTime = 0;
    }

    collCandidates.Clear();
  }

  void OnCollisionEnter(Collision coll) {
    if (curState != manipState.grabbed) return;
    if (coll.transform.tag != "omnijack") return;

    omniJack j = coll.transform.GetComponent<omniJack>();

    if (!targetJackList.Contains(j)) return;
    if (j.signal != null || j.near != null) return;

    collCandidates.Add(j.transform);
  }

  void updateConnection(omniJack j) {
    if (connected == j) return;
    if (connected != null) endConnection();
    if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);

    connected = j;
    connected.beginConnection(this);
    signal = connected.homesignal;

    plugTrans.position = connected.transform.position;
    plugTrans.rotation = connected.transform.rotation;
    plugTrans.parent = connected.transform;
    plugTrans.Rotate(-90, 0, 0);
    plugTrans.Translate(0, 0, -.02f);
  }

  void OnCollisionExit(Collision coll) {
    omniJack j = coll.transform.GetComponent<omniJack>();
    if (j != null) {
      if (collCandidates.Contains(coll.transform)) collCandidates.Remove(coll.transform);
    }
  }

  void endConnection() {
    connected.endConnection();
    connected = null;
    plugTrans.parent = transform;
    plugTrans.localPosition = Vector3.zero;
    plugTrans.localRotation = Quaternion.identity;
  }

  public void updateForceWireShow(bool on) {
    if (outputPlug) {
      forcedWireShow = on;
      updateLineVerts();
    } else {
      otherPlug.updateForceWireShow(on);
    }
  }

  public void mouseoverEvent(bool on) {
    mouseoverFeedback.SetActive(on);

    if (!on && curState == manipState.none) {
      updateForceWireShow(false);
    } else updateForceWireShow(true);
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.selected) {
      mouseoverFeedback.SetActive(false);
    }

    if (curState == manipState.grabbed) {
      Release();
    }

    curState = state;

    if (curState == manipState.none) {
      updateForceWireShow(false);
    }

    if (curState == manipState.selected) {
      updateForceWireShow(true);
      mouseoverFeedback.SetActive(true);
    }

    if (curState == manipState.grabbed) {
      updateForceWireShow(true);
      collCandidates.Clear();
      if (connected != null) collCandidates.Add(connected.transform);
      transform.parent = manipulatorObj;
      updateJackList();
      foreach (omniJack j in targetJackList) j.flash(cordColor);
    }
  }
}
