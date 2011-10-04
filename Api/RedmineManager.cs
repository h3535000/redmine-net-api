﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using Redmine.Net.Api.Types;
using Version = Redmine.Net.Api.Types.Version;

namespace Redmine.Net.Api
{
    /// <summary>
    /// 
    /// </summary>
    public class RedmineManager
    {
        private const string REQUESTFORMAT = "{0}/{1}/{2}.xml";
        private const string FORMAT = "{0}/{1}.xml";
        private readonly string host, apiKey;
        private readonly CredentialCache cache;
        private readonly Dictionary<Type, String> urls = new Dictionary<Type, string>
                                                             {
                                                                 { typeof(Issue), "issues" },
                                                                 { typeof(Project), "projects" },
                                                                 { typeof(User), "users" },
                                                                 { typeof(News), "news" },
                                                                 { typeof(Query), "queries" },
                                                                 { typeof(Version), "versions"},
                                                                 { typeof(Attachment),"attachments"},
                                                                 { typeof(IssueRelation), "relations"},
                                                                 { typeof(TimeEntry),"time_entries"}
                                                             };

        /// <summary>
        /// Initializes a new instance of the <see cref="RedmineManager"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        public RedmineManager(string host)
        {
            this.host = host;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedmineManager"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="apiKey">The API key.</param>
        public RedmineManager(string host, string apiKey)
        {
            this.host = host;
            this.apiKey = apiKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedmineManager"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="login">The login.</param>
        /// <param name="password">The password.</param>
        public RedmineManager(string host, string login, string password)
        {
            this.host = host;
            cache = new CredentialCache { { new Uri(host), "Basic", new NetworkCredential(login, password) } };
        }

        /// <summary>
        /// Gets the object list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public IList<T> GetObjectList<T>(NameValueCollection parameters) where T : class
        {
            if (!urls.ContainsKey(typeof(T))) return null;

            using (var wc = CreateWebClient(parameters))
            {
                var xml = wc.DownloadString(string.Format(FORMAT, host, urls[typeof(T)]));
                using (var text = new StringReader(xml))
                {
                    using (var xmlReader = new XmlTextReader(text))
                    {
                        xmlReader.Read();
                        xmlReader.Read();
                        return xmlReader.ReadElementContentAsCollection<T>();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The id.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public T GetObject<T>(string id, NameValueCollection parameters) where T : class
        {
            if (!urls.ContainsKey(typeof(T))) return null;

            using (var wc = CreateWebClient(parameters))
            {
                var xml = wc.DownloadString(string.Format(REQUESTFORMAT, host, urls[typeof(T)], id));
                return Deserialize<T>(xml);
            }
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        public void CreateObject<T>(T obj) where T : class
        {
            if (!urls.ContainsKey(typeof(T))) return;

            using (var wc = CreateWebClient(null))
            {
                var xml = Serialize(obj);

                string result = wc.UploadString(string.Format(FORMAT, host, urls[typeof(T)]), xml);
            }
        }

        /// <summary>
        /// Updates the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The id.</param>
        /// <param name="obj">The obj.</param>
        public void UpdateObject<T>(string id, T obj) where T : class
        {
            if (!urls.ContainsKey(typeof(T))) return;

            using (var wc = CreateWebClient(null))
            {
                var xml = Serialize(obj);

                string result = wc.UploadString(string.Format(REQUESTFORMAT, host, urls[typeof(T)], id), "PUT", xml);
            }
        }

        /// <summary>
        /// Deletes the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The id.</param>
        /// <param name="parameters">The parameters.</param>
        public void DeleteObject<T>(string id, NameValueCollection parameters) where T : class
        {
            if (!urls.ContainsKey(typeof(T))) return;

            using (var wc = CreateWebClient(parameters))
            {
                wc.UploadString(string.Format(REQUESTFORMAT, host, urls[typeof(T)], id), "DELETE", string.Empty);
            }
        }

        /// <summary>
        /// Creates the web client.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        protected WebClient CreateWebClient(NameValueCollection parameters)
        {
            var webClient = new WebClient();

            if (parameters != null)
                webClient.QueryString = parameters;

            if (!string.IsNullOrEmpty(apiKey))
            {
                webClient.QueryString["key"] = apiKey;
            }
            else if (cache != null)
            {
                webClient.Credentials = cache;
            }

            webClient.Headers.Add("Content-Type", "text/xml; charset=utf-8");

            return webClient;
        }

        /// <summary>
        /// Serializes the specified obj.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        private static string Serialize<T>(T obj) where T : class
        {
            var xws = new XmlWriterSettings {OmitXmlDeclaration = true};

            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, xws))
                {
                    var sr = new XmlSerializer(typeof (T));
                    sr.Serialize(xmlWriter, obj);
                    return stringWriter.ToString();
                }
            }
        }

        /// <summary>
        /// Deserializes the specified XML.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml">The XML.</param>
        /// <returns></returns>
        private static T Deserialize<T>(string xml) where T : class
        {
            using (var text = new StringReader(xml))
            {
                var sr = new XmlSerializer(typeof(T));
                return sr.Deserialize(text) as T;
            }
        }
    }
}