using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

public class LeanCloudRestAPI
{
    public string AppId { get; set; }
    public string AppKey { get; set; }
    private readonly string _baseUrl;

    public LeanCloudRestAPI(string appId, string appKey)
    {
        AppId = appId;
        AppKey = appKey;
        _baseUrl = BuildUrl(appId);
    }

    string BuildUrl(string appKey)
    {
        var prefix = appKey.Substring(0, 8).ToLower();
        return string.Format("https://{0}.api.lncld.net", prefix);
    }

    public IEnumerator Create(string className, string json, Action calback)
    {
        var url = _baseUrl + "/1.1/classes/" + className;
        using (var www = UnityWebRequest.Put(url, json))
        {
            // Unity UnityWebRequest BUG 
            // see:https://forum.unity.com/threads/unitywebrequest-post-url-jsondata-sending-broken-json.414708/
            www.method = "POST";

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-LC-Id", AppId);
            www.SetRequestHeader("X-LC-Key", AppKey);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.downloadHandler.text);
            }
            else
            {
                calback();
            }
        }
    }

    public IEnumerator Query(string className, Dictionary<string, object> parameters, Action<string> calback)
    {
        var url = _baseUrl + "/1.1/classes/" + className + "?";
        var i = 0;
        foreach (var o in parameters)
        {
            if (i != 0)
                url += "&";
            url += o.Key + "=" + o.Value;
            i++;
        }

        using (var www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("X-LC-Id", AppId);
            www.SetRequestHeader("X-LC-Key", AppKey);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.downloadHandler.text);
            }
            else
            {
                calback(www.downloadHandler.text);
            }
        }
    }
}