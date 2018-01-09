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

public class libraryDeviceInterface : deviceInterface {

  public GameObject sprocketPrefab, tapePrefab, tapegroupPrefab;
  public GameObject sprocketRing, panelRing, sprocketRingB, panelRingB, ghostTape;
  public TextMesh note;

  public Transform tapeHolder;

  public tape curTape;
  panelRingComponentInterface _panelRingPrimary, _panelRingSecondary;

  public bool[] spinLocks = new bool[2];

  Quaternion[] lastRotation = new Quaternion[] { Quaternion.identity, Quaternion.identity };

  Color glowColor;

  public override void Awake() {
    base.Awake();
    glowColor = Color.HSVToRGB(.6f, .8f, .5f);
    createSprocketRings();

    _panelRingPrimary = panelRing.GetComponent<panelRingComponentInterface>();
    _panelRingSecondary = panelRingB.GetComponent<panelRingComponentInterface>();

    lastRotation[0] = panelRing.transform.localRotation = sprocketRing.transform.localRotation;
    lastRotation[1] = panelRingB.transform.localRotation = sprocketRingB.transform.localRotation;
  }


  void Start() {
    _panelRingPrimary.labels = sampleManager.instance.sampleDictionary.Keys.ToList();
    _panelRingPrimary.loadPanels(sprocketRadius * .95f);
    _panelRingSecondary.loadPanels(sprocketRadius * .85f);
  }


  float emission = .5f;
  const int sprocketCount = 45;
  const float sprocketRadius = .25f;

  sprocket[][] sprockets;
  void createSprocketRings() {
    sprockets = new sprocket[2][];
    sprockets[0] = new sprocket[sprocketCount];
    sprockets[1] = new sprocket[sprocketCount];
    for (int i = 0; i < sprocketCount; i++) {

      GameObject g = Instantiate(sprocketPrefab, sprocketRing.transform, false) as GameObject;
      g.transform.localPosition = Quaternion.Euler(360f * i / sprocketCount, 0, 0) * Vector3.forward * sprocketRadius;
      g.transform.LookAt(sprocketRing.transform.position);
      sprockets[0][i] = g.GetComponent<sprocket>();
      g.GetComponent<sprocket>().Setup(transform, sprocketRadius, emission, glowColor);
    }

    for (int i = 0; i < sprocketCount; i++) {
      GameObject g = Instantiate(sprocketPrefab, sprocketRingB.transform, false) as GameObject;
      g.transform.localPosition = Quaternion.Euler(360f * i / sprocketCount, 0, 0) * Vector3.forward * sprocketRadius * .9f;
      g.transform.LookAt(sprocketRingB.transform.position);
      sprockets[1][i] = g.GetComponent<sprocket>();
      g.GetComponent<sprocket>().Setup(transform, sprocketRadius, emission, glowColor);
    }
  }

  void Update() {
    manageRotation(panelRing.transform, sprocketRing.transform, 0);
    manageRotation(panelRingB.transform, sprocketRingB.transform, 1);
    if (curTape != null) {
      if (curTape.inDeck()) {
        createNewTape();
      }
    }

    tapeHolder.Rotate(0, Time.deltaTime * 15, 0);
  }

  void manageRotation(Transform a, Transform b, int i) {
    if (a.localRotation != lastRotation[i] || b.localRotation != lastRotation[i]) {
      if (a.localRotation != lastRotation[i] && b.localRotation != lastRotation[i]) {
        lastRotation[i] = Quaternion.Lerp(a.localRotation, b.localRotation, .5f);
        a.localRotation = b.localRotation = lastRotation[i];
      } else if (a.localRotation != lastRotation[i]) {
        lastRotation[i] = b.localRotation = a.localRotation;
      } else {
        lastRotation[i] = a.localRotation = b.localRotation;
      }

      for (int i2 = 0; i2 < sprocketCount; i2++) {
        sprockets[i][i2].UpdatePosition();
      }
    }
  }

  public string curPrimary = "";
  public string curSecondary = "";
  public void updateSecondaryPanels(string s) {
    curPrimary = s;
    curSecondary = "";
    if (curTape != null) Destroy(curTape.gameObject);
    _panelRingSecondary.updatePanels(sampleManager.instance.sampleDictionary[s].Keys.ToList());
    if (sampleManager.instance.sampleDictionary[s].Keys.ToList().Count == 0) {
      note.gameObject.SetActive(true);

      if (s == "Custom") {
        note.text = "Add custom samples from the desktop.\n" +
                    "(Take off your headset and look at the menu\n" +
                    "on the desktop view of the game)";
      } else if (s == "Recordings") {
        note.text = "Sounds saved with the\n" +
                    "RECORDER will show up here.";
      } else {
        note.text = "[category empty]";
      }
    } else {
      note.gameObject.SetActive(false);
    }
  }

  void updateTape(string s, Transform t) {
    curSecondary = s;
    if (curTape != null) Destroy(curTape.gameObject);
    curTape = (Instantiate(tapePrefab, tapeHolder, false) as GameObject).GetComponent<tape>();
    curTape.Setup(s, sampleManager.instance.sampleDictionary[curPrimary][s]);
  }


  public string getFilename(string s) {
    if (s == "") return "";
    return sampleManager.instance.sampleDictionary[curPrimary][s];
  }

  void createNewTape() {
    Vector3 p;
    Quaternion q;
    curTape.getOrigTrans(out p, out q);
    curTape.masterObj = null;
    curTape = (Instantiate(tapePrefab, tapeHolder, false) as GameObject).GetComponent<tape>();

    curTape.transform.localPosition = p;
    curTape.Setup(curSecondary, sampleManager.instance.sampleDictionary[curPrimary][curSecondary]);
  }

  public void forceTape(Transform t, string p, string s) {
    tape g = (Instantiate(tapePrefab, t.position, t.rotation) as GameObject).GetComponent<tape>();
    if (sampleManager.instance.sampleDictionary[p][s] != null) g.Setup(s, sampleManager.instance.sampleDictionary[p][s]);
    else g.Setup(curSecondary, sampleManager.instance.sampleDictionary[curPrimary][curSecondary]);
    t.GetComponent<manipulator>().ForceGrab(g);
    g.transform.localPosition = Vector3.zero;
    g.transform.localRotation = Quaternion.Euler(-90, -90, -90);//.zero;
  }

  public void forceGroup(Transform t, string p) {
    if (sampleManager.instance.sampleDictionary[p].Keys.Count == 0) return;
    tapeGroupDeviceInterface g = (Instantiate(tapegroupPrefab, t.position, t.rotation) as GameObject).GetComponent<tapeGroupDeviceInterface>();
    t.GetComponent<manipulator>().ForceGrab(g.GetComponentInChildren<handle>());
    g.Setup(p);
    g.transform.Rotate(0, 180, 0, Space.Self);
  }

  public void panelEvent(int ID, bool secondary, int panelID) {
    if (!secondary) updateSecondaryPanels(_panelRingPrimary.labels[ID]);
    else updateTape(_panelRingSecondary.labels[ID], _panelRingSecondary.panels[panelID].transform);
  }

  public override InstrumentData GetData() {
    InstrumentData data = new InstrumentData();
    data.deviceType = menuItem.deviceType.Tapes;
    GetTransformData(data);
    return data;
  }

}
