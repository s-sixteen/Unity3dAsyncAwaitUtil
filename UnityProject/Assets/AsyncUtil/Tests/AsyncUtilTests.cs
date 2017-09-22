using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class AsyncUtilTests : MonoBehaviour
{
    const string AssetBundleSampleUrl = "http://www.stevevermeulen.com/wp-content/uploads/2017/09/teapot.unity3d";
    const string AssetBundleSampleAssetName = "Teapot";

    Subject<string> _signal = new Subject<string>();

    [SerializeField]
    TestButtonHandler.Settings _buttonSettings = null;

    TestButtonHandler _buttonHandler;

    public void Awake()
    {
        _buttonHandler = new TestButtonHandler(_buttonSettings);
    }

    public void OnGUI()
    {
        _buttonHandler.Restart();

        if (_buttonHandler.Display("Test await seconds"))
        {
            RunAwaitSecondsTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test return value"))
        {
            RunReturnValueTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test try-catch exception"))
        {
            RunTryCatchExceptionTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test unhandled exception"))
        {
            // Note: Without WrapErrors here this wouldn't log anything
            RunUnhandledExceptionTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test IEnumerator"))
        {
            RunIEnumeratorTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test IEnumerator with return value (untyped)"))
        {
            RunIEnumeratorUntypedStringTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test IEnumerator with return value (typed)"))
        {
            RunIEnumeratorStringTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test IEnumerator unhandled exception"))
        {
            RunIEnumeratorUnhandledExceptionAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test IEnumerator try-catch exception"))
        {
            RunIEnumeratorTryCatchExceptionAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Load assetbundle with StartCoroutine"))
        {
            StartCoroutine(RunAsyncOperation2());
        }

        if (_buttonHandler.Display("Load assetbundle with async await"))
        {
            RunAsyncOperationAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test UniRx observable"))
        {
            RunUniRxTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Trigger UniRx observable"))
        {
            _signal.OnNext("zcvd");
        }

        if (_buttonHandler.Display("Test opening notepad"))
        {
            RunOpenNotepadTestAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test www download"))
        {
            RunWwwAsync().WrapErrors();
        }

        if (_buttonHandler.Display("Test www download coroutines"))
        {
            StartCoroutine(RunWww());
        }

        if (_buttonHandler.Display("Test Call Async from coroutine"))
        {
            StartCoroutine(RunAsyncFromCoroutineTest());
        }
    }

    IEnumerator RunAsyncFromCoroutineTest()
    {
        Debug.Log("Waiting 1 second...");
        yield return new WaitForSeconds(1.0f);
        Debug.Log("Waiting 1 second again...");
        yield return RunAsyncFromCoroutineTest2().AsIEnumerator();
        Debug.Log("Done");
    }

    async Task RunAsyncFromCoroutineTest2()
    {
        await new WaitForSeconds(1.0f);
    }

    IEnumerator RunWww()
    {
        var www = new WWW(AssetBundleSampleUrl);
        yield return www;
        Debug.Log("Downloaded " + (www.bytes.Length / 1024) + " kb");
    }

    async Task RunWwwAsync()
    {
        var bytes = (await new WWW(AssetBundleSampleUrl)).bytes;
        Debug.Log("Downloaded " + (bytes.Length / 1024) + " kb");
    }

    async Task RunOpenNotepadTestAsync()
    {
        Debug.Log("Waiting for user to close notepad...");
        await Process.Start("notepad.exe");
        Debug.Log("Closed notepad");
    }

    async Task RunUnhandledExceptionTestAsync()
    {
        // This should be logged when using WrapErrors
        await WaitThenThrowException();
    }

    async Task RunTryCatchExceptionTestAsync()
    {
        var test = NestedRunAsync();
        try
        {
            await test;
        }
        catch (Exception e)
        {
            Debug.Log("Caught expected exception: " + e.Message);
        }
    }

    async Task NestedRunAsync()
    {
        await new WaitForSeconds(1);
        throw new Exception();
    }

    async Task WaitThenThrowException()
    {
        await new WaitForSeconds(1.5f);
        throw new Exception("asdf");
    }

    IEnumerator RunAsyncOperation2()
    {
        yield return InstantiateAssetBundle(
            AssetBundleSampleUrl, AssetBundleSampleAssetName);
    }

    IEnumerator InstantiateAssetBundle(string url, string assetName)
    {
        var request = UnityWebRequest.Get(url);
        yield return request.Send();

        var abLoader = AssetBundle.LoadFromMemoryAsync(request.downloadHandler.data);
        yield return abLoader;
        var assetbundle = abLoader.assetBundle;

        var prefabLoader = assetbundle.LoadAssetAsync<GameObject>(assetName);
        yield return prefabLoader;
        var prefab = prefabLoader.asset as GameObject;

        GameObject.Instantiate(prefab);
        assetbundle.Unload(false);
    }

    async Task RunAsyncOperationAsync()
    {
        await InstantiateAssetBundleAsync(AssetBundleSampleUrl, AssetBundleSampleAssetName);
    }

    async Task InstantiateAssetBundleAsync(string abUrl, string assetName)
    {
        var assetBundle = await AssetBundle.LoadFromMemoryAsync(
            await DownloadRawDataAsync(abUrl));

        var prefab = (GameObject)(await assetBundle.LoadAssetAsync<GameObject>(assetName));

        GameObject.Instantiate(prefab);
        assetBundle.Unload(false);
    }

    async Task InstantiateAssetBundleAsync2(string abUrl, string assetName)
    {
        var assetBundle = await AssetBundle.LoadFromMemoryAsync(
            await DownloadRawDataAsync(abUrl));

        var prefab = (GameObject)(await assetBundle.LoadAssetAsync<GameObject>(assetName));

        GameObject.Instantiate(prefab);
        assetBundle.Unload(false);
    }

    async Task<byte[]> DownloadRawDataAsync(string url)
    {
        var request = UnityWebRequest.Get(url);
        await request.Send();
        return request.downloadHandler.data;
    }

    async Task RunIEnumeratorTryCatchExceptionAsync()
    {
        try
        {
            await WaitThenThrow();
        }
        catch (Exception e)
        {
            Debug.Log("Caught exception! {0}" + e);
        }
    }

    async Task RunIEnumeratorUnhandledExceptionAsync()
    {
        await WaitThenThrow();
    }

    IEnumerator WaitThenThrow()
    {
        yield return WaitThenThrowNested();
    }

    IEnumerator WaitThenThrowNested()
    {
        Debug.Log("Waiting 2 seconds...");
        yield return new WaitForSeconds(2.0f);
        throw new Exception("zxcv");
    }

    async Task RunIEnumeratorStringTestAsync()
    {
        Debug.Log("Waiting for ienumerator...");
        Debug.Log("Done! Result: " + await WaitForString());
    }

    async Task RunIEnumeratorUntypedStringTestAsync()
    {
        Debug.Log("Waiting for ienumerator...");
        string result = (string)(await WaitForStringUntyped());
        Debug.Log("Done! Result: " + result);
    }

    async Task RunIEnumeratorTestAsync()
    {
        Debug.Log("Waiting for ienumerator...");
        await WaitABit();
        Debug.Log("Done!");
    }

    IEnumerator<string> WaitForString()
    {
        var startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < 2)
        {
            yield return null;
        }
        yield return "bsdfgas";
    }

    IEnumerator WaitForStringUntyped()
    {
        yield return WaitABit();
        yield return "qwer";
    }

    IEnumerator WaitABit()
    {
        yield return new WaitForSeconds(1.5f);
    }

    async Task RunUniRxTestAsync()
    {
        Debug.Log("Waiting for UniRx trigger...");
        var result = await _signal;
        Debug.Log("Received UniRx trigger with value: " + result);
    }

    async Task RunReturnValueTestAsync()
    {
        Debug.Log("Waiting to get value...");
        var result = await GetValueExampleAsync();
        Debug.Log("Got value: " + result);
    }

    async Task<string> GetValueExampleAsync()
    {
        await new WaitForSeconds(1.0f);
        return "asdf";
    }

    async Task RunAwaitSecondsTestAsync()
    {
        Debug.Log("Waiting 1 second...");
        await new WaitForSeconds(1.0f);
        Debug.Log("Done!");
    }
}