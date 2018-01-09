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

public class exampleManager : MonoBehaviour {

  public GameObject exampleItemPrefab, vidplayerPrefab;

  Vector3[] bounds = new Vector3[4];
  float height = 1.5f;

  int curSide = 0;
  bool used = false;

  void SetupItems() {
    List<exampleItem> items = new List<exampleItem>();

    for (int i = 0; i < 5; i++) {
      GameObject g = Instantiate(exampleItemPrefab, transform, false) as GameObject;
      g.transform.localPosition = new Vector3(.125f * (i - 2), -.35f, 0);
      g.transform.localRotation = Quaternion.Euler(-90, 90, -90);
      g.transform.localScale = Vector3.one * .9f;
      items.Add(g.GetComponent<exampleItem>());
    }

    items[0].Setup(this, menuItem.deviceType.Drum, "basicDrumExample", "Basic\nDrums");
    items[1].Setup(this, menuItem.deviceType.Tapes, "basicSamplerExample", "Basic\nSampler");
    items[2].Setup(this, menuItem.deviceType.Sequencer, "basicDrumMachineExample", "Drum\nMachine");
    items[3].Setup(this, menuItem.deviceType.Oscillator, "basicOscillatorExample", "Basic\nSynthesizer");
    items[4].Setup(this, menuItem.deviceType.Timeline, "basicSequencerExample", "Basic\nSequencer");

    for (int i = 0; i < 7; i++) {
      GameObject g = Instantiate(exampleItemPrefab, transform, false) as GameObject;
      g.transform.localPosition = new Vector3(-.12f * (i - 3f), -.55f, 0);
      g.transform.localRotation = Quaternion.Euler(-90, 90, -90);
      g.transform.localScale = Vector3.one * .8f;
      items.Add(g.GetComponent<exampleItem>());
    }

    items[5].Setup(this, menuItem.deviceType.Mixer, "mixerExample", "Mixer");
    items[6].Setup(this, menuItem.deviceType.ControlCube, "cubeExample", "Cube");
    items[7].Setup(this, menuItem.deviceType.Maracas, "maracaExample", "Maraca");
    items[8].Setup(this, menuItem.deviceType.Filter, "filterExample", "Filter");
    items[9].Setup(this, menuItem.deviceType.Drum, "complexDrumExample", "Drumkit");
    items[10].Setup(this, menuItem.deviceType.Sequencer, "complexSequencerExample", "Drum\nMachines");
    items[11].Setup(this, menuItem.deviceType.Camera, "camAndMicExample", "Camera\n+ Mic");
  }

  void Start() {
    GameObject g = Instantiate(vidplayerPrefab, transform, false) as GameObject;
    g.transform.localPosition = Vector3.zero;
    g.transform.localRotation = Quaternion.Euler(0, 180, 0);

    StartCoroutine(SetupCheck());
    StartCoroutine(heightUpdate());
  }

  void InitSides() {
    height = Mathf.Clamp(Camera.main.transform.position.y, 1, 2);
    float mag = .5f;
    bounds = new Vector3[4];
    bounds[0] = Vector3.forward * mag;
    bounds[1] = Vector3.right * mag;
    bounds[2] = Vector3.forward * -mag;
    bounds[3] = Vector3.right * -mag;

    for (int i = 0; i < 4; i++) bounds[i].y = height;

    curSide = 0;
    transform.position = bounds[curSide];
    transform.LookAt(new Vector3(0, bounds[curSide].y, 0));
    transform.localScale = Vector3.one * .75f;
  }

  IEnumerator SetupCheck() {
    yield return new WaitForSeconds(.25f);
    while (!menuManager.instance.loaded) {
      yield return null;
    }
    InitSides();
    SetupItems();
    firstLoad();
  }

  void firstLoad() {
    SaveLoadInterface.instance.Load(System.IO.Directory.GetParent(Application.dataPath).FullName + System.IO.Path.DirectorySeparatorChar + "examples" + System.IO.Path.DirectorySeparatorChar + "startExample.xml", true);

    GameObject exampleParent = new GameObject("exampleParent");
    exampleParent.transform.position = new Vector3(-.5f, .5f, 0);
    exampleParent.transform.LookAt(new Vector3(0, 1, 0));

    handle[] handles = FindObjectsOfType<handle>();
    for (int i = 0; i < handles.Length; i++) {
      handles[i].setObjectParent(exampleParent.transform);
    }

    exampleParent.transform.parent = transform;
    exampleParent.transform.localRotation = Quaternion.Euler(-45, 0, 0);
    exampleParent.transform.localPosition = new Vector3(0, -1f / transform.localScale.x, 0);

    drumDeviceInterface[] drums = FindObjectsOfType<drumDeviceInterface>();
    for (int i = 0; i < 3; i++) {
      if (drums[i].transform.localScale.x > .95f) {
        drums[i].displayDrumsticks(true);
        break;
      }
    }
  }

  void killManipHints() {
    manipulator[] manips = FindObjectsOfType<manipulator>();
    for (int i = 0; i < manips.Length; i++) manips[i].toggleTips(false);
  }

  public void LoadExample(string s) {
    killManipHints();
    if (_loadRoutine != null) StopCoroutine(_loadRoutine);
    _loadRoutine = StartCoroutine(loadRoutine(s));
  }

  void OnDisable() {
    StopAllCoroutines();
    killManipHints();

    transform.position = bounds[curSide];
    transform.LookAt(new Vector3(0, bounds[curSide].y, 0));
    transform.localScale = Vector3.one * .75f;

    moving = false;
  }

  Coroutine _loadRoutine;
  IEnumerator loadRoutine(string s) {
    moving = true;
    List<GameObject> gameObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("instrument"));
    for (int i = 0; i < gameObjects.Count; i++) {
      Destroy(gameObjects[i]);
    }

    GameObject prevParent = GameObject.Find("exampleParent");
    if (prevParent != null) Destroy(prevParent);

    SaveLoadInterface.instance.Load(System.IO.Directory.GetParent(Application.dataPath).FullName + System.IO.Path.DirectorySeparatorChar + "examples" + System.IO.Path.DirectorySeparatorChar + s + ".xml", true);

    Vector3 avg = Vector3.zero;
    int objectCount = 0;
    GameObject[] gameObjectsB = GameObject.FindGameObjectsWithTag("instrument");
    for (int i = 0; i < gameObjectsB.Length; i++) {
      if (!gameObjects.Contains(gameObjectsB[i])) {
        avg = avg + gameObjectsB[i].transform.position;
        objectCount++;
      }
    }

    if (objectCount > 0) avg = avg / objectCount;

    GameObject exampleParent = new GameObject("exampleParent");
    exampleParent.transform.position = avg;
    exampleParent.transform.LookAt(new Vector3(0, avg.y, 0));

    handle[] handles = FindObjectsOfType<handle>();
    for (int i = 0; i < handles.Length; i++) {
      handles[i].setObjectParent(exampleParent.transform);
    }

    Vector3 examplePosA, examplePosB;
    float mult = 1.5f;
    if (!used) {
      examplePosA = bounds[(curSide + 1) % 4] * 2;
      examplePosA.y = bounds[(curSide + 1) % 4].y - .3f;

      mult = 2;
      examplePosB = bounds[curSide];
      examplePosB.y = examplePosB.y - .3f;
    } else {
      examplePosB = bounds[(curSide + 1) % 4];
      examplePosB.y = examplePosB.y - .3f;

      examplePosA = Quaternion.Euler(0, -90, 0) * examplePosB;
      examplePosA.x *= 2;
      examplePosA.z *= 2;
    }

    exampleParent.transform.position = examplePosA;
    exampleParent.transform.LookAt(new Vector3(0, examplePosA.y, 0));

    int candidate = curSide - 1;
    if (candidate < 0) candidate = 3;
    if (used) candidate = curSide;

    float timer = 0;
    Vector3 posA = transform.position;
    Vector3 posB = bounds[candidate];

    Quaternion q1 = Quaternion.FromToRotation(posA, posB);
    Quaternion q2 = Quaternion.FromToRotation(examplePosA, examplePosB);
    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime / mult);
      float t = SmoothStep(timer);

      Vector3 pos = Quaternion.Lerp(Quaternion.identity, q1, t) * posA;
      pos = pos.normalized * Mathf.Lerp(posA.magnitude, posB.magnitude, t);
      transform.position = pos;
      transform.LookAt(new Vector3(0, pos.y, 0));


      Vector3 posEx = Quaternion.Lerp(Quaternion.identity, q2, t) * examplePosA;
      posEx = posEx.normalized * Mathf.Lerp(examplePosA.magnitude, examplePosB.magnitude, t);
      exampleParent.transform.position = posEx;
      exampleParent.transform.LookAt(new Vector3(0, posEx.y, 0));

      yield return null;
    }

    curSide = candidate;
    used = true;
    moving = false;
  }

  float SmoothStep(float t) {
    return t * t * (3f - 2f * t);
  }

  bool moving = false;
  IEnumerator heightUpdate() {
    yield return new WaitForSeconds(3);
    while (true) {
      float thresh = .15f;
      if (height - transform.position.y < thresh || moving) yield return new WaitForSeconds(3);
      else {
        float timer = 0;
        Vector3 pos = transform.position;
        Vector3 posB = pos;

        posB.y = height;
        for (int i = 0; i < 4; i++) bounds[i].y = height;
        while (timer < 1 && !moving) {
          timer = Mathf.Clamp01(timer + Time.deltaTime / 2f);
          transform.position = Vector3.Lerp(pos, posB, SmoothStep(timer));
          yield return null;
        }
      }
    }
  }

  void OnDestroy() {
    StopAllCoroutines();
  }

  void Update() {
    height = Mathf.Lerp(height, Mathf.Clamp(Camera.main.transform.position.y, 1, 2), .03f);
  }
}
