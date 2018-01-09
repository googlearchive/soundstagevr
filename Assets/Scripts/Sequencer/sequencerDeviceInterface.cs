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

public class sequencerDeviceInterface : deviceInterface {
  public GameObject touchCubePrefab, jackOutPrefab, samplerPrefab;
  public Transform stretchNode;
  public xHandle stepSelect;
  public bool running = true;

  float swingPercent = 0;
  int beatSpeed = 0;

  public int[] dimensions = new int[] { 1, 1 };
  int[] curDimensions = new int[] { 0, 0 };
  public List<List<Transform>> cubeList;
  public List<Transform> jackList;
  public List<sequencer> seqList;
  public List<clipPlayerSimple> samplerList;
  public bool[][] cubeStates;
  public bool[] rowMute;
  public string[][] tapeList;

  float cubeConst = .04f;

  int max = 32;

  public sliderNotched beatSlider;
  public omniJack controlInput;
  public button playButton;

  dial swingDial;
  signalGenerator externalPulse;
  beatTracker _beatManager;

  double _phase = 0;
  double _sampleDuration = 0;
  float[] lastPlaySig = new float[] { 0, 0 };

  public TextMesh[] dimensionDisplays;

  public override void Awake() {
    base.Awake();
    cubeList = new List<List<Transform>>();
    jackList = new List<Transform>();
    seqList = new List<sequencer>();
    samplerList = new List<clipPlayerSimple>();

    cubeStates = new bool[max][];
    tapeList = new string[max][];
    rowMute = new bool[max];
    for (int i = 0; i < max; i++) {
      cubeStates[i] = new bool[max];
      tapeList[i] = new string[] { "", "" };
    }

    beatSlider = GetComponentInChildren<sliderNotched>();
    swingDial = GetComponentInChildren<dial>();

    _sampleDuration = 1.0 / AudioSettings.outputSampleRate;
    _beatManager = ScriptableObject.CreateInstance<beatTracker>();

    for (int i = 0; i < dimensionDisplays.Length; i++) {
      dimensionDisplays[i].GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
      dimensionDisplays[i].gameObject.SetActive(false);
    }

    dimensionDisplays[0].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);
    dimensionDisplays[1].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);
  }

  void Start() {
    _beatManager.setTriggers(executeNextStep, resetSteps);
    _beatManager.updateBeatNoTriplets(beatSpeed);
    _beatManager.updateSwing(swingPercent);
  }

  public void SetDimensions(int dimX, int dimY) {
    dimensions[0] = dimX;
    dimensions[1] = dimY;
    Vector3 p = stretchNode.localPosition;
    p.x = dimX * -cubeConst - cubeConst * .75f;
    p.y = dimY * -cubeConst - cubeConst * .75f;

    stretchNode.localPosition = p;

    UpdateDimensions();
  }

  int targetStep = 0;
  public void SelectStep(int s, bool silent = false) {
    selectedStep = targetStep = s;

    if (silent) return;

    for (int i = 0; i < curDimensions[1]; i++) {
      seqList[i].setSignal(cubeStates[targetStep][i]);
    }
  }

  void SelectStepUpdate() {
    if (targetStep == curStep) return;
    if (curStep < dimensions[0]) stepOff(curStep);
    curStep = targetStep;
    stepOn(curStep);
    stepSelect.updatePos(-cubeConst * curStep);
  }

  int curStep = 0;
  public bool silent = false;

  public void executeNextStep() {
    if (stepSelect.curState == manipObject.manipState.grabbed) return;

    int s = 1;

    bool minicheck = runningUpdated;
    if (runningUpdated) {
      s = 0;
      runningUpdated = false;
    }

    int next = (targetStep + s) % dimensions[0];

    if (next == 0 && externalPulse != null && !minicheck) forcePlay(false);
    else SelectStep(next);


  }

  void stepOff(int step) {
    for (int i = 0; i < curDimensions[1]; i++) {
      if (cubeList[i][step] != null) cubeList[i][step].GetComponent<button>().Highlight(false);
    }
  }

  void stepOn(int step) {
    for (int i = 0; i < curDimensions[1]; i++) {
      if (cubeList[i][step] != null) cubeList[i][step].GetComponent<button>().Highlight(true);
    }
  }

  void Update() {
    SelectStepUpdate();

    dimensions[0] = Mathf.CeilToInt((stretchNode.localPosition.x + cubeConst * .75f) / -cubeConst);
    dimensions[1] = Mathf.CeilToInt((stretchNode.localPosition.y + cubeConst * .75f) / -cubeConst);

    if (dimensions[0] < 1) dimensions[0] = 1;
    if (dimensions[1] < 1) dimensions[1] = 1;
    if (dimensions[0] > max) dimensions[0] = max;
    if (dimensions[1] > max) dimensions[1] = max;
    UpdateDimensions();
    UpdateStepSelect();

    if (beatSpeed != beatSlider.switchVal) {
      beatSpeed = beatSlider.switchVal;
      _beatManager.updateBeatNoTriplets(beatSpeed);
    }
    if (swingPercent != swingDial.percent) {
      swingPercent = swingDial.percent;
      _beatManager.updateSwing(swingPercent);
    }

    if (externalPulse != controlInput.signal) {
      externalPulse = controlInput.signal;
      _beatManager.toggleMC(externalPulse == null);
      if (externalPulse != null) forcePlay(false);
    }
  }

  public void forcePlay(bool on) {
    togglePlay(on);
    playButton.phantomHit(on);
  }

  void resetSteps() {
    SelectStep(0, true);
    runningUpdated = true;
  }

  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (externalPulse == null) return;

    double dspTime = AudioSettings.dspTime;

    float[] playBuffer = new float[buffer.Length];
    externalPulse.processBuffer(playBuffer, dspTime, channels);

    for (int i = 0; i < playBuffer.Length; i += channels) {
      if (playBuffer[i] > lastPlaySig[1] && lastPlaySig[1] <= lastPlaySig[0]) {
        _beatManager.beatResetEvent();
        _phase = 0;
        forcePlay(true);
      }
      lastPlaySig[0] = lastPlaySig[1];
      lastPlaySig[1] = playBuffer[i];
    }

    for (int i = 0; i < buffer.Length; i += channels) {
      _phase += _sampleDuration;

      if (_phase > masterControl.instance.measurePeriod) _phase -= masterControl.instance.measurePeriod;
      _beatManager.beatUpdateEvent((float)(_phase / masterControl.instance.measurePeriod));
    }
  }

  int selectedStep = 0;
  void UpdateStepSelect() {
    int s = (int)Mathf.Round(stepSelect.transform.localPosition.x / -cubeConst);
    if (s == selectedStep) return;
    stepSelect.pulse();
    selectedStep = s;
    SelectStep(s);
  }

  void UpdateDimensions() {
    if (dimensions[0] == curDimensions[0] && dimensions[1] == curDimensions[1]) return;

    stretchNode.GetComponent<xyHandle>().pulse();
    if (dimensions[0] > curDimensions[0]) {
      addColumns(dimensions[0] - curDimensions[0]);
    } else if (dimensions[0] < curDimensions[0]) {
      removeColumns(curDimensions[0] - dimensions[0]);
    }
    if (dimensions[1] > curDimensions[1]) {
      addRows(dimensions[1] - curDimensions[1]);
    } else if (dimensions[1] < curDimensions[1]) {
      removeRows(curDimensions[1] - dimensions[1]);
    }

    dimensionDisplays[0].text = curDimensions[0] + " X " + curDimensions[1];
  }

  void addColumns(int c) {
    for (int i = 0; i < c; i++) {
      for (int i2 = 0; i2 < curDimensions[1]; i2++) {
        Transform t = (Instantiate(touchCubePrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
        t.parent = transform;
        t.localRotation = Quaternion.identity;
        float xMult = curDimensions[0];
        float yMult = i2;
        t.localPosition = new Vector3(-cubeConst * xMult, -cubeConst * yMult, 0);

        t.localScale = Vector3.one;
        cubeList[i2].Add(t);

        float Hval = (float)i2 / max;
        t.GetComponent<button>().Setup(curDimensions[0], i2, cubeStates[curDimensions[0]][i2], Color.HSVToRGB(Hval, .9f, .05f));
        Vector3 pJ = jackList[i2].localPosition;
        pJ.x -= cubeConst;
        jackList[i2].localPosition = pJ;
      }
      curDimensions[0]++;
    }
    stepSelect.xBounds.x = -cubeConst * (curDimensions[0] - 1);
    stepSelect.updatePos(stepSelect.transform.localPosition.x);
  }


  void removeColumns(int c) {
    for (int i = 0; i < c; i++) {
      for (int i2 = 0; i2 < curDimensions[1]; i2++) {
        Transform t = cubeList[i2].Last();
        Destroy(t.gameObject);
        cubeList[i2].RemoveAt(cubeList[i2].Count - 1);
        Vector3 pJ = jackList[i2].localPosition;
        pJ.x += cubeConst;
        jackList[i2].localPosition = pJ;
      }
      curDimensions[0]--;
    }
    stepSelect.xBounds.x = -cubeConst * (curDimensions[0] - 1);
    stepSelect.updatePos(stepSelect.transform.localPosition.x);
  }

  void addRows(int c) {
    for (int i = 0; i < c; i++) {
      cubeList.Add(new List<Transform>());
      for (int i2 = 0; i2 < curDimensions[0]; i2++) {
        Transform t = (Instantiate(touchCubePrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
        t.parent = transform;
        t.localRotation = Quaternion.identity;
        float yMult = curDimensions[1];
        float xMult = i2;
        t.localPosition = new Vector3(-cubeConst * xMult, -cubeConst * yMult, 0);
        t.localScale = Vector3.one;
        cubeList.Last().Add(t);
        float Hval = (float)curDimensions[1] / max;
        t.GetComponent<button>().Setup(i2, curDimensions[1], cubeStates[i2][curDimensions[1]], Color.HSVToRGB(Hval, .9f, .05f));
      }


      Transform jack = (Instantiate(jackOutPrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
      jack.parent = transform;
      jack.localRotation = Quaternion.Euler(0, 0, -90);
      jack.localScale = Vector3.one;
      jack.localPosition = new Vector3(-cubeConst / 2f - .001f - cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);

      jackList.Add(jack);
      seqList.Add(jack.GetComponent<sequencer>());

      clipPlayerSimple samp = (Instantiate(samplerPrefab, Vector3.zero, Quaternion.identity, transform) as GameObject).GetComponent<clipPlayerSimple>();
      samp.transform.localRotation = Quaternion.identity;
      samp.transform.localScale = Vector3.one;
      samp.transform.localPosition = new Vector3(.081f, -cubeConst * curDimensions[1], -.028f);
      samp.seqGen = jack.GetComponent<sequencer>();

      samp.gameObject.GetComponent<samplerLoad>().SetSample(tapeList[curDimensions[1]][0], tapeList[curDimensions[1]][1]);
      samp.gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.startToggled = rowMute[curDimensions[1]];

      samplerList.Add(samp);
      curDimensions[1]++;
    }

    updateStepSelectVertical();
  }

  public void LoadSamplerInfo() {
    for (int i = 0; i < curDimensions[1]; i++) {
      samplerList[i].gameObject.GetComponent<samplerLoad>().SetSample(tapeList[curDimensions[i]][0], tapeList[curDimensions[i]][1]);
      samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.keyHit(rowMute[curDimensions[i]]);
    }
  }

  void updateStepSelectVertical() {
    Vector3 sPos = stepSelect.transform.localPosition;
    sPos.y = -cubeConst * (curDimensions[1]);
    stepSelect.transform.localPosition = sPos;
  }

  void removeRows(int c) {
    for (int i = 0; i < c; i++) {
      int z = cubeList.Count - 1;
      for (int i2 = 0; i2 < cubeList[z].Count; i2++) {
        Transform t = cubeList[z][i2];
        Destroy(t.gameObject);
      }
      cubeList.RemoveAt(z);

      Transform j = jackList.Last();
      Destroy(j.gameObject);
      jackList.RemoveAt(jackList.Count - 1);
      seqList.RemoveAt(jackList.Count);

      clipPlayerSimple clipTemp = samplerList.Last();
      int tempIndex = samplerList.Count - 1;
      clipTemp.gameObject.GetComponent<samplerLoad>().getTapeInfo(out tapeList[tempIndex][0], out tapeList[tempIndex][1]);
      rowMute[tempIndex] = clipTemp.gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.isHit;

      Destroy(clipTemp.gameObject);
      samplerList.RemoveAt(tempIndex);

      curDimensions[1]--;
    }

    updateStepSelectVertical();
  }

  public void saveRowSampler(int r) {
    samplerList[r].gameObject.GetComponent<samplerLoad>().getTapeInfo(out tapeList[r][0], out tapeList[r][1]);
    rowMute[r] = samplerList[r].gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.isHit;
  }

  bool runningUpdated = false;
  public void togglePlay(bool on) {
    _beatManager.toggle(on);
    if (!on) SelectStep(0, true);
    else runningUpdated = true;
  }

  public override void hit(bool on, int ID = -1) {
    togglePlay(on);
  }

  public override void hit(bool on, int IDx, int IDy) {
    cubeStates[IDx][IDy] = on;
  }

  void OnDestroy() {
    Destroy(_beatManager);
  }

  public override void onSelect(bool on, int IDx, int IDy) {
    if (!on) dimensionDisplays[1].gameObject.SetActive(false);
    else {
      dimensionDisplays[1].text = ((IDx + 1) + "X" + (IDy + 1)).ToString();
      dimensionDisplays[1].gameObject.SetActive(true);
      Vector3 pos = cubeList[IDy][IDx].localPosition;
      pos.z = .021f;
      dimensionDisplays[1].transform.localPosition = pos;
    }

  }
  Coroutine _rowDisplayFadeRoutine;
  public override void onSelect(bool on, int ID = -1) {
    if (_rowDisplayFadeRoutine != null) StopCoroutine(_rowDisplayFadeRoutine);

    if (on) {
      dimensionDisplays[0].GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
      dimensionDisplays[0].gameObject.SetActive(true);
    } else {
      _rowDisplayFadeRoutine = StartCoroutine(rowDisplayFadeRoutine());
    }
  }

  IEnumerator rowDisplayFadeRoutine() {
    float t = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime);
      dimensionDisplays[0].GetComponent<Renderer>().material.SetColor("_TintColor", Color.Lerp(Color.white, Color.black, t));
      yield return null;
    }
    dimensionDisplays[0].gameObject.SetActive(false);
  }

  public override InstrumentData GetData() {
    SequencerData data = new SequencerData();
    data.deviceType = menuItem.deviceType.Sequencer;
    GetTransformData(data);
    data.speedMult = beatSlider.switchVal;

    data.onSwitch = playButton.isHit;
    data.jackInID = controlInput.transform.GetInstanceID();

    data.dimensions = dimensions;
    data.cubeStates = cubeStates;

    data.jackOutIDs = new int[jackList.Count];
    for (int i = 0; i < jackList.Count; i++) {
      data.jackOutIDs[i] = jackList[i].GetChild(0).GetInstanceID();
    }

    for (int i = 0; i < dimensions[1]; i++) {
      saveRowSampler(i);
    }

    data.rowSamples = tapeList;
    data.rowMute = rowMute;
    data.swing = swingDial.percent;

    data.sampleJackOutIDs = new int[samplerList.Count];
    for (int i = 0; i < samplerList.Count; i++) {
      data.sampleJackOutIDs[i] = samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackout.transform.GetInstanceID();
    }

    return data;
  }

  public override void Load(InstrumentData d) {
    SequencerData data = d as SequencerData;
    base.Load(data);

    playButton.startToggled = data.onSwitch;

    tapeList = data.rowSamples;
    rowMute = data.rowMute;

    controlInput.ID = data.jackInID;
    SetDimensions(data.dimensions[0], data.dimensions[1]);

    for (int i = 0; i < data.cubeStates.Length; i++) {
      for (int i2 = 0; i2 < data.cubeStates[i].Length; i2++) {
        cubeStates[i][i2] = data.cubeStates[i][i2];
      }
    }

    for (int i = 0; i < data.dimensions[0]; i++) {
      for (int i2 = 0; i2 < data.dimensions[1]; i2++) {
        if (data.cubeStates[i][i2]) {
          cubeList[i2][i].GetComponent<button>().keyHit(true);
        }
      }
    }
    for (int i = 0; i < jackList.Count; i++) {
      jackList[i].GetComponentInChildren<omniJack>().ID = data.jackOutIDs[i];
    }
    beatSlider.setVal(data.speedMult);

    swingDial.setPercent(data.swing);

    for (int i = 0; i < samplerList.Count; i++) {
      samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackout.ID = data.sampleJackOutIDs[i];
    }
  }
}

public class SequencerData : InstrumentData {
  public bool onSwitch;
  public int jackInID;
  public int[] dimensions;
  public bool[][] cubeStates;

  public int[] jackOutIDs;

  public int speedMult;
  public string[][] rowSamples;
  public int[] sampleJackOutIDs;
  public bool[] rowMute;
  public float swing;
}