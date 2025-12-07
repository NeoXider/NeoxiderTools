using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Flexalon.Samples
{
    public class RemoteTextureDataBinding : MonoBehaviour, DataBinding
    {
        private string _url;
        private Material _material;

        void OnDisable()
        {
            Destroy(_material);
        }

        public void SetData(object data)
        {
            if (!_material)
            {
                _material = GetComponent<MeshRenderer>()?.material;
                if (!_material)
                {
                    return;
                }
            }

            var url = (string) data;
            if (url != _url)
            {
                _url = url;
#if UNITY_WEB_REQUEST
                StartCoroutine(DownloadTexture());
#endif
            }
        }

#if UNITY_WEB_REQUEST
        private IEnumerator DownloadTexture()
        {
            var url = _url;
            Debug.Log("Download: " + url);
            using (var www = new UnityWebRequest(url))
            {
                www.downloadHandler = new DownloadHandlerTexture();
                yield return www.SendWebRequest();
                if (www.responseCode == 200 && _url == url)
                {
                    var texture = (www.downloadHandler as DownloadHandlerTexture).texture;
                    if (_material.HasProperty("_BaseColorMap")) // HRDP.Lit
                    {
                        _material.SetTexture("_BaseColorMap", texture);
                    }
                    else if (_material.HasProperty("_BaseMap")) // URP.Lit
                    {
                        _material.SetTexture("_BaseMap", texture);
                    }
                    else if (_material.HasProperty("_MainTex")) // Standard
                    {
                        _material.SetTexture("_MainTex", texture);
                    }
                }
            }
        }
#endif
    }
}