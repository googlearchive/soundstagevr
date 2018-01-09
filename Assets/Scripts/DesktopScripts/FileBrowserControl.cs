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
using System;
using System.Collections;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.IO;

public class FileBrowserControl : MonoBehaviour {
  public GameObject prefabBrowser;
  public GameObject fileNotification;
  private string defaultPath = "/";
  private string[] extensions = { ".wav", ".ogg", ".mp3" };
  private string[] imageExtensions = { ".jpg", ".png" };
  bool soundSamples = true;

  string selectedFilePath;
  bool browserOpen = false;

  string[] outputstrings = new string[] { };

  Thread fileBrowserThread;

  void Start() {
    Win32FileDialog.onOutput += OutputHandler;
  }

  public void LaunchBrowser(bool audio = true) {
    if (browserOpen) return;
    browserOpen = true;
    soundSamples = audio;
    fileBrowserThread = new Thread(() => Win32FileDialog.ShowWin32FileDialog(audio ? "Import audio files" : "Import image files", audio ? Win32FileDialog.FilterType.AUDIO_FILES : Win32FileDialog.FilterType.IMAGE_FILES));
    fileBrowserThread.Start();
  }

  IEnumerator FileNoteRoutine(string s) {
    Text uiText = fileNotification.GetComponent<Text>();
    uiText.color = Color.clear;
    fileNotification.SetActive(true);
    uiText.text = s;

    float t = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 4f);
      uiText.color = Color.Lerp(Color.clear, Color.white, t);
      yield return null;
    }

    yield return new WaitForSeconds(3f);

    t = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 4f);
      uiText.color = Color.Lerp(Color.clear, Color.white, 1 - t);
      yield return null;
    }
    fileNotification.SetActive(false);
  }

  void OutputHandler(string[] output) {
    browserOpen = false;
    outputstrings = output;
  }

  Coroutine FileNoteCoroutine;
  private void Output(string[] output) {

    string s = "";
    if (soundSamples) {
      if (output.Length > 0) {
        s = "SAMPLES ADDED:";
        foreach (string o in output) s += "\n" + o;
        foreach (string path in output) sampleManager.instance.AddSample(path);
      } else s = "No samples imported.";
    } else {
      if (output.Length > 0) {
        s = "PANO IMAGES ADDED:";
        foreach (string o in output) s += "\n" + o;
        foreach (string path in output) imageLoad.instance.createPano(path);
      } else s = "No images imported.";
    }

    if (FileNoteCoroutine != null) StopCoroutine(FileNoteCoroutine);
    FileNoteCoroutine = StartCoroutine(FileNoteRoutine(s));
  }

  public void ClearSamples() {
    if (FileNoteCoroutine != null) StopCoroutine(FileNoteCoroutine);
    FileNoteCoroutine = StartCoroutine(FileNoteRoutine("Custom samples emptied."));
    sampleManager.instance.ClearCustomSamples();
  }

  private void Update() {
    if (outputstrings.Length > 0) {
      string[] temparray = new string[outputstrings.Length];
      for(int i = 0; i < temparray.Length; i++) {
        temparray[i] = outputstrings[i];
      }
      Output(temparray);
      outputstrings = new string[] { };
    }
  }
}

public static class Win32FileDialog {
  public enum FilterType {
    AUDIO_FILES,
    IMAGE_FILES
  }

  public delegate void OnOutput(string[] s);
  public static OnOutput onOutput;

  private const string AUDIO_FILES_FILTER = "Audio files (*.wav; *.ogg; *.mp3)\0*.wav;*.ogg:*.mp3\0All files (*.*)\0*.*\0\0";
  private const string IMAGE_FILES_FILTER = "Image files (*.jpg; *.png)\0*.jpg;*.png\0All files (*.*)\0*.*\0\0";


  /// <summary>
  /// Show a Win32 built-in "Open File" dialog.
  /// </summary>
  /// <param name="title">Title of the dialog box.</param>
  /// <param name="filter">The filter to use in the dialog box for selecting files. For example, use
  /// IMAGE_FILES_FILTER for JPG and PNG images.</param>
  /// <param name="selectedFilePath">(Out) the selected file path, if the user confirmed the
  /// dialog box, or null if they cancelled.</param>
  /// <returns>True if the user picked a file and confirmed the dialog box. False if the user cancelled.</returns>
 // public static bool ShowWin32FileDialog(string title, FilterType filterType, out string selectedFilePath) {
  public static bool ShowWin32FileDialog(string title, FilterType filterType) {
    OpenFileName ofn = new OpenFileName();
    ofn.lStructSize = Marshal.SizeOf(ofn);
    if (filterType == FilterType.AUDIO_FILES) {
      ofn.lpstrFilter = AUDIO_FILES_FILTER;
    } else {
      ofn.lpstrFilter = IMAGE_FILES_FILTER;
    }

    ofn.lpstrFile = Marshal.AllocHGlobal(2048 * Marshal.SystemDefaultCharSize);
    ofn.nMaxFile = 2048;

    ofn.lpstrFileTitle = new String(new char[512]);
    ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
    ofn.lFlags = 0x00001000 /* OFN_FILEMUSTEXIST */ |
      0x00000800 /* OFN_PATHMUSTEXIST */ |
      0x00080000 /* OFN_EXPLORER */ |
      0x00000200 /* OFN_ALLOWMULTISELECT  */ |
      0x00000008 /* OFN_NOCHANGEDIR */;
    ofn.lpstrTitle = title;

    if (GetOpenFileName(ofn)) {
      List<string> fileList = new List<string>();

      string filedir = Marshal.PtrToStringAuto(ofn.lpstrFile);
      long filePtr = (long)ofn.lpstrFile + filedir.Length * Marshal.SystemDefaultCharSize + Marshal.SystemDefaultCharSize;

      string filename = Marshal.PtrToStringAuto((IntPtr)filePtr);

      if (filename.Length == 0) {
        fileList.Add(filedir);
      }

      while (filename.Length > 0) {
        fileList.Add(filedir + Path.DirectorySeparatorChar + filename);
        filePtr += filename.Length * Marshal.SystemDefaultCharSize + Marshal.SystemDefaultCharSize;
        filename = Marshal.PtrToStringAuto((IntPtr)filePtr);
      }

      onOutput(fileList.ToArray());
      Marshal.FreeHGlobal(ofn.lpstrFile);
      return true;
    } else {
      onOutput(new string[] { });
      return false;
    }
  }

  /*
  ORIGINAL WIN32 STRUCT:
  typedef struct tagOFN { 
    DWORD         lStructSize; 
    HWND          hwndOwner; 
    HINSTANCE     hInstance; 
    LPCTSTR       lpstrFilter; 
    LPTSTR        lpstrCustomFilter; 
    DWORD         nMaxCustFilter; 
    DWORD         nFilterIndex; 
    LPTSTR        lpstrFile; 
    DWORD         nMaxFile; 
    LPTSTR        lpstrFileTitle; 
    DWORD         nMaxFileTitle; 
    LPCTSTR       lpstrInitialDir; 
    LPCTSTR       lpstrTitle; 
    DWORD         Flags; 
    WORD          nFileOffset; 
    WORD          nFileExtension; 
    LPCTSTR       lpstrDefExt; 
    LPARAM        lCustData; 
    LPOFNHOOKPROC lpfnHook; 
    LPCTSTR       lpTemplateName; 
    void *        pvReserved;
    DWORD         dwReserved;
    DWORD         FlagsEx;
  } OPENFILENAME, *LPOPENFILENAME; 
  */
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  private class OpenFileName {
    public int lStructSize;
    public IntPtr hwndOwner = IntPtr.Zero;
    public IntPtr hInstance = IntPtr.Zero;
    public string lpstrFilter = null;
    public string lpstrCustomFilter = null;
    public int nMaxCustFilter;
    public int nFilterIndex;
    public IntPtr lpstrFile;
    public int nMaxFile = 0;
    public string lpstrFileTitle = null;
    public int nMaxFileTitle = 0;
    public string lpstrInitialDir = null;
    public string lpstrTitle = null;
    public int lFlags;
    public ushort nFileOffset;
    public ushort nFileExtension;
    public string lpstrDefExt = null;
    public IntPtr lCustData = IntPtr.Zero;
    public IntPtr lpfHook = IntPtr.Zero;
    public int lpTemplateName;
    public IntPtr pvReserved = IntPtr.Zero;
    public int dwReserved;
    public int lFlagsEx;
  }

  [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
  private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
}
