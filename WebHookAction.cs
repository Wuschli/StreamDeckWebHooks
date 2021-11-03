using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebHooks
{
    [PluginActionId("dev.wuschli.web-hooks.webhookaction")]
    public class WebHookAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    Url = string.Empty,
                    Method = RequestMethod.Get,
                    ContentType = "application/json",
                    Body = "{}"
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "url")]
            public string Url { get; set; }

            [JsonProperty(PropertyName = "method")]
            public RequestMethod Method { get; set; }

            [JsonProperty(PropertyName = "contentType")]
            public string ContentType { get; set; }

            [JsonProperty(PropertyName = "body")]
            public string Body { get; set; }
        }


        private readonly PluginSettings _settings;

        public WebHookAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                _settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                _settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void Dispose()
        {
        }

        public override void KeyPressed(KeyPayload payload)
        {
        }

        public override void KeyReleased(KeyPayload payload)
        {
            _ = DoAction();
        }

        public override void OnTick()
        {
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            _ = SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        private async Task DoAction()
        {
            try
            {
                HttpMethod method;
                switch (_settings.Method)
                {
                    case RequestMethod.Get:
                        method = HttpMethod.Get;
                        break;
                    case RequestMethod.Head:
                        method = HttpMethod.Head;
                        break;
                    case RequestMethod.Post:
                        method = HttpMethod.Post;
                        break;
                    case RequestMethod.Put:
                        method = HttpMethod.Put;
                        break;
                    case RequestMethod.Delete:
                        method = HttpMethod.Delete;
                        break;
                    case RequestMethod.Options:
                        method = HttpMethod.Options;
                        break;
                    case RequestMethod.Trace:
                        method = HttpMethod.Trace;
                        break;
                    default: throw new InvalidEnumArgumentException("Method");
                }

                using var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Add("Accept", _settings.ContentType);
                var request = new HttpRequestMessage(method, _settings.Url);

                if (!string.IsNullOrEmpty(_settings.Body))
                {
                    request.Content = new StringContent(_settings.Body, Encoding.Default, _settings.ContentType);
                }

                var response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{method} to {_settings.Url} returned {response.StatusCode}: {content}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Logger.Instance.LogMessage(TracingLevel.FATAL, $"{e.Message}\n{e.StackTrace}");
                throw;
            }
        }
    }
}