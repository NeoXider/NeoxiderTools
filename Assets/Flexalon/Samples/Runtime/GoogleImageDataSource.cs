#if UNITY_TMPRO

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0067

namespace Flexalon.Samples
{
    public class GoogleImageDataSource : MonoBehaviour, DataSource
    {
        // Get your own API key and Programmable Search Engine: https://developers.google.com/custom-search/v1/overview
        public string ApiKey = "";
        public string ProgrammableSearchEngine = "";

        [SerializeField]
        TMP_InputField _inputField = null;

        private string _lastSearched;
        private List<string> _data = new List<string>();
        private bool _requestInProgress = false;

        public IReadOnlyList<object> Data => _data;

        public event Action DataChanged;

        private float _changeTimer;
        private bool _updatePending;

        void Awake()
        {
            if (string.IsNullOrWhiteSpace(ApiKey) || string.IsNullOrWhiteSpace(ProgrammableSearchEngine))
            {
                Debug.LogError("For this sample to work, enter your Google Search API key and Programmable Engine in GoogleImageDataSource. See https://developers.google.com/custom-search/v1/overview");
            }
        }

        void Start()
        {
            if (!string.IsNullOrWhiteSpace(_inputField.text))
            {
                _lastSearched = _inputField.text;
#if UNITY_WEB_REQUEST
                StartCoroutine(Search());
#endif
            }
        }

        void Update()
        {
            if (_lastSearched != _inputField.text && !string.IsNullOrWhiteSpace(_inputField.text) && !_requestInProgress)
            {
                _changeTimer = 1;
                _lastSearched = _inputField.text;
                _updatePending = true;
            }

            if (_updatePending)
            {
                _changeTimer -= Time.deltaTime;
                if (_changeTimer <= 0)
                {
#if UNITY_WEB_REQUEST
                    StartCoroutine(Search());
#endif
                    _updatePending = false;
                }
            }
        }

#if UNITY_WEB_REQUEST
        private IEnumerator Search()
        {
            _requestInProgress = true;
            var request = "https://www.googleapis.com/customsearch/v1?key=" + ApiKey + "&cx=" + ProgrammableSearchEngine + "&q=" + _inputField.text;
            Debug.Log("Search Request: " + request);
            using (UnityWebRequest www = UnityWebRequest.Get(request))
            {
                yield return www.SendWebRequest();
                if (www.responseCode == 200)
                {
                    var json = www.downloadHandler.text;
                    Regex rx = new Regex(@"(https://encrypted-[^""]+)");
                    var matches = rx.Matches(json);

                    _data.Clear();
                    int i = 0;
                    foreach (Match match in matches)
                    {
                        _data.Add(match.Value);
                        if (i++ == 8) break;
                    }

                    Debug.Log("Results: " + _data.Count);
                    DataChanged?.Invoke();
                }
                else
                {
                    Debug.LogError("Search Request Error: " + www.responseCode);
                }
            }

            _requestInProgress = false;
        }
#endif
    }
}

#endif