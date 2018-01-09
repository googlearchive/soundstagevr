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

public class menuItem : manipObject {
  public deviceType item = deviceType.Oscillator;
  public Material menuMat;
  public Renderer glowRend;
  menuManager manager;
  Material glowMat;

  Texture tex;
  GameObject itemPrefab;
  public Renderer symbol;
  TextMesh label;
  Material labelMat;

  bool disabled = false;

  public enum deviceType {
    Oscillator,
    Speaker,
    Sampler,
    Recorder,
    Mixer,
    Drum,
    Keyboard,
    Sequencer,
    Maracas,
    ControlCube,
    Tapes,
    Splitter,
    Valve,
    TouchPad,
    XyloRoll,
    Reverb,
    Microphone,
    Camera,
    Filter,
    Noise,
    Looper,
    Airhorn,
    Timeline,
    ADSR,
    MIDIIN,
    MIDIOUT,
    Max,
    TapeGroup,
    Pano
  };

  Color disabledColor = new Color(.25f, .25f, 1);
  Color normalColor = new Color(.5f, .5f, 1);
  Color selectColor = new Color(.85f, .85f, 1);

  public void disable(bool on) {
    if (disabled == on) return;
    disabled = on;

    GetComponent<Collider>().enabled = !disabled;

    if (!disabled) symbol.material.SetColor("_TintColor", normalColor);
    else symbol.material.SetColor("_TintColor", disabledColor);
  }

  public override void Awake() {
    base.Awake();
    disabledColor = Color.HSVToRGB(.6f, .7f, .2f);
    normalColor = Color.HSVToRGB(.6f, .7f, .9f);
    selectColor = Color.HSVToRGB(.4f, .7f, .9f);

    label = GetComponentInChildren<TextMesh>();
    labelMat = label.GetComponent<Renderer>().material;
    symbol.material.SetColor("_TintColor", normalColor);

    labelMat.SetColor("_TintColor", normalColor);
    glowRend.gameObject.SetActive(false);
    glowMat = glowRend.material;
  }

  public GameObject Setup(deviceType d) {
    item = d;
    tex = Resources.Load("Textures/" + item.ToString() + "Symbol") as Texture;
    symbol.material.SetTexture("_MainTex", tex);
    itemPrefab = Resources.Load("Prefabs/" + item.ToString()) as GameObject;
    label.text = item.ToString();
    if (item == deviceType.Valve) label.text = "Gate";
    if (item == deviceType.MIDIIN) label.text = "MIDI IN";
    if (item == deviceType.MIDIOUT) label.text = "MIDI OUT";
    if (item == deviceType.Sequencer) label.text = "Drum Machine";
    if (item == deviceType.Timeline) label.text = "Sequencer";
    label.gameObject.SetActive(false);
    symbol.gameObject.SetActive(false);
    GameObject g = Instantiate(itemPrefab, transform.position, transform.rotation) as GameObject;
    g.transform.parent = transform;

    manager = transform.parent.parent.GetComponent<menuManager>();

    Vector3 size = Vector3.zero;
    Vector3 center = Vector3.zero;

    if (item == deviceType.Sequencer) {
      for (int i = 0; i < 2; i++) {
        for (int i2 = 0; i2 < 4; i2++) {
          GameObject cube = Instantiate(g.GetComponent<sequencerDeviceInterface>().touchCubePrefab, transform.position, transform.rotation) as GameObject;
          cube.transform.parent = g.transform;
          cube.transform.Translate(Vector3.right * i2 * -.04f, Space.Self);
          cube.transform.Translate(Vector3.up * i * -.04f, Space.Self);
        }

        GameObject seq = Instantiate(g.GetComponent<sequencerDeviceInterface>().samplerPrefab, transform.position, transform.rotation) as GameObject;
        seq.transform.parent = g.transform;
        seq.transform.Translate(Vector3.right * .081f, Space.Self);
        seq.transform.Translate(Vector3.up * i * -.04f, Space.Self);
      }
      Destroy(g.transform.FindChild("stretchNode").gameObject);
    }

    if (item == deviceType.Tapes) {
      GameObject tape = Instantiate(g.GetComponent<libraryDeviceInterface>().tapePrefab, transform, false) as GameObject;
      Destroy(g);
      g = tape;
    }

    if (item == deviceType.Timeline) {
      GameObject tl = Instantiate(Resources.Load("Prefabs/timelineRep") as GameObject, transform, false) as GameObject;
      Destroy(g);
      g = tl;
    }

    MonoBehaviour[] m = g.GetComponentsInChildren<MonoBehaviour>();
    for (int i = 0; i < m.Length; i++) Destroy(m[i]);

    AudioSource[] audios = g.GetComponentsInChildren<AudioSource>();
    for (int i = 0; i < audios.Length; i++) Destroy(audios[i]);

    Rigidbody[] rig = g.GetComponentsInChildren<Rigidbody>();
    for (int i = 0; i < rig.Length; i++) Destroy(rig[i]);

    Renderer[] r = g.GetComponentsInChildren<Renderer>();
    for (int i = 0; i < r.Length; i++) {
      r[i].material = menuMat;
      if (r[i].bounds.size.sqrMagnitude > size.sqrMagnitude) {
        size = r[i].bounds.size;
        center = r[i].bounds.center;
      }
    }

    Collider[] c = g.GetComponentsInChildren<Collider>();
    for (int i = 0; i < c.Length; i++) Destroy(c[i]);
    tooltips t = GetComponentInChildren<tooltips>();
    if (t != null) Destroy(t.gameObject);

    g.tag = "Untagged";
    g.transform.localScale = g.transform.localScale / (size.magnitude * 20);
    g.transform.localPosition = g.transform.localPosition + Vector3.forward * .02f;

    if (item == deviceType.Maracas) g.transform.localPosition = new Vector3(0, -.02f, .02f);

    if (item == deviceType.Camera) {
      g.transform.localRotation = Quaternion.Euler(90, 0, 0);
      Camera[] cams = g.GetComponentsInChildren<Camera>();
      for (int i = 0; i < cams.Length; i++) Destroy(cams[i].gameObject);
      Destroy(g.transform.Find("screenFrame").gameObject);
    }

    if (item == deviceType.Keyboard) {
      g.transform.localPosition = new Vector3(0.013f, 0, .026f);
      g.transform.localScale = Vector3.one * .08f;
      Destroy(g.transform.Find("KeyboardTimeline").gameObject);
    }

    if (item == deviceType.XyloRoll) {
      g.transform.localPosition = new Vector3(.0239f, 0, .02f);
      g.transform.localScale = Vector3.one * .087f;
      g.transform.localRotation = Quaternion.Euler(34, 0, 0);
      Destroy(g.transform.Find("KeyboardTimeline").gameObject);
      Destroy(g.transform.Find("OscillatorBank").gameObject);
      Destroy(g.transform.Find("ADSR").gameObject);
    }

    if (item == deviceType.MIDIOUT) {
      Destroy(g.transform.Find("CChandle").gameObject);
      Destroy(g.transform.Find("NOTEhandle").gameObject);
    }

    if (item == deviceType.Airhorn) {
      g.transform.localPosition = new Vector3(-0.005f, -.018f, 0.02f);
      g.transform.localRotation = Quaternion.Euler(0, 90, 0);
      g.transform.localScale = Vector3.one * .14f;
    }

    if (item == deviceType.Tapes) {
      g.transform.localPosition = new Vector3(0, 0, 0.02f);
      g.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
    if (item == deviceType.Timeline) {
      g.transform.localPosition = new Vector3(0, 0, 0.02f);
      g.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
    if (item == deviceType.Filter) {
      g.transform.localPosition = new Vector3(.015f, 0, .02f);
      g.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }
    if (item == deviceType.Splitter) {
      g.transform.localPosition = new Vector3(.0185f, 0, .02f);
    }
    if (item == deviceType.Sequencer) {
      g.transform.localScale = Vector3.one * .166f;
    }
    if (item == deviceType.ControlCube) g.transform.localPosition = new Vector3(0, -.01f, .024f);
    if (item == deviceType.Reverb) g.transform.localPosition = new Vector3(0, -0.0175f, .02f);
    if (item == deviceType.Drum) {
      g.transform.localPosition = new Vector3(0, 0, .026f);
      g.transform.localRotation = Quaternion.Euler(40, 0, 0);
    }
    if (item == deviceType.Mixer) {
      g.transform.localPosition = new Vector3(0.014f, 0, .02f);
      g.transform.localRotation = Quaternion.Euler(60, 0, 0);
    }

    return g;
  }

  IEnumerator appearRoutine(bool on) {
    Vector3 destSize = Vector3.one;
    Vector3 startSize = Vector3.zero;
    glowRend.gameObject.SetActive(true);
    if (!on) {
      destSize = Vector3.zero;
      startSize = Vector3.one;
    }

    glowMat.SetColor("_EmissionColor", Color.white);
    float timer = 0;
    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
      transform.localScale = Vector3.Lerp(startSize, destSize, timer);
      yield return null;
    }

    timer = 0;
    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
      glowMat.SetColor("_EmissionColor", Color.Lerp(Color.white, Color.black, timer));
      yield return null;
    }
    glowRend.gameObject.SetActive(false);
  }

  Coroutine appearCoroutine;
  public void Appear(bool on) {

    if (appearCoroutine != null) StopCoroutine(appearCoroutine);
    appearCoroutine = StartCoroutine(appearRoutine(on));
  }

  void createItem() {
    GameObject g = Instantiate(itemPrefab, transform.position, manipulatorObj.rotation) as GameObject;

    if (item == deviceType.Tapes) {
      g.transform.Translate(.1f, .02f, -.185f, Space.Self);
    } else if (item != deviceType.Filter && item != deviceType.Airhorn && item != deviceType.ADSR) g.transform.Rotate(0, 180, 0, Space.Self);

    manipulatorObj.GetComponent<manipulator>().ForceGrab(g.GetComponentInChildren<handle>());
  }

  public override void setState(manipState state) {
    curState = state;

    if (disabled) return;

    if (curState == manipState.none) {
      label.gameObject.SetActive(false);
      symbol.gameObject.SetActive(false);
    } else if (curState == manipState.selected) {
      symbol.material.SetColor("_TintColor", normalColor);
      labelMat.SetColor("_TintColor", normalColor);
      label.gameObject.SetActive(true);
      symbol.gameObject.SetActive(true);
      manager.SelectAudio();
    } else if (curState == manipState.grabbed) {
      symbol.material.SetColor("_TintColor", Color.white);
      label.gameObject.SetActive(true);
      symbol.gameObject.SetActive(true);
      labelMat.SetColor("_TintColor", Color.white);
      manager.GrabAudio();
      StartCoroutine(flash());
      createItem();
    }
  }

  IEnumerator flash() {
    float t = 0;
    glowRend.gameObject.SetActive(true);
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 3);
      glowMat.SetFloat("_EmissionGain", Mathf.Lerp(.6f, .2f, t));
      glowMat.SetColor("_EmissionColor", Color.Lerp(Color.white, Color.black, t));
      yield return null;
    }
    glowRend.gameObject.SetActive(false);
  }
}
